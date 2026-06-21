using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.State;
using WatchParty.Contracts.Rooms;
using WatchParty.Domain.Common;
using WatchParty.Domain.Rooms;

namespace WatchParty.Application.Rooms;

/// <summary>Lightweight room preview for the "join by link/code" screen (before joining).</summary>
public sealed record GetRoomByCodeQuery(string Code) : IQuery<Result<RoomDto>>;

public sealed class GetRoomByCodeValidator : AbstractValidator<GetRoomByCodeQuery>
{
    public GetRoomByCodeValidator() => RuleFor(x => x.Code).NotEmpty();
}

public sealed class GetRoomByCodeQueryHandler(IRoomQueries roomQueries, IPresenceStore presenceStore)
    : IQueryHandler<GetRoomByCodeQuery, Result<RoomDto>>
{
    public async Task<Result<RoomDto>> Handle(GetRoomByCodeQuery query, CancellationToken cancellationToken)
    {
        var codeResult = RoomCode.Create(query.Code);
        if (codeResult.IsFailure)
        {
            return codeResult.Error;
        }

        var room = await roomQueries.GetRoomByCodeAsync(codeResult.Value.Value, cancellationToken);
        if (room is null)
        {
            return DomainErrors.Rooms.CodeNotFound;
        }

        var onlineCount = await presenceStore.GetOnlineCountAsync(room.Id, cancellationToken);
        return room with { OnlineCount = onlineCount };
    }
}
