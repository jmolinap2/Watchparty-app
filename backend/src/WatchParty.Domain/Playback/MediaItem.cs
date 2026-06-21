using WatchParty.Domain.Common;

namespace WatchParty.Domain.Playback;

/// <summary>
/// A media item that has been loaded into a room. Persisted so room history can
/// list previously played videos.
/// </summary>
public sealed class MediaItem : AggregateRoot
{
    public const int MaxTitleLength = 200;

    private MediaItem()
    {
    }

    private MediaItem(Guid id, Guid roomId, MediaSource source, string title, Guid addedByUserId) : base(id)
    {
        RoomId = roomId;
        Source = source;
        Title = title;
        AddedByUserId = addedByUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid RoomId { get; private set; }
    public MediaSource Source { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public Guid AddedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static MediaItem Create(Guid roomId, MediaSource source, string? title, Guid addedByUserId)
    {
        var safeTitle = string.IsNullOrWhiteSpace(title)
            ? DeriveTitle(source)
            : title.Trim();

        if (safeTitle.Length > MaxTitleLength)
        {
            safeTitle = safeTitle[..MaxTitleLength];
        }

        return new MediaItem(Guid.NewGuid(), roomId, source, safeTitle, addedByUserId);
    }

    private static string DeriveTitle(MediaSource source) => source.Kind switch
    {
        MediaSourceKind.YouTube => "YouTube video",
        MediaSourceKind.GoogleDrive => "Google Drive video",
        MediaSourceKind.Mega => "MEGA video",
        _ => "Video"
    };
}
