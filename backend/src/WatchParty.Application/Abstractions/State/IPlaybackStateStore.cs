using WatchParty.Domain.Playback;

namespace WatchParty.Application.Abstractions.State;

/// <summary>
/// Holds the live, authoritative playback state per room. Backed by Redis: it can
/// be rebuilt from PostgreSQL, so it is allowed to expire (architecture §13).
/// </summary>
public interface IPlaybackStateStore
{
    Task<PlaybackState?> GetAsync(Guid roomId, CancellationToken cancellationToken);
    Task SaveAsync(PlaybackState state, CancellationToken cancellationToken);
    Task RemoveAsync(Guid roomId, CancellationToken cancellationToken);
}
