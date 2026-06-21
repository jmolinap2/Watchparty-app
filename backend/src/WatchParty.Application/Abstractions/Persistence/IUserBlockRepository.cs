using WatchParty.Domain.Users;

namespace WatchParty.Application.Abstractions.Persistence;

public interface IUserBlockRepository
{
    Task<UserBlock?> GetAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken);
    Task AddAsync(UserBlock block, CancellationToken cancellationToken);
    void Remove(UserBlock block);
}
