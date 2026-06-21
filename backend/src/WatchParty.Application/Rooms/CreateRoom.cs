using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Services;
using WatchParty.Contracts.Rooms;
using WatchParty.Domain.Common;
using WatchParty.Domain.Rooms;

namespace WatchParty.Application.Rooms;

public sealed record CreateRoomCommand(Guid HostUserId, string Name, bool IsPrivate, int? MaxMembers)
    : ICommand<Result<RoomDto>>;

public sealed class CreateRoomValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Room.MaxNameLength);
        RuleFor(x => x.MaxMembers)
            .InclusiveBetween(2, RoomSettings.HardMaxMembers)
            .When(x => x.MaxMembers.HasValue);
    }
}

public sealed class CreateRoomCommandHandler(
    IRoomRepository roomRepository,
    IInviteCodeGenerator inviteCodeGenerator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateRoomCommand, Result<RoomDto>>
{
    private const int MaxCodeAttempts = 8;

    public async Task<Result<RoomDto>> Handle(CreateRoomCommand command, CancellationToken cancellationToken)
    {
        var codeResult = await GenerateUniqueCodeAsync(cancellationToken);
        if (codeResult.IsFailure)
        {
            return codeResult.Error;
        }

        var settings = RoomSettings.Create(command.IsPrivate, command.MaxMembers);
        var roomResult = Room.Create(command.Name, command.HostUserId, codeResult.Value, settings);
        if (roomResult.IsFailure)
        {
            return roomResult.Error;
        }

        var room = roomResult.Value;
        await roomRepository.AddAsync(room, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return room.ToDto(onlineCount: 0);
    }

    private async Task<Result<RoomCode>> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
        {
            var candidate = inviteCodeGenerator.Generate();
            if (!await roomRepository.CodeExistsAsync(candidate, cancellationToken))
            {
                return RoomCode.FromTrusted(candidate);
            }
        }

        return DomainErrors.Rooms.InviteCodeExhausted;
    }
}
