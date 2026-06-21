using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using WatchParty.Infrastructure.Auditing;

namespace WatchParty.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by `dotnet ef` commands. Runtime DI still creates
/// the context from <see cref="DependencyInjection"/>.
/// </summary>
public sealed class WatchPartyDbContextFactory : IDesignTimeDbContextFactory<WatchPartyDbContext>
{
    public WatchPartyDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=watchparty;Username=watchparty;Password=watchparty";

        var optionsBuilder = new DbContextOptionsBuilder<WatchPartyDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new WatchPartyDbContext(optionsBuilder.Options, new AuditContextAccessor());
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var apiProjectPath = FindApiProjectPath(Directory.GetCurrentDirectory());
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        return new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();
    }

    private static string FindApiProjectPath(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            var candidates = new[]
            {
                Path.Combine(directory.FullName, "src", "WatchParty.Api"),
                Path.Combine(directory.FullName, "WatchParty.Api")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(Path.Combine(candidate, "appsettings.json")))
                {
                    return candidate;
                }
            }

            directory = directory.Parent;
        }

        return startPath;
    }
}
