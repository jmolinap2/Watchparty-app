using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Reports;
using WatchParty.Domain.Common;
using WatchParty.Domain.Reports;

namespace WatchParty.Application.Reports;

public sealed record ReportMessageCommand(Guid ReporterUserId, Guid MessageId, string Reason)
    : ICommand<Result<ReportDto>>;

public sealed class ReportMessageValidator : AbstractValidator<ReportMessageCommand>
{
    public ReportMessageValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(Report.MaxReasonLength);
    }
}

public sealed class ReportMessageCommandHandler(
    IChatMessageRepository chatMessageRepository,
    IReportRepository reportRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReportMessageCommand, Result<ReportDto>>
{
    public async Task<Result<ReportDto>> Handle(ReportMessageCommand command, CancellationToken cancellationToken)
    {
        var message = await chatMessageRepository.GetByIdAsync(command.MessageId, cancellationToken);
        if (message is null)
        {
            return DomainErrors.Chat.MessageNotFound;
        }

        var reportResult = Report.CreateMessageReport(command.ReporterUserId, command.MessageId, message.RoomId, command.Reason);
        if (reportResult.IsFailure)
        {
            return reportResult.Error;
        }

        await reportRepository.AddAsync(reportResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return reportResult.Value.ToDto();
    }
}
