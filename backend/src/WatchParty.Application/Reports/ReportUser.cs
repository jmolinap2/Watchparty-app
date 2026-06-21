using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Reports;
using WatchParty.Domain.Common;
using WatchParty.Domain.Reports;

namespace WatchParty.Application.Reports;

public sealed record ReportUserCommand(Guid ReporterUserId, Guid TargetUserId, Guid? RoomId, string Reason)
    : ICommand<Result<ReportDto>>;

public sealed class ReportUserValidator : AbstractValidator<ReportUserCommand>
{
    public ReportUserValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(Report.MaxReasonLength);
    }
}

public sealed class ReportUserCommandHandler(
    IUserRepository userRepository,
    IReportRepository reportRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReportUserCommand, Result<ReportDto>>
{
    public async Task<Result<ReportDto>> Handle(ReportUserCommand command, CancellationToken cancellationToken)
    {
        var target = await userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);
        if (target is null)
        {
            return DomainErrors.Users.NotFound;
        }

        var reportResult = Report.CreateUserReport(command.ReporterUserId, command.TargetUserId, command.RoomId, command.Reason);
        if (reportResult.IsFailure)
        {
            return reportResult.Error;
        }

        await reportRepository.AddAsync(reportResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return reportResult.Value.ToDto();
    }
}
