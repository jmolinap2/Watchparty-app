using WatchParty.Contracts.Playback;
using WatchParty.Domain.Playback;

namespace WatchParty.Application.Playback;

public static class PlaybackMappings
{
    public static PlaybackStateDto ToDto(this PlaybackState state) => new(
        state.RoomId,
        state.MediaId,
        state.Status.ToString(),
        state.PositionSeconds,
        state.ServerTimestampUtc,
        state.Version,
        state.UpdatedByUserId);

    public static MediaDto ToDto(this MediaItem media) => new(
        media.Id,
        media.Source.Kind.ToString(),
        media.Source.Url,
        media.Source.ProviderId,
        media.Title,
        media.AddedByUserId,
        media.CreatedAtUtc);
}
