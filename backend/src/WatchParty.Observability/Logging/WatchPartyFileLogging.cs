using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WatchParty.Observability.Logging;

public static class WatchPartyFileLoggingExtensions
{
    public static ILoggingBuilder AddWatchPartyFileLogger(
        this ILoggingBuilder builder,
        IConfiguration configuration,
        string applicationName,
        string defaultFileName)
    {
        var options = WatchPartyFileLoggerOptions.Create(configuration, applicationName, defaultFileName);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<ILoggerProvider, WatchPartyFileLoggerProvider>();
        return builder;
    }
}

public static class WatchPartyLogPaths
{
    public static string ResolveLogFilePath(
        IConfiguration configuration,
        string defaultFileName,
        bool useConfiguredFileName = true)
    {
        var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory) ?? AppContext.BaseDirectory;

        if (useConfiguredFileName)
        {
            var configuredPath = configuration["WatchPartyLogs:FilePath"];
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.GetFullPath(Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.Combine(repositoryRoot, configuredPath));
            }
        }

        var directory = configuration["WatchPartyLogs:Directory"];
        var logDirectory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(repositoryRoot, "logs")
            : Path.IsPathRooted(directory)
                ? directory
                : Path.Combine(repositoryRoot, directory);

        var fileName = useConfiguredFileName
            ? configuration["WatchPartyLogs:FileName"] ?? defaultFileName
            : defaultFileName;

        return Path.GetFullPath(Path.Combine(logDirectory, fileName));
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WatchParty.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

public static class WatchPartyStartupLogger
{
    public static void LogFatal(
        IConfiguration configuration,
        string applicationName,
        string defaultFileName,
        Exception exception)
    {
        try
        {
            var path = WatchPartyLogPaths.ResolveLogFilePath(configuration, defaultFileName);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
            File.AppendAllText(
                path,
                $"{timestamp} [FTL] {applicationName} Startup failed before the host could finish starting.{Environment.NewLine}{exception}{Environment.NewLine}");
        }
        catch (Exception logException)
        {
            Console.Error.WriteLine($"Could not write WatchParty startup log: {logException.Message}");
        }
    }
}

internal sealed class WatchPartyFileLoggerOptions
{
    private WatchPartyFileLoggerOptions(
        string applicationName,
        string logFilePath,
        LogLevel minimumLevel,
        bool includeScopes)
    {
        ApplicationName = applicationName;
        LogFilePath = logFilePath;
        MinimumLevel = minimumLevel;
        IncludeScopes = includeScopes;
    }

    public string ApplicationName { get; }
    public string LogFilePath { get; }
    public LogLevel MinimumLevel { get; }
    public bool IncludeScopes { get; }

    public static WatchPartyFileLoggerOptions Create(
        IConfiguration configuration,
        string applicationName,
        string defaultFileName)
    {
        var minimumLevel = Enum.TryParse<LogLevel>(
            configuration["WatchPartyLogs:MinimumLevel"],
            ignoreCase: true,
            out var parsed)
            ? parsed
            : LogLevel.Information;

        var includeScopes = !bool.TryParse(configuration["WatchPartyLogs:IncludeScopes"], out var parsedScopes)
            || parsedScopes;

        return new WatchPartyFileLoggerOptions(
            applicationName,
            WatchPartyLogPaths.ResolveLogFilePath(configuration, defaultFileName),
            minimumLevel,
            includeScopes);
    }
}

internal sealed class WatchPartyFileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly object syncRoot = new();
    private readonly StreamWriter? writer;
    private IExternalScopeProvider scopeProvider = new LoggerExternalScopeProvider();

    public WatchPartyFileLoggerProvider(WatchPartyFileLoggerOptions options)
    {
        Options = options;

        try
        {
            var directory = Path.GetDirectoryName(options.LogFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            writer = new StreamWriter(new FileStream(
                options.LogFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite))
            {
                AutoFlush = true
            };

            WriteLine(BuildLifecycleLine(options, "Log file opened."));
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Could not open WatchParty log file '{options.LogFilePath}': {exception.Message}");
        }
    }

    private WatchPartyFileLoggerOptions Options { get; }

    public ILogger CreateLogger(string categoryName) =>
        new WatchPartyFileLogger(categoryName, Options, () => scopeProvider, WriteLine);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) =>
        this.scopeProvider = scopeProvider;

    public void Dispose()
    {
        lock (syncRoot)
        {
            writer?.Flush();
            writer?.Dispose();
        }
    }

    private void WriteLine(string line)
    {
        if (writer is null)
        {
            return;
        }

        lock (syncRoot)
        {
            writer.WriteLine(line);
        }
    }

    private static string BuildLifecycleLine(WatchPartyFileLoggerOptions options, string message)
    {
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
        return $"{timestamp} [INF] {options.ApplicationName} WatchParty.Observability {message}";
    }
}

internal sealed class WatchPartyFileLogger(
    string categoryName,
    WatchPartyFileLoggerOptions options,
    Func<IExternalScopeProvider> scopeProvider,
    Action<string> writeLine) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        scopeProvider().Push(state);

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None && logLevel >= options.MinimumLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrWhiteSpace(message) && exception is null)
        {
            return;
        }

        var builder = new StringBuilder();
        builder
            .Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture))
            .Append(" [")
            .Append(GetLevelName(logLevel))
            .Append("] ")
            .Append(options.ApplicationName)
            .Append(' ')
            .Append(categoryName);

        if (eventId.Id != 0 || !string.IsNullOrWhiteSpace(eventId.Name))
        {
            builder.Append(" event=").Append(eventId.Id);
            if (!string.IsNullOrWhiteSpace(eventId.Name))
            {
                builder.Append('/').Append(eventId.Name);
            }
        }

        if (Activity.Current is { } activity)
        {
            builder
                .Append(" trace=")
                .Append(activity.TraceId)
                .Append(" span=")
                .Append(activity.SpanId);
        }

        builder.Append(" - ").Append(message);

        if (options.IncludeScopes)
        {
            AppendScopes(builder);
        }

        if (exception is not null)
        {
            builder.AppendLine().Append(exception);
        }

        writeLine(builder.ToString());
    }

    private void AppendScopes(StringBuilder builder)
    {
        var scopes = new StringBuilder();
        scopeProvider().ForEachScope((scope, state) =>
        {
            if (scope is null)
            {
                return;
            }

            if (state.Length == 0)
            {
                state.Append(" scopes=[");
            }
            else
            {
                state.Append(" | ");
            }

            state.Append(scope);
        }, scopes);

        if (scopes.Length > 0)
        {
            builder.Append(scopes).Append(']');
        }
    }

    private static string GetLevelName(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "FTL",
            _ => "UNK"
        };
}
