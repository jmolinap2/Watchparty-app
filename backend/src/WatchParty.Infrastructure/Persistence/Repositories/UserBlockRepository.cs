using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Users;

namespace WatchParty.Infrastructure.Persistence.Repositories;

public sealed class UserBlockRepository(WatchPartyDbContext dbContext) : IUserBlockRepository
{
    public Task<UserBlock?> GetAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
        dbContext.UserBlocks.FirstOrDefaultAsync(
            block => block.BlockerUserId == blockerUserId && block.BlockedUserId == blockedUserId,
            cancellationToken);

    public Task<bool> ExistsAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
        dbContext.UserBlocks.AnyAsync(
            block => block.BlockerUserId == blockerUserId && block.BlockedUserId == blockedUserId,
            cancellationToken);

    public async Task AddAsync(UserBlock block, CancellationToken cancellationToken) =>
        await dbContext.UserBlocks.AddAsync(block, cancellationToken);

    public void Remove(UserBlock block) => dbContext.UserBlocks.Remove(block);
}
