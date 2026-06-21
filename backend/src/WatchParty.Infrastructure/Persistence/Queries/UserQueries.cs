using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Users;
using WatchParty.Contracts.Admin;
using WatchParty.Contracts.Common;
using WatchParty.Contracts.Users;
using WatchParty.Domain.Identity;

namespace WatchParty.Infrastructure.Persistence.Queries;

public sealed class UserQueries(WatchPartyDbContext dbContext) : IUserQueries
{
    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user?.ToProfileDto();
    }

    public async Task<PublicUserDto?> GetPublicAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user?.ToPublicDto();
    }

    public async Task<IReadOnlyList<PublicUserDto>> GetBlockedUsersAsync(Guid userId, CancellationToken cancellationToken)
    {
        var users = await (
            from block in dbContext.UserBlocks.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on block.BlockedUserId equals user.Id
            where block.BlockerUserId == userId
            orderby block.CreatedAtUtc descending
            select user).ToListAsync(cancellationToken);

        return users.Select(u => u.ToPublicDto()).ToList();
    }

    public async Task<PagedResult<AdminUserDto>> SearchUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var emailMatch = Email.Create(term);
            query = emailMatch.IsSuccess
                ? query.Where(u => u.DisplayName.Contains(term) || u.Email == emailMatch.Value)
                : query.Where(u => u.DisplayName.Contains(term));
        }

        var total = await query.LongCountAsync(cancellationToken);
        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = users.Select(ToAdminDto).ToList();
        return new PagedResult<AdminUserDto>(items, page, pageSize, total);
    }

    public async Task<AdminUserDetailDto?> GetAdminUserDetailAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roomsCreated = await dbContext.Rooms.AsNoTracking().CountAsync(r => r.HostUserId == userId, cancellationToken);
        var messagesSent = await dbContext.ChatMessages.AsNoTracking().CountAsync(m => m.SenderUserId == userId, cancellationToken);
        var reportsAgainst = await dbContext.Reports.AsNoTracking().CountAsync(r => r.TargetUserId == userId, cancellationToken);

        return new AdminUserDetailDto(ToAdminDto(user), user.BlockedReason, roomsCreated, messagesSent, reportsAgainst);
    }

    public Task<long> CountUsersAsync(CancellationToken cancellationToken) =>
        dbContext.Users.AsNoTracking().LongCountAsync(cancellationToken);

    private static AdminUserDto ToAdminDto(User user) => new(
        user.Id,
        user.Email.Value,
        user.DisplayName,
        user.Role.ToString(),
        user.IsBlocked,
        user.EmailConfirmed,
        user.CreatedAtUtc,
        user.LastLoginAtUtc);
}
