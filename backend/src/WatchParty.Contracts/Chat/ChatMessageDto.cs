namespace WatchParty.Contracts.Chat;

public sealed record ChatMessageDto(
    Guid Id,
    Guid RoomId,
    Guid SenderUserId,
    string SenderDisplayName,
    string? SenderAvatarUrl,
    string Content,
    bool IsDeleted,
    DateTimeOffset CreatedAtUtc);
