using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Chat;

namespace WatchParty.Infrastructure.Persistence.Repositories;

public sealed class ChatMessageRepository(WatchPartyDbContext dbContext) : IChatMessageRepository
{
    public Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.ChatMessages.FirstOrDefaultAsync(message => message.Id == id, cancellationToken);

    public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken) =>
        await dbContext.ChatMessages.AddAsync(message, cancellationToken);

    public void Update(ChatMessage message) => dbContext.ChatMessages.Update(message);
}
