namespace WatchParty.Application.Abstractions.Auditing;

public interface IAuditContextAccessor
{
    AuditContext Current { get; set; }
}
