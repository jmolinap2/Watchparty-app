namespace WatchParty.Contracts.Reports;

public sealed record ReportDto(
    Guid Id,
    string Type,
    Guid ReporterUserId,
    Guid? TargetUserId,
    Guid? TargetMessageId,
    Guid? RoomId,
    string Reason,
    string Status,
    DateTimeOffset CreatedAtUtc,
    Guid? ResolvedByUserId,
    DateTimeOffset? ResolvedAtUtc,
    string? ResolutionNote);
