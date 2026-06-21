using WatchParty.Contracts.Playback;

namespace WatchParty.Contracts.Rooms;

/// <summary>Full room view returned when entering a room: members + current playback.</summary>
public sealed record RoomDetailDto(
    RoomDto Room,
    IReadOnlyList<RoomMemberDto> Members,
    MediaDto? CurrentMedia,
    PlaybackStateDto? Playback);
