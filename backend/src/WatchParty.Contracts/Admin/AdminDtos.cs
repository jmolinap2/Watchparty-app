namespace WatchParty.Contracts.Admin;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsBlocked,
    bool EmailConfirmed,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc);

public sealed record AdminUserDetailDto(
    AdminUserDto User,
    string? BlockedReason,
    int RoomsCreated,
    int MessagesSent,
    int ReportsAgainst);

public sealed record AdminRoomDto(
    Guid Id,
    string Code,
    string Name,
    Guid HostUserId,
    string Status,
    int MemberCount,
    int OnlineCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ClosedAtUtc);

public sealed record AllowedDomainDto(
    Guid Id,
    string Host,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc);

public sealed record AuditLogDto(
    Guid Id,
    string Category,
    string Action,
    Guid? ActorUserId,
    string? TargetType,
    string? TargetId,
    string? Details,
    string? IpAddress,
    string? Resource,
    string? Operation,
    string? HttpMethod,
    string? RequestPath,
    int? StatusCode,
    long? DurationMs,
    string? UserAgent,
    string? CorrelationId,
    string? Exception,
    bool HasException,
    DateTimeOffset CreatedAtUtc);

/// <summary>Minimum V1 metrics (scope §11).</summary>
public sealed record MetricsDto(
    long RegisteredUsers,
    long ActiveUsers,
    long RoomsCreated,
    long ActiveRooms,
    long MessagesSent,
    long OpenReports,
    long ResolvedReports,
    long PlaybackErrors,
    long SignalrReconnections);
