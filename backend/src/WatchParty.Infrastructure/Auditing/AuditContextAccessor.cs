using WatchParty.Application.Abstractions.Auditing;

namespace WatchParty.Infrastructure.Auditing;

public sealed class AuditContextAccessor : IAuditContextAccessor
{
    public AuditContext Current { get; set; } = new(null, null, null, null, null, null);
}
