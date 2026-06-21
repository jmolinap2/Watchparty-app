namespace WatchParty.Contracts.Playback;

/// <summary>
/// Load/replace the room's media. The server resolves the kind (mp4/hls/youtube/
/// drive) from <see cref="Url"/> and validates it against the allow-list.
/// </summary>
public sealed record ChangeMediaRequest(string Url, string? Title);

public sealed record SeekRequest(double PositionSeconds);
