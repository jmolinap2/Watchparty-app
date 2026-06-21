using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Rooms;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Rooms;

public sealed record GetRoomDetailQuery(Guid RoomId, Guid UserId) : IQuery<Result<RoomDetailDto>>;

public sealed class GetRoomDetailQueryHandler(IRoomQueries roomQueries, RoomDetailComposer detailComposer)
    : IQueryHandler<GetRoomDetailQuery, Result<RoomDetailDto>>
{
    public async Task<Result<RoomDetailDto>> Handle(GetRoomDetailQuery query, CancellationToken cancellationToken)
    {
        if (!await roomQueries.IsActiveMemberAsync(query.RoomId, query.UserId, cancellationToken))
        {
            return DomainErrors.Rooms.NotMember;
        }

        var detail = await detailComposer.ComposeAsync(query.RoomId, cancellationToken);
        return detail is null ? DomainErrors.Rooms.NotFound : detail;
    }
}
