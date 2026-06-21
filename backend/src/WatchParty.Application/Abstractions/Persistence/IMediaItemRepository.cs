using WatchParty.Domain.Playback;

namespace WatchParty.Application.Abstractions.Persistence;

public interface IMediaItemRepository
{
    Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(MediaItem mediaItem, CancellationToken cancellationToken);
}
