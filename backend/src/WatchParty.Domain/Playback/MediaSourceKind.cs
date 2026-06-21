namespace WatchParty.Domain.Playback;

/// <summary>
/// Supported, legally-permitted media source kinds for V1. No DRM / paid
/// streaming services (those are explicitly out of scope).
/// </summary>
public enum MediaSourceKind
{
    /// <summary>A direct progressive file URL (e.g. .mp4).</summary>
    Direct = 0,

    /// <summary>An HLS playlist URL (.m3u8).</summary>
    Hls = 1,

    /// <summary>A YouTube video, played via the embedded player (no download).</summary>
    YouTube = 2,

    /// <summary>A file the user owns in Google Drive, streamed with their authorization.</summary>
    GoogleDrive = 3,

    /// <summary>A file the user owns in MEGA, streamed via MEGA's embedded player (the decryption key travels in the link the user supplies).</summary>
    Mega = 4
}
