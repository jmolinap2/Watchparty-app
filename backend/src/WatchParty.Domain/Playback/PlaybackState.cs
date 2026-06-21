using WatchParty.Domain.Common;

namespace WatchParty.Domain.Playback;

/// <summary>
/// The server-authoritative playback state for a room (architecture §12).
/// Immutable: every transition returns a new state with an incremented
/// <see cref="Version"/> and a fresh <see cref="ServerTimestampUtc"/>. Clients
/// must ignore any event whose version is not newer than the last one applied.
/// </summary>
public sealed class PlaybackState
{
    private PlaybackState(
        Guid roomId,
        Guid? mediaId,
        PlaybackStatus status,
        double positionSeconds,
        DateTimeOffset serverTimestampUtc,
        long version,
        Guid updatedByUserId)
    {
        RoomId = roomId;
        MediaId = mediaId;
        Status = status;
        PositionSeconds = positionSeconds;
        ServerTimestampUtc = serverTimestampUtc;
        Version = version;
        UpdatedByUserId = updatedByUserId;
    }

    public Guid RoomId { get; }
    public Guid? MediaId { get; }
    public PlaybackStatus Status { get; }

    /// <summary>Anchor position. Effective position while Playing advances with the wall clock.</summary>
    public double PositionSeconds { get; }
    public DateTimeOffset ServerTimestampUtc { get; }
    public long Version { get; }
    public Guid UpdatedByUserId { get; }

    /// <summary>Empty state for a room that has no media loaded yet.</summary>
    public static PlaybackState Empty(Guid roomId) =>
        new(roomId, null, PlaybackStatus.Idle, 0, DateTimeOffset.UtcNow, 0, Guid.Empty);

    /// <summary>State produced when a new media item is loaded: paused at the start.</summary>
    public PlaybackState WithMedia(Guid mediaId, Guid byUserId, DateTimeOffset now) =>
        new(RoomId, mediaId, PlaybackStatus.Paused, 0, now, Version + 1, byUserId);

    public PlaybackState Play(Guid byUserId, DateTimeOffset now)
    {
        EnsureMedia();
        var position = EffectivePositionAt(now);
        return new PlaybackState(RoomId, MediaId, PlaybackStatus.Playing, position, now, Version + 1, byUserId);
    }

    public PlaybackState Pause(Guid byUserId, DateTimeOffset now)
    {
        EnsureMedia();
        var position = EffectivePositionAt(now);
        return new PlaybackState(RoomId, MediaId, PlaybackStatus.Paused, position, now, Version + 1, byUserId);
    }

    public PlaybackState Seek(Guid byUserId, double positionSeconds, DateTimeOffset now)
    {
        EnsureMedia();
        var clamped = Math.Max(0, positionSeconds);
        // Keep current play/pause status; just re-anchor at the new position.
        return new PlaybackState(RoomId, MediaId, Status, clamped, now, Version + 1, byUserId);
    }

    public PlaybackState Ended(Guid byUserId, DateTimeOffset now)
    {
        EnsureMedia();
        return new PlaybackState(RoomId, MediaId, PlaybackStatus.Ended, EffectivePositionAt(now), now, Version + 1, byUserId);
    }

    /// <summary>
    /// The position a client should be at right now, accounting for elapsed wall
    /// time while playing. Used when a late joiner / reconnecting client recovers state.
    /// </summary>
    public double EffectivePositionAt(DateTimeOffset now)
    {
        if (Status != PlaybackStatus.Playing)
        {
            return PositionSeconds;
        }

        var elapsed = (now - ServerTimestampUtc).TotalSeconds;
        return PositionSeconds + Math.Max(0, elapsed);
    }

    /// <summary>Rehydrate persisted state (e.g. from Redis) without a transition.</summary>
    public static PlaybackState FromTrusted(
        Guid roomId,
        Guid? mediaId,
        PlaybackStatus status,
        double positionSeconds,
        DateTimeOffset serverTimestampUtc,
        long version,
        Guid updatedByUserId) =>
        new(roomId, mediaId, status, positionSeconds, serverTimestampUtc, version, updatedByUserId);

    private void EnsureMedia()
    {
        if (MediaId is null)
        {
            throw new DomainException(DomainErrors.Playback.NoMediaLoaded);
        }
    }
}
