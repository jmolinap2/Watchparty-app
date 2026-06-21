using WatchParty.Domain.Common;

namespace WatchParty.Domain.Playback;

/// <summary>
/// A validated, playable media reference. Structural only — security policy
/// (HTTPS, allowed domain, format) is enforced by the application validator
/// before one of these is constructed.
/// </summary>
public sealed class MediaSource : ValueObject
{
    private MediaSource(MediaSourceKind kind, string url, string? originalUrl, string? providerId)
    {
        Kind = kind;
        Url = url;
        OriginalUrl = originalUrl;
        ProviderId = providerId;
    }

    public MediaSourceKind Kind { get; }

    /// <summary>Canonical URL the player consumes (for Direct/Hls). May equal <see cref="OriginalUrl"/>.</summary>
    public string Url { get; }

    /// <summary>The URL the user originally supplied (kept for display/audit).</summary>
    public string? OriginalUrl { get; }

    /// <summary>YouTube video id or Google Drive file id, when applicable.</summary>
    public string? ProviderId { get; }

    public static MediaSource Direct(string url) => new(MediaSourceKind.Direct, url, url, null);

    public static MediaSource Hls(string url) => new(MediaSourceKind.Hls, url, url, null);

    public static MediaSource YouTube(string videoId, string? originalUrl) =>
        new(MediaSourceKind.YouTube, $"https://www.youtube.com/watch?v={videoId}", originalUrl, videoId);

    public static MediaSource GoogleDrive(string fileId, string? originalUrl) =>
        new(MediaSourceKind.GoogleDrive, $"https://drive.google.com/file/d/{fileId}/view", originalUrl, fileId);

    /// <summary>
    /// A MEGA file. The canonical <see cref="Url"/> is MEGA's embed URL and carries the
    /// decryption key in its fragment (required to play); <see cref="ProviderId"/> holds the file id.
    /// </summary>
    public static MediaSource Mega(string fileId, string key, string? originalUrl) =>
        new(MediaSourceKind.Mega, $"https://mega.nz/embed/{fileId}#{key}", originalUrl, fileId);

    /// <summary>Rehydrate a persisted source without re-validating.</summary>
    public static MediaSource FromTrusted(MediaSourceKind kind, string url, string? originalUrl, string? providerId) =>
        new(kind, url, originalUrl, providerId);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return Url;
        yield return ProviderId;
    }
}
