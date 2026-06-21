using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Application.Abstractions.State;
using WatchParty.Contracts.Realtime;
using WatchParty.Domain.Admin;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Admin;

public sealed record AdminCloseRoomCommand(Guid AdminUserId, Guid RoomId) : ICommand<Result>;

public sealed class AdminCloseRoomCommandHandler(
    IRoomRepository roomRepository,
    IRoomRealtimeNotifier notifier,
    IPlaybackStateStore playbackStateStore,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdminCloseRoomCommand, Result>
{
    public async Task<Result> Handle(AdminCloseRoomCommand command, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
        if (room is null)
        {
            return DomainErrors.Rooms.NotFound;
        }

        var result = room.ForceCloseByAdmin(command.AdminUserId);
        if (result.IsFailure)
        {
            return result.Error;
        }

        roomRepository.Update(room);
        await auditLogRepository.AddAsync(
            AuditLog.Admin("room_closed", command.AdminUserId, "Room", room.Id.ToString()),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await playbackStateStore.RemoveAsync(room.Id, cancellationToken);
        await notifier.RoomClosedAsync(new RoomClosedEvent(room.Id), cancellationToken);
        return Result.Success();
    }
}
