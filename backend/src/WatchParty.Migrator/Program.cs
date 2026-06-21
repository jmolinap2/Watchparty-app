using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WatchParty.Application;
using WatchParty.Infrastructure;
using WatchParty.Infrastructure.Persistence;

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
    })
    .AddApplication()
    .AddInfrastructure(configuration)
    .AddInfrastructureRuntime(configuration)
    .BuildServiceProvider(validateScopes: true);

var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("WatchParty.Migrator");

try
{
    logger.LogInformation("Starting WatchParty database migration for {Environment}.", environment);
    await DbInitializer.InitializeAsync(services);
    logger.LogInformation("WatchParty database migration completed.");
    return 0;
}
catch (Exception exception)
{
    logger.LogCritical(exception, "WatchParty database migration failed.");
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
