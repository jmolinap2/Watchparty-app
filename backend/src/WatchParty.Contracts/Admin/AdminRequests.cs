namespace WatchParty.Contracts.Admin;

public sealed record BlockUserAdminRequest(string? Reason);

public sealed record SetUserRoleRequest(string Role);

public sealed record AddAllowedDomainRequest(string Host);

public sealed record AuditLogSearchRequest
{
    public DateTimeOffset? StartDateUtc { get; init; }
    public DateTimeOffset? EndDateUtc { get; init; }
    public string? Category { get; init; }
    public Guid? ActorUserId { get; init; }
    public string? Action { get; init; }
    public string? TargetType { get; init; }
    public string? Resource { get; init; }
    public string? Operation { get; init; }
    public bool? HasException { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
