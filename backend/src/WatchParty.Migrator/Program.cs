using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WatchParty.Application;
using WatchParty.Infrastructure;
using WatchParty.Infrastructure.Persistence;
using WatchParty.Observability.Logging;

var quiet = args.Any(arg => string.Equals(arg, "-q", StringComparison.OrdinalIgnoreCase)
    || string.Equals(arg, "--quiet", StringComparison.OrdinalIgnoreCase));

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? "Development";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

try
{
    await using var services = new ServiceCollection()
        .AddSingleton<IConfiguration>(configuration)
        .AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            });
            logging.AddWatchPartyFileLogger(configuration, "WatchParty.Migrator", "watchparty-migrator.log");
        })
        .AddApplication()
        .AddInfrastructure(configuration)
        .AddInfrastructureRuntime(configuration)
        .BuildServiceProvider(validateScopes: true);

    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("WatchParty.Migrator");
    logger.LogInformation("Starting WatchParty database migration for {Environment}.", environment);
    logger.LogInformation(
        "Migrator log file: {LogFile}; Postgres={Postgres}.",
        WatchPartyLogPaths.ResolveLogFilePath(configuration, "watchparty-migrator.log"),
        RedactConnectionString(configuration.GetConnectionString("DefaultConnection")));

    await DbInitializer.InitializeAsync(services);
    logger.LogInformation("WatchParty database migration completed.");
    return 0;
}
catch (Exception exception)
{
    WatchPartyStartupLogger.LogFatal(configuration, "WatchParty.Migrator", "watchparty-migrator.log", exception);
    Console.Error.WriteLine($"WatchParty database migration failed: {exception.Message}");
    return 1;
}
finally
{
    if (!quiet && Environment.UserInteractive)
    {
        Console.WriteLine("Press ENTER to exit...");
        Console.ReadLine();
    }
}

static string? RedactConnectionString(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return connectionString;
    }

    var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    for (var index = 0; index < parts.Length; index++)
    {
        if (parts[index].StartsWith("Password=", StringComparison.OrdinalIgnoreCase)
            || parts[index].StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase))
        {
            var key = parts[index].Split('=', 2)[0];
            parts[index] = $"{key}=***";
        }
    }

    return string.Join(';', parts);
}
