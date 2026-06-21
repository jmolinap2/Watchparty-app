using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Contracts.Realtime;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Rooms;

public sealed record KickMemberCommand(Guid UserId, Guid RoomId, Guid TargetUserId) : ICommand<Result>;

public sealed class KickMemberCommandHandler(
    IRoomRepository roomRepository,
    IRoomRealtimeNotifier notifier,
    IUnitOfWork unitOfWork)
    : ICommandHandler<KickMemberCommand, Result>
{
    public async Task<Result> Handle(KickMemberCommand command, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
        if (room is null)
        {
            return DomainErrors.Rooms.NotFound;
        }

        var result = room.Kick(command.UserId, command.TargetUserId);
        if (result.IsFailure)
        {
            return result.Error;
        }

        roomRepository.Update(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Tell the room, then tell the kicked user's own connections so their client exits.
        await notifier.MemberKickedAsync(new MemberKickedEvent(room.Id, command.TargetUserId), cancellationToken);
        await notifier.NotifyUserKickedAsync(room.Id, command.TargetUserId, cancellationToken);

        return Result.Success();
    }
}
