using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Domain.Admin;
using WatchParty.Domain.Identity;

namespace WatchParty.Infrastructure.Persistence;

/// <summary>
/// Applies pending EF Core migrations and seeds the minimum data the system needs
/// to be usable on a fresh database: an administrator account and the initial set
/// of allowed media hosts (including the user-requested YouTube and Google Drive).
/// </summary>
public static class DbInitializer
{
    /// <summary>Default media hosts seeded so the product is testable out of the box.</summary>
    private static readonly string[] DefaultAllowedHosts =
    [
        // User-requested sources (also special-cased by the validator, listed for visibility in admin).
        "youtube.com",
        "youtu.be",
        "drive.google.com",
        "docs.google.com",
        // Public sample MP4/HLS hosts so a fresh install can play something immediately.
        "commondatastorage.googleapis.com",
        "storage.googleapis.com",
        "test-streams.mux.dev"
    ];

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var context = provider.GetRequiredService<WatchPartyDbContext>();

        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync(cancellationToken);

        await SeedAdminAsync(provider, logger, cancellationToken);
        await SeedAllowedDomainsAsync(provider, logger, cancellationToken);
    }

    private static async Task SeedAdminAsync(IServiceProvider provider, ILogger logger, CancellationToken cancellationToken)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var users = provider.GetRequiredService<IUserRepository>();
        var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();

        var adminEmailRaw = configuration["Seed:AdminEmail"] ?? "admin@watchparty.local";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "ChangeMe123!";
        var adminDisplayName = configuration["Seed:AdminDisplayName"] ?? "Administrator";
        var adminUsername = configuration["Seed:AdminUsername"] ?? "admin";
        var normalizedAdminUsername = User.NormalizeUsername(adminUsername);

        var emailResult = Email.Create(adminEmailRaw);
        if (emailResult.IsFailure)
        {
            logger.LogWarning("Seed admin email '{Email}' is invalid; skipping admin seed.", adminEmailRaw);
            return;
        }

        var email = emailResult.Value;
        var existing = await users.GetByEmailAsync(email.Value, cancellationToken);
        if (existing is not null)
        {
            await EnsureSeedAdminAsync(users, unitOfWork, existing, normalizedAdminUsername, logger, cancellationToken);
            return;
        }

        if (normalizedAdminUsername is not null)
        {
            var existingByUsername = await users.GetByUsernameAsync(normalizedAdminUsername, cancellationToken);
            if (existingByUsername is not null)
            {
                logger.LogWarning(
                    "Seed admin email '{SeedEmail}' was not found, but username '{Username}' already belongs to '{ExistingEmail}'. " +
                    "Skipping new admin creation to avoid a username conflict. Existing password was not changed.",
                    email.Value,
                    normalizedAdminUsername,
                    existingByUsername.Email.Value);
                return;
            }
        }
        else
        {
            logger.LogWarning("Seed admin username '{Username}' is invalid; admin will be seeded without username login.", adminUsername);
        }

        var userResult = User.Register(email, passwordHasher.Hash(adminPassword), adminDisplayName);
        if (userResult.IsFailure)
        {
            logger.LogWarning("Could not create seed admin: {Error}", userResult.Error.Code);
            return;
        }

        var admin = userResult.Value;
        admin.SetRole(UserRole.Admin);
        admin.ConfirmEmail();
        if (normalizedAdminUsername is not null)
        {
            admin.SetUsername(normalizedAdminUsername);
        }

        await users.AddAsync(admin, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded administrator account '{Email}' (username '{Username}').", email.Value, admin.Username);
    }

    private static async Task EnsureSeedAdminAsync(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        User admin,
        string? adminUsername,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var changed = false;

        if (admin.Role != UserRole.Admin)
        {
            admin.SetRole(UserRole.Admin);
            changed = true;
        }

        if (!admin.EmailConfirmed)
        {
            admin.ConfirmEmail();
            changed = true;
        }

        if (string.IsNullOrEmpty(admin.Username) && !string.IsNullOrEmpty(adminUsername))
        {
            if (await users.UsernameExistsAsync(adminUsername, cancellationToken))
            {
                logger.LogWarning(
                    "Seed administrator '{Email}' exists, but username '{Username}' is already used by another account. " +
                    "Username login was not backfilled.",
                    admin.Email.Value,
                    adminUsername);
            }
            else
            {
                admin.SetUsername(adminUsername);
                changed = true;
            }
        }

        if (changed)
        {
            users.Update(admin);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Updated seed administrator '{Email}' (username '{Username}'). Existing password was not changed.",
                admin.Email.Value,
                admin.Username ?? "-");
            return;
        }

        logger.LogInformation(
            "Seed administrator '{Email}' already exists (username '{Username}'). Existing password was not changed.",
            admin.Email.Value,
            admin.Username ?? "-");
    }

    private static async Task SeedAllowedDomainsAsync(IServiceProvider provider, ILogger logger, CancellationToken cancellationToken)
    {
        var domains = provider.GetRequiredService<IAllowedDomainRepository>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();

        var added = 0;
        foreach (var host in DefaultAllowedHosts)
        {
            var normalized = AllowedDomain.Normalize(host);
            if (normalized is null)
            {
                continue;
            }

            if (await domains.GetByHostAsync(normalized, cancellationToken) is not null)
            {
                continue;
            }

            var domainResult = AllowedDomain.Create(normalized, addedByUserId: null);
            if (domainResult.IsFailure)
            {
                continue;
            }

            await domains.AddAsync(domainResult.Value, cancellationToken);
            added++;
        }

        if (added > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} allowed media host(s).", added);
        }
    }
}
