using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Rooms;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Rooms;

public sealed record GetMyRoomHistoryQuery(Guid UserId) : IQuery<Result<IReadOnlyList<RoomHistoryItemDto>>>;

public sealed class GetMyRoomHistoryQueryHandler(IRoomQueries roomQueries)
    : IQueryHandler<GetMyRoomHistoryQuery, Result<IReadOnlyList<RoomHistoryItemDto>>>
{
    private const int MaxItems = 50;

    public async Task<Result<IReadOnlyList<RoomHistoryItemDto>>> Handle(GetMyRoomHistoryQuery query, CancellationToken cancellationToken)
    {
        var history = await roomQueries.GetUserRoomHistoryAsync(query.UserId, MaxItems, cancellationToken);
        return Result.Success(history);
    }
}
