using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Application.Abstractions.State;
using WatchParty.Contracts.Realtime;
using WatchParty.Domain.Common;
using WatchParty.Domain.Rooms;

namespace WatchParty.Application.Rooms;

public sealed record LeaveRoomCommand(Guid UserId, Guid RoomId) : ICommand<Result>;

public sealed class LeaveRoomCommandHandler(
    IRoomRepository roomRepository,
    IRoomRealtimeNotifier notifier,
    IPlaybackStateStore playbackStateStore,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LeaveRoomCommand, Result>
{
    public async Task<Result> Handle(LeaveRoomCommand command, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
        if (room is null)
        {
            return DomainErrors.Rooms.NotFound;
        }

        var result = room.Leave(command.UserId);
        if (result.IsFailure)
        {
            return result.Error;
        }

        roomRepository.Update(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // The host leaving while alone closes the room: tell everyone and clean up live state.
        if (room.Status == RoomStatus.Closed)
        {
            await playbackStateStore.RemoveAsync(room.Id, cancellationToken);
            await notifier.RoomClosedAsync(new RoomClosedEvent(room.Id), cancellationToken);
        }

        return Result.Success();
    }
}
