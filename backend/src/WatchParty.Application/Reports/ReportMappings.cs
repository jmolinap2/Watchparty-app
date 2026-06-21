using WatchParty.Contracts.Reports;
using WatchParty.Domain.Reports;

namespace WatchParty.Application.Reports;

public static class ReportMappings
{
    public static ReportDto ToDto(this Report report) => new(
        report.Id,
        report.Type.ToString(),
        report.ReporterUserId,
        report.TargetUserId,
        report.TargetMessageId,
        report.RoomId,
        report.Reason,
        report.Status.ToString(),
        report.CreatedAtUtc,
        report.ResolvedByUserId,
        report.ResolvedAtUtc,
        report.ResolutionNote);
}
