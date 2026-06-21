using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Admin;

namespace WatchParty.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository(WatchPartyDbContext dbContext) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog entry, CancellationToken cancellationToken) =>
        await dbContext.AuditLogs.AddAsync(entry, cancellationToken);
}
