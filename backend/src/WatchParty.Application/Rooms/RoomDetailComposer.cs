using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.State;
using WatchParty.Application.Playback;
using WatchParty.Contracts.Rooms;

namespace WatchParty.Application.Rooms;

/// <summary>
/// Builds a <see cref="RoomDetailDto"/> by combining the persisted room/members
/// (PostgreSQL) with live presence and playback state (Redis). Shared by the
/// join and get-detail use cases.
/// </summary>
public sealed class RoomDetailComposer(
    IRoomQueries roomQueries,
    IPresenceStore presenceStore,
    IPlaybackStateStore playbackStateStore)
{
    public async Task<RoomDetailDto?> ComposeAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await roomQueries.GetRoomAsync(roomId, cancellationToken);
        if (room is null)
        {
            return null;
        }

        var onlineIds = await presenceStore.GetOnlineUserIdsAsync(roomId, cancellationToken);
        var onlineSet = onlineIds.ToHashSet();

        var members = (await roomQueries.GetActiveMembersAsync(roomId, cancellationToken))
            .Select(m => m with { IsOnline = onlineSet.Contains(m.UserId) })
            .ToList();

        var media = await roomQueries.GetCurrentMediaAsync(roomId, cancellationToken);
        var playbackState = await playbackStateStore.GetAsync(roomId, cancellationToken);

        var roomDto = room with { OnlineCount = onlineSet.Count };
        return new RoomDetailDto(roomDto, members, media, playbackState?.ToDto());
    }
}
