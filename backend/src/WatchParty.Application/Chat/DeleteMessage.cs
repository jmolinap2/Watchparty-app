using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Contracts.Realtime;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Chat;

/// <summary><paramref name="RequesterIsAdmin"/> is supplied by the API from the caller's claims.</summary>
public sealed record DeleteMessageCommand(Guid UserId, Guid MessageId, bool RequesterIsAdmin) : ICommand<Result>;

public sealed class DeleteMessageCommandHandler(
    IChatMessageRepository chatMessageRepository,
    IRoomQueries roomQueries,
    IRoomRealtimeNotifier notifier,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteMessageCommand, Result>
{
    public async Task<Result> Handle(DeleteMessageCommand command, CancellationToken cancellationToken)
    {
        var message = await chatMessageRepository.GetByIdAsync(command.MessageId, cancellationToken);
        if (message is null)
        {
            return DomainErrors.Chat.MessageNotFound;
        }

        // A moderator is the room host or a global admin.
        var room = await roomQueries.GetRoomAsync(message.RoomId, cancellationToken);
        var isModerator = command.RequesterIsAdmin || (room is not null && room.HostUserId == command.UserId);

        var result = message.Delete(command.UserId, isModerator);
        if (result.IsFailure)
        {
            return result.Error;
        }

        chatMessageRepository.Update(message);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await notifier.ChatMessageDeletedAsync(
            new ChatMessageDeletedEvent(message.RoomId, message.Id, command.UserId),
            cancellationToken);

        return Result.Success();
    }
}
