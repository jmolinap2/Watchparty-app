using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Chat;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Chat;

public sealed record GetChatHistoryQuery(Guid RoomId, Guid UserId, DateTimeOffset? Before, int Limit)
    : IQuery<Result<IReadOnlyList<ChatMessageDto>>>;

public sealed class GetChatHistoryQueryHandler(IRoomQueries roomQueries, IChatQueries chatQueries)
    : IQueryHandler<GetChatHistoryQuery, Result<IReadOnlyList<ChatMessageDto>>>
{
    private const int MaxLimit = 100;

    public async Task<Result<IReadOnlyList<ChatMessageDto>>> Handle(GetChatHistoryQuery query, CancellationToken cancellationToken)
    {
        if (!await roomQueries.IsActiveMemberAsync(query.RoomId, query.UserId, cancellationToken))
        {
            return DomainErrors.Rooms.NotMember;
        }

        var limit = Math.Clamp(query.Limit <= 0 ? 50 : query.Limit, 1, MaxLimit);
        var history = await chatQueries.GetHistoryAsync(query.RoomId, query.Before, limit, cancellationToken);
        return Result.Success(history);
    }
}
