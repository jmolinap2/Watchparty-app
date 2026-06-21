using WatchParty.Application.Abstractions;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.State;
using WatchParty.Contracts.Playback;
using WatchParty.Domain.Common;
using WatchParty.Domain.Playback;

namespace WatchParty.Application.Playback;

/// <summary>
/// Returns the authoritative playback state, used by clients to recover after a
/// reconnect (architecture §12). If the live state has expired from Redis but the
/// room still has media, it is rebuilt as paused at the start.
/// </summary>
public sealed record GetPlaybackStateQuery(Guid RoomId, Guid UserId) : IQuery<Result<PlaybackStateDto>>;

public sealed class GetPlaybackStateQueryHandler(
    IRoomQueries roomQueries,
    IPlaybackStateStore store,
    IClock clock)
    : IQueryHandler<GetPlaybackStateQuery, Result<PlaybackStateDto>>
{
    public async Task<Result<PlaybackStateDto>> Handle(GetPlaybackStateQuery query, CancellationToken cancellationToken)
    {
        if (!await roomQueries.IsActiveMemberAsync(query.RoomId, query.UserId, cancellationToken))
        {
            return DomainErrors.Rooms.NotMember;
        }

        var state = await store.GetAsync(query.RoomId, cancellationToken);
        if (state is not null)
        {
            return state.ToDto();
        }

        // Live state expired: rebuild from the persisted current media, if any.
        var media = await roomQueries.GetCurrentMediaAsync(query.RoomId, cancellationToken);
        if (media is null)
        {
            return PlaybackState.Empty(query.RoomId).ToDto();
        }

        var rebuilt = PlaybackState.Empty(query.RoomId).WithMedia(media.Id, Guid.Empty, clock.UtcNow);
        await store.SaveAsync(rebuilt, cancellationToken);
        return rebuilt.ToDto();
    }
}
