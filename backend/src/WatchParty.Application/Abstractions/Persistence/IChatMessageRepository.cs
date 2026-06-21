using WatchParty.Domain.Chat;

namespace WatchParty.Application.Abstractions.Persistence;

public interface IChatMessageRepository
{
    Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken);
    void Update(ChatMessage message);
}
