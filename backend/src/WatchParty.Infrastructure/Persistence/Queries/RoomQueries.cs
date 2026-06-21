using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Playback;
using WatchParty.Application.Rooms;
using WatchParty.Contracts.Admin;
using WatchParty.Contracts.Common;
using WatchParty.Contracts.Playback;
using WatchParty.Contracts.Rooms;
using WatchParty.Domain.Rooms;

namespace WatchParty.Infrastructure.Persistence.Queries;

public sealed class RoomQueries(WatchPartyDbContext dbContext) : IRoomQueries
{
    public Task<bool> IsActiveMemberAsync(Guid roomId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.RoomMembers.AsNoTracking()
            .AnyAsync(m => m.RoomId == roomId && m.UserId == userId && m.LeftAtUtc == null, cancellationToken);

    public async Task<RoomDto?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await dbContext.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);
        return room?.ToDto(onlineCount: 0);
    }

    public async Task<RoomDto?> GetRoomByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var roomCode = RoomCode.FromTrusted(code);
        var room = await dbContext.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Code == roomCode, cancellationToken);
        return room?.ToDto(onlineCount: 0);
    }

    public async Task<IReadOnlyList<RoomMemberDto>> GetActiveMembersAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var rows = await (
            from member in dbContext.RoomMembers.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on member.UserId equals user.Id
            where member.RoomId == roomId && member.LeftAtUtc == null
            orderby member.JoinedAtUtc
            select new { member.UserId, user.DisplayName, user.AvatarUrl, member.Role, member.JoinedAtUtc })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new RoomMemberDto(r.UserId, r.DisplayName, r.AvatarUrl, r.Role.ToString(), false, r.JoinedAtUtc))
            .ToList();
    }

    public async Task<MediaDto?> GetMediaAsync(Guid mediaId, CancellationToken cancellationToken)
    {
        var media = await dbContext.MediaItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == mediaId, cancellationToken);
        return media?.ToDto();
    }

    public async Task<MediaDto?> GetCurrentMediaAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var currentMediaId = await dbContext.Rooms.AsNoTracking()
            .Where(r => r.Id == roomId)
            .Select(r => r.CurrentMediaId)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentMediaId is null)
        {
            return null;
        }

        return await GetMediaAsync(currentMediaId.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<RoomHistoryItemDto>> GetUserRoomHistoryAsync(Guid userId, int limit, CancellationToken cancellationToken)
    {
        var rows = await (
            from member in dbContext.RoomMembers.AsNoTracking()
            join room in dbContext.Rooms.AsNoTracking() on member.RoomId equals room.Id
            where member.UserId == userId
            orderby member.JoinedAtUtc descending
            select new { room.Id, room.Code, room.Name, member.Role, room.Status, member.JoinedAtUtc, member.LeftAtUtc })
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new RoomHistoryItemDto(r.Id, r.Code.Value, r.Name, r.Role.ToString(), r.Status.ToString(), r.JoinedAtUtc, r.LeftAtUtc))
            .ToList();
    }

    public async Task<PagedResult<AdminRoomDto>> GetRoomsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Rooms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RoomStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(r => r.Status == parsed);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var rooms = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var roomIds = rooms.Select(r => r.Id).ToList();
        var memberCounts = await dbContext.RoomMembers.AsNoTracking()
            .Where(m => roomIds.Contains(m.RoomId) && m.LeftAtUtc == null)
            .GroupBy(m => m.RoomId)
            .Select(g => new { RoomId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoomId, x => x.Count, cancellationToken);

        var items = rooms.Select(r => ToAdminDto(r, memberCounts.GetValueOrDefault(r.Id))).ToList();
        return new PagedResult<AdminRoomDto>(items, page, pageSize, total);
    }

    public async Task<AdminRoomDto?> GetAdminRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await dbContext.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);
        if (room is null)
        {
            return null;
        }

        var memberCount = await dbContext.RoomMembers.AsNoTracking()
            .CountAsync(m => m.RoomId == roomId && m.LeftAtUtc == null, cancellationToken);
        return ToAdminDto(room, memberCount);
    }

    public Task<long> CountRoomsAsync(CancellationToken cancellationToken) =>
        dbContext.Rooms.AsNoTracking().LongCountAsync(cancellationToken);

    private static AdminRoomDto ToAdminDto(Room room, int memberCount) => new(
        room.Id,
        room.Code.Value,
        room.Name,
        room.HostUserId,
        room.Status.ToString(),
        memberCount,
        0,
        room.CreatedAtUtc,
        room.ClosedAtUtc);
}
