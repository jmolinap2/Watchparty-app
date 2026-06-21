using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Contracts.Realtime;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Rooms;

public sealed record TransferHostCommand(Guid UserId, Guid RoomId, Guid ToUserId) : ICommand<Result>;

public sealed class TransferHostCommandHandler(
    IRoomRepository roomRepository,
    IRoomRealtimeNotifier notifier,
    IUnitOfWork unitOfWork)
    : ICommandHandler<TransferHostCommand, Result>
{
    public async Task<Result> Handle(TransferHostCommand command, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
        if (room is null)
        {
            return DomainErrors.Rooms.NotFound;
        }

        var result = room.TransferHost(command.UserId, command.ToUserId);
        if (result.IsFailure)
        {
            return result.Error;
        }

        roomRepository.Update(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await notifier.HostTransferredAsync(
            new HostTransferredEvent(room.Id, command.UserId, command.ToUserId),
            cancellationToken);

        return Result.Success();
    }
}
