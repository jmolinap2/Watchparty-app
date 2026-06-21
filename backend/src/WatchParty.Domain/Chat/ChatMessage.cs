using WatchParty.Domain.Common;

namespace WatchParty.Domain.Chat;

/// <summary>A chat message within a room. Soft-deleted so moderation keeps an audit trail.</summary>
public sealed class ChatMessage : AggregateRoot
{
    public const int MaxLength = 2000;

    private ChatMessage()
    {
    }

    private ChatMessage(Guid id, Guid roomId, Guid senderUserId, string content) : base(id)
    {
        RoomId = roomId;
        SenderUserId = senderUserId;
        Content = content;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid RoomId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Content { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public Guid? DeletedByUserId { get; private set; }

    public static Result<ChatMessage> Create(Guid roomId, Guid senderUserId, string? content)
    {
        var trimmed = content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return DomainErrors.Chat.MessageEmpty;
        }

        if (trimmed.Length > MaxLength)
        {
            return DomainErrors.Chat.MessageTooLong;
        }

        return new ChatMessage(Guid.NewGuid(), roomId, senderUserId, trimmed);
    }

    /// <summary>
    /// Delete the message. The sender may delete their own; a moderator (room host
    /// or admin) may delete any. The role decision is made by the use case.
    /// </summary>
    public Result Delete(Guid byUserId, bool asModerator)
    {
        if (IsDeleted)
        {
            return DomainErrors.Chat.AlreadyDeleted;
        }

        if (!asModerator && byUserId != SenderUserId)
        {
            return DomainErrors.Chat.CannotDeleteOthers;
        }

        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
        DeletedByUserId = byUserId;
        return Result.Success();
    }
}
