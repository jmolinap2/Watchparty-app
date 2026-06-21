using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Admin;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Reports;

public sealed record RejectReportCommand(Guid AdminUserId, Guid ReportId, string? Note) : ICommand<Result>;

public sealed class RejectReportCommandHandler(
    IReportRepository reportRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RejectReportCommand, Result>
{
    public async Task<Result> Handle(RejectReportCommand command, CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdAsync(command.ReportId, cancellationToken);
        if (report is null)
        {
            return DomainErrors.Reports.NotFound;
        }

        var result = report.Reject(command.AdminUserId, command.Note);
        if (result.IsFailure)
        {
            return result.Error;
        }

        reportRepository.Update(report);
        await auditLogRepository.AddAsync(
            AuditLog.Admin("report_rejected", command.AdminUserId, "Report", report.Id.ToString(), command.Note),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
