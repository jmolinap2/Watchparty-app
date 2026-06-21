using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Chat;

namespace WatchParty.Infrastructure.Persistence.Queries;

public sealed class ChatQueries(WatchPartyDbContext dbContext) : IChatQueries
{
    public async Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(Guid roomId, DateTimeOffset? before, int limit, CancellationToken cancellationToken)
    {
        var query = dbContext.ChatMessages.AsNoTracking().Where(m => m.RoomId == roomId);
        if (before.HasValue)
        {
            query = query.Where(m => m.CreatedAtUtc < before.Value);
        }

        var rows = await (
            from message in query
            join user in dbContext.Users.AsNoTracking() on message.SenderUserId equals user.Id
            orderby message.CreatedAtUtc descending
            select new
            {
                message.Id,
                message.RoomId,
                message.SenderUserId,
                user.DisplayName,
                user.AvatarUrl,
                message.Content,
                message.IsDeleted,
                message.CreatedAtUtc
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        // Returned oldest-first for natural rendering.
        rows.Reverse();
        return rows
            .Select(r => new ChatMessageDto(
                r.Id, r.RoomId, r.SenderUserId, r.DisplayName, r.AvatarUrl,
                r.IsDeleted ? string.Empty : r.Content, r.IsDeleted, r.CreatedAtUtc))
            .ToList();
    }

    public async Task<ChatMessageDto?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var row = await (
            from message in dbContext.ChatMessages.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on message.SenderUserId equals user.Id
            where message.Id == messageId
            select new
            {
                message.Id,
                message.RoomId,
                message.SenderUserId,
                user.DisplayName,
                user.AvatarUrl,
                message.Content,
                message.IsDeleted,
                message.CreatedAtUtc
            }).FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new ChatMessageDto(
            row.Id, row.RoomId, row.SenderUserId, row.DisplayName, row.AvatarUrl,
            row.IsDeleted ? string.Empty : row.Content, row.IsDeleted, row.CreatedAtUtc);
    }

    public Task<long> CountMessagesAsync(CancellationToken cancellationToken) =>
        dbContext.ChatMessages.AsNoTracking().LongCountAsync(cancellationToken);
}
