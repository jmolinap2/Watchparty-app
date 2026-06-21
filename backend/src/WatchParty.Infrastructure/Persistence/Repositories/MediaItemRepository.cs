using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Playback;

namespace WatchParty.Infrastructure.Persistence.Repositories;

public sealed class MediaItemRepository(WatchPartyDbContext dbContext) : IMediaItemRepository
{
    public Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.MediaItems.FirstOrDefaultAsync(media => media.Id == id, cancellationToken);

    public async Task AddAsync(MediaItem mediaItem, CancellationToken cancellationToken) =>
        await dbContext.MediaItems.AddAsync(mediaItem, cancellationToken);
}
