using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using WatchParty.Application.Abstractions;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Application.Abstractions.Services;
using WatchParty.Application.Abstractions.State;
using WatchParty.Application.Common;
using WatchParty.Infrastructure.Persistence.Queries;
using WatchParty.Infrastructure.Security;
using WatchParty.Infrastructure.Services;
using WatchParty.Infrastructure.State;

namespace WatchParty.Infrastructure;

/// <summary>
/// Registers the remaining infrastructure runtime services: Redis state, security,
/// read-side query services and supporting services. Kept separate from
/// <see cref="DependencyInjection"/> (which owns DbContext + repositories).
/// </summary>
public static class DependencyInjectionRuntime
{
    public static IServiceCollection AddInfrastructureRuntime(this IServiceCollection services, IConfiguration configuration)
    {
        // Options
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<MediaSourceOptions>(configuration.GetSection(MediaSourceOptions.SectionName));

        // Redis
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"]
            ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        // Redis-backed live state
        services.AddSingleton<IPlaybackStateStore, RedisPlaybackStateStore>();
        services.AddSingleton<IMetricsCounter, RedisMetricsCounter>();
        services.AddSingleton<RedisPresenceStore>();
        services.AddSingleton<IPresenceStore>(sp => sp.GetRequiredService<RedisPresenceStore>());
        services.AddSingleton<IPresenceMaintenance>(sp => sp.GetRequiredService<RedisPresenceStore>());

        // Security
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();
        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Services
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IInviteCodeGenerator, InviteCodeGenerator>();
        services.AddScoped<IMediaSourceValidator, MediaSourceValidator>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();

        // Read-side query services
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<IRoomQueries, RoomQueries>();
        services.AddScoped<IChatQueries, ChatQueries>();
        services.AddScoped<IReportQueries, ReportQueries>();
        services.AddScoped<IAdminQueries, AdminQueries>();

        return services;
    }
}
