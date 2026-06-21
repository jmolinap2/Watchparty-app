using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Rooms;
using WatchParty.Domain.Common;
using WatchParty.Domain.Rooms;

namespace WatchParty.Application.Rooms;

public sealed record JoinRoomByCodeCommand(Guid UserId, string Code) : ICommand<Result<RoomDetailDto>>;

public sealed class JoinRoomByCodeValidator : AbstractValidator<JoinRoomByCodeCommand>
{
    public JoinRoomByCodeValidator() => RuleFor(x => x.Code).NotEmpty();
}

public sealed class JoinRoomByCodeCommandHandler(
    IRoomRepository roomRepository,
    RoomDetailComposer detailComposer,
    IUnitOfWork unitOfWork)
    : ICommandHandler<JoinRoomByCodeCommand, Result<RoomDetailDto>>
{
    public async Task<Result<RoomDetailDto>> Handle(JoinRoomByCodeCommand command, CancellationToken cancellationToken)
    {
        var codeResult = RoomCode.Create(command.Code);
        if (codeResult.IsFailure)
        {
            return codeResult.Error;
        }

        var room = await roomRepository.GetByCodeAsync(codeResult.Value.Value, cancellationToken);
        if (room is null)
        {
            return DomainErrors.Rooms.CodeNotFound;
        }

        var joinResult = room.Join(command.UserId);
        if (joinResult.IsFailure)
        {
            return joinResult.Error;
        }

        roomRepository.Update(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await detailComposer.ComposeAsync(room.Id, cancellationToken);
        return detail is null ? DomainErrors.Rooms.NotFound : detail;
    }
}
