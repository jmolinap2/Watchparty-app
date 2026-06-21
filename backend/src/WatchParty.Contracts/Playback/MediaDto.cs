namespace WatchParty.Contracts.Playback;

/// <summary>
/// A media item as seen by clients. <see cref="Kind"/> tells the client which
/// player to use: "Direct" (mp4), "Hls" (m3u8), "YouTube", "GoogleDrive", "Mega".
/// <see cref="ProviderId"/> is the YouTube video id, Drive file id or MEGA file id when
/// relevant. For "Mega" the <see cref="Url"/> is the embed URL and carries the decryption key.
/// </summary>
public sealed record MediaDto(
    Guid Id,
    string Kind,
    string Url,
    string? ProviderId,
    string Title,
    Guid AddedByUserId,
    DateTimeOffset CreatedAtUtc);
