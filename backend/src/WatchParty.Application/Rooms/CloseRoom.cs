using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Application.Abstractions.State;
using WatchParty.Contracts.Realtime;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Rooms;

public sealed record CloseRoomCommand(Guid UserId, Guid RoomId) : ICommand<Result>;

public sealed class CloseRoomCommandHandler(
    IRoomRepository roomRepository,
    IRoomRealtimeNotifier notifier,
    IPlaybackStateStore playbackStateStore,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CloseRoomCommand, Result>
{
    public async Task<Result> Handle(CloseRoomCommand command, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
        if (room is null)
        {
            return DomainErrors.Rooms.NotFound;
        }

        var result = room.Close(command.UserId);
        if (result.IsFailure)
        {
            return result.Error;
        }

        roomRepository.Update(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await playbackStateStore.RemoveAsync(room.Id, cancellationToken);
        await notifier.RoomClosedAsync(new RoomClosedEvent(room.Id), cancellationToken);

        return Result.Success();
    }
}
