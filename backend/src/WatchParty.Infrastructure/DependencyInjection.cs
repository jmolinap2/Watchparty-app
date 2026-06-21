using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WatchParty.Application.Abstractions.Admin;
using WatchParty.Application.Abstractions.Auditing;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Infrastructure.Auditing;
using WatchParty.Infrastructure.Persistence;
using WatchParty.Infrastructure.Persistence.Repositories;

namespace WatchParty.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddScoped<IAuditContextAccessor, AuditContextAccessor>();
        services.AddScoped<IAuditLogReader, AuditLogReader>();
        services.AddScoped<IAuditLogWriter, EfAuditLogWriter>();

        services.AddDbContext<WatchPartyDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<WatchPartyDbContext>());
        services.AddScoped<IAllowedDomainRepository, AllowedDomainRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IMediaItemRepository, MediaItemRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IUserBlockRepository, UserBlockRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
