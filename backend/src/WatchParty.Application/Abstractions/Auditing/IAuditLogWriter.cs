using WatchParty.Domain.Admin;

namespace WatchParty.Application.Abstractions.Auditing;

public interface IAuditLogWriter
{
    Task WriteAsync(AuditLog log, CancellationToken cancellationToken = default);
}
