namespace WatchParty.Contracts.Playback;

/// <summary>
/// The authoritative playback state sent to clients (architecture §12). Clients
/// ignore any state whose <see cref="Version"/> is not greater than the last applied.
/// </summary>
public sealed record PlaybackStateDto(
    Guid RoomId,
    Guid? MediaId,
    string Status,
    double PositionSeconds,
    DateTimeOffset ServerTimestampUtc,
    long Version,
    Guid UpdatedByUserId);
