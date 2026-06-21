using WatchParty.Contracts.Chat;

namespace WatchParty.Application.Abstractions.Persistence;

public interface IChatQueries
{
    /// <summary>Most recent messages before <paramref name="before"/> (descending then reversed by caller).</summary>
    Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(Guid roomId, DateTimeOffset? before, int limit, CancellationToken cancellationToken);
    Task<ChatMessageDto?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken);
    Task<long> CountMessagesAsync(CancellationToken cancellationToken);
}
