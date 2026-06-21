namespace WatchParty.Contracts.Reports;

public sealed record ReportUserRequest(Guid TargetUserId, Guid? RoomId, string Reason);

public sealed record ReportMessageRequest(Guid MessageId, string Reason);

public sealed record ResolveReportRequest(string? Note);

public sealed record RejectReportRequest(string? Note);
