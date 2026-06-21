using WatchParty.Domain.Common;

namespace WatchParty.Domain.Reports;

/// <summary>An abuse report against a user or a message. Reviewed by admins.</summary>
public sealed class Report : AggregateRoot
{
    public const int MaxReasonLength = 1000;

    private Report()
    {
    }

    private Report(
        Guid id,
        ReportType type,
        Guid reporterUserId,
        Guid? targetUserId,
        Guid? targetMessageId,
        Guid? roomId,
        string reason) : base(id)
    {
        Type = type;
        ReporterUserId = reporterUserId;
        TargetUserId = targetUserId;
        TargetMessageId = targetMessageId;
        RoomId = roomId;
        Reason = reason;
        Status = ReportStatus.Open;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public ReportType Type { get; private set; }
    public Guid ReporterUserId { get; private set; }
    public Guid? TargetUserId { get; private set; }
    public Guid? TargetMessageId { get; private set; }
    public Guid? RoomId { get; private set; }
    public string Reason { get; private set; } = null!;
    public ReportStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public string? ResolutionNote { get; private set; }

    public static Result<Report> CreateUserReport(Guid reporterUserId, Guid targetUserId, Guid? roomId, string? reason)
    {
        if (reporterUserId == targetUserId)
        {
            return DomainErrors.Reports.CannotReportSelf;
        }

        var reasonResult = ValidateReason(reason);
        if (reasonResult.IsFailure)
        {
            return reasonResult.Error;
        }

        return new Report(Guid.NewGuid(), ReportType.User, reporterUserId, targetUserId, null, roomId, reasonResult.Value);
    }

    public static Result<Report> CreateMessageReport(Guid reporterUserId, Guid targetMessageId, Guid? roomId, string? reason)
    {
        var reasonResult = ValidateReason(reason);
        if (reasonResult.IsFailure)
        {
            return reasonResult.Error;
        }

        return new Report(Guid.NewGuid(), ReportType.Message, reporterUserId, null, targetMessageId, roomId, reasonResult.Value);
    }

    public Result Resolve(Guid byUserId, string? note)
    {
        if (Status != ReportStatus.Open)
        {
            return DomainErrors.Reports.AlreadyResolved;
        }

        Status = ReportStatus.Resolved;
        Complete(byUserId, note);
        return Result.Success();
    }

    public Result Reject(Guid byUserId, string? note)
    {
        if (Status != ReportStatus.Open)
        {
            return DomainErrors.Reports.AlreadyResolved;
        }

        Status = ReportStatus.Rejected;
        Complete(byUserId, note);
        return Result.Success();
    }

    private void Complete(Guid byUserId, string? note)
    {
        ResolvedByUserId = byUserId;
        ResolvedAtUtc = DateTimeOffset.UtcNow;
        ResolutionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    private static Result<string> ValidateReason(string? reason)
    {
        var trimmed = reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return DomainErrors.Reports.ReasonRequired;
        }

        return trimmed.Length > MaxReasonLength ? trimmed[..MaxReasonLength] : trimmed;
    }
}
