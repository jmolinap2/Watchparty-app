namespace WatchParty.Application.Common;

/// <summary>
/// Media source policy, bound from configuration (section <see cref="SectionName"/>).
/// Lets an operator add or remove recognised providers, hosts and direct/HLS formats
/// without code changes. The values below are safe defaults for V1.
/// </summary>
public sealed class MediaSourceOptions
{
    public const string SectionName = "Media";

    /// <summary>Hosts recognised as YouTube (played via the embedded IFrame API).</summary>
    public string[] YouTubeHosts { get; set; } =
        ["youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com", "youtu.be"];

    /// <summary>Hosts recognised as Google Drive (streamed via the preview embed).</summary>
    public string[] GoogleDriveHosts { get; set; } =
        ["drive.google.com", "docs.google.com"];

    /// <summary>Hosts recognised as MEGA (streamed via the MEGA embed player).</summary>
    public string[] MegaHosts { get; set; } =
        ["mega.nz", "www.mega.nz", "mega.io", "mega.co.nz"];

    /// <summary>File extensions treated as direct progressive video.</summary>
    public string[] DirectExtensions { get; set; } =
        [".mp4", ".m4v", ".webm", ".mov", ".ogg"];

    /// <summary>File extensions treated as HLS playlists.</summary>
    public string[] HlsExtensions { get; set; } = [".m3u8"];
}
