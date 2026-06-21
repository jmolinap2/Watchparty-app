namespace WatchParty.Application.Abstractions.Auditing;

public sealed record AuditContext(
    Guid? ActorUserId,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId,
    string? HttpMethod,
    string? RequestPath);
