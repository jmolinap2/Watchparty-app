using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Contracts.Chat;
using WatchParty.Domain.Chat;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Chat;

public sealed record SendMessageCommand(Guid UserId, Guid RoomId, string Content) : ICommand<Result<ChatMessageDto>>;

public sealed class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(ChatMessage.MaxLength);
    }
}

public sealed class SendMessageCommandHandler(
    IRoomQueries roomQueries,
    IUserQueries userQueries,
    IChatMessageRepository chatMessageRepository,
    IRoomRealtimeNotifier notifier,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SendMessageCommand, Result<ChatMessageDto>>
{
    public async Task<Result<ChatMessageDto>> Handle(SendMessageCommand command, CancellationToken cancellationToken)
    {
        if (!await roomQueries.IsActiveMemberAsync(command.RoomId, command.UserId, cancellationToken))
        {
            return DomainErrors.Rooms.NotMember;
        }

        var messageResult = ChatMessage.Create(command.RoomId, command.UserId, command.Content);
        if (messageResult.IsFailure)
        {
            return messageResult.Error;
        }

        var message = messageResult.Value;
        await chatMessageRepository.AddAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var sender = await userQueries.GetPublicAsync(command.UserId, cancellationToken);
        var dto = new ChatMessageDto(
            message.Id,
            message.RoomId,
            message.SenderUserId,
            sender?.DisplayName ?? "Unknown",
            sender?.AvatarUrl,
            message.Content,
            message.IsDeleted,
            message.CreatedAtUtc);

        await notifier.ChatMessageReceivedAsync(command.RoomId, dto, cancellationToken);
        return dto;
    }
}
