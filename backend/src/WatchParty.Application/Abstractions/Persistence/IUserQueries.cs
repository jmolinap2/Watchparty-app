using WatchParty.Contracts.Admin;
using WatchParty.Contracts.Common;
using WatchParty.Contracts.Users;

namespace WatchParty.Application.Abstractions.Persistence;

/// <summary>Read-side projections for users (returns DTOs directly, no aggregates).</summary>
public interface IUserQueries
{
    Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<PublicUserDto?> GetPublicAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicUserDto>> GetBlockedUsersAsync(Guid userId, CancellationToken cancellationToken);

    // Admin
    Task<PagedResult<AdminUserDto>> SearchUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<AdminUserDetailDto?> GetAdminUserDetailAsync(Guid userId, CancellationToken cancellationToken);
    Task<long> CountUsersAsync(CancellationToken cancellationToken);
}
