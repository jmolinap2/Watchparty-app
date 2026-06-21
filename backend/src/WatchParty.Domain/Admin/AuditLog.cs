using WatchParty.Domain.Common;

namespace WatchParty.Domain.Admin;

/// <summary>
/// Immutable audit record. Captures HTTP execution, entity changes, admin actions
/// and security events for accountability (architecture sections 18 and 19).
/// </summary>
public sealed class AuditLog : AggregateRoot
{
    private AuditLog()
    {
    }

    private AuditLog(
        Guid id,
        AuditCategory category,
        string action,
        Guid? actorUserId,
        string? targetType,
        string? targetId,
        string? details,
        string? ipAddress,
        string? resource,
        string? operation,
        string? httpMethod,
        string? requestPath,
        int? statusCode,
        long? durationMs,
        string? userAgent,
        string? correlationId,
        string? exception) : base(id)
    {
        Category = category;
        Action = action;
        ActorUserId = actorUserId;
        TargetType = targetType;
        TargetId = targetId;
        Details = details;
        IpAddress = ipAddress;
        Resource = resource;
        Operation = operation;
        HttpMethod = httpMethod;
        RequestPath = requestPath;
        StatusCode = statusCode;
        DurationMs = durationMs;
        UserAgent = userAgent;
        CorrelationId = correlationId;
        Exception = exception;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public AuditCategory Category { get; private set; }
    public string Action { get; private set; } = null!;
    public Guid? ActorUserId { get; private set; }
    public string? TargetType { get; private set; }
    public string? TargetId { get; private set; }
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }
    public string? Resource { get; private set; }
    public string? Operation { get; private set; }
    public string? HttpMethod { get; private set; }
    public string? RequestPath { get; private set; }
    public int? StatusCode { get; private set; }
    public long? DurationMs { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? Exception { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static AuditLog Admin(string action, Guid actorUserId, string? targetType = null, string? targetId = null, string? details = null) =>
        Create(AuditCategory.Admin, action, actorUserId, targetType, targetId, details);

    public static AuditLog Security(string action, Guid? actorUserId, string? details = null, string? ipAddress = null) =>
        Create(AuditCategory.Security, action, actorUserId, details: details, ipAddress: ipAddress);

    public static AuditLog DataChange(
        string action,
        Guid? actorUserId,
        string targetType,
        string targetId,
        string? details,
        string? ipAddress,
        string? correlationId) =>
        Create(
            AuditCategory.Data,
            action,
            actorUserId,
            targetType,
            targetId,
            details,
            ipAddress,
            resource: targetType,
            operation: action,
            correlationId: correlationId);

    public static AuditLog HttpRequest(
        string action,
        Guid? actorUserId,
        string? ipAddress,
        string? resource,
        string? operation,
        string? httpMethod,
        string? requestPath,
        int statusCode,
        long durationMs,
        string? userAgent,
        string? correlationId,
        string? exception) =>
        Create(
            AuditCategory.Http,
            action,
            actorUserId,
            targetType: "HttpRequest",
            targetId: correlationId,
            details: null,
            ipAddress,
            resource,
            operation,
            httpMethod,
            requestPath,
            statusCode,
            durationMs,
            userAgent,
            correlationId,
            exception);

    public static AuditLog Create(
        AuditCategory category,
        string action,
        Guid? actorUserId = null,
        string? targetType = null,
        string? targetId = null,
        string? details = null,
        string? ipAddress = null,
        string? resource = null,
        string? operation = null,
        string? httpMethod = null,
        string? requestPath = null,
        int? statusCode = null,
        long? durationMs = null,
        string? userAgent = null,
        string? correlationId = null,
        string? exception = null) =>
        new(
            Guid.NewGuid(),
            category,
            action,
            actorUserId,
            targetType,
            targetId,
            details,
            ipAddress,
            resource,
            operation,
            httpMethod,
            requestPath,
            statusCode,
            durationMs,
            userAgent,
            correlationId,
            exception);
}
