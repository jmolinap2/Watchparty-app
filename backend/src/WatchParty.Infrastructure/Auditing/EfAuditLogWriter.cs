using WatchParty.Application.Abstractions.Auditing;
using WatchParty.Domain.Admin;
using WatchParty.Infrastructure.Persistence;

namespace WatchParty.Infrastructure.Auditing;

public sealed class EfAuditLogWriter(WatchPartyDbContext dbContext) : IAuditLogWriter
{
    public async Task WriteAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        await dbContext.AuditLogs.AddAsync(log, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
