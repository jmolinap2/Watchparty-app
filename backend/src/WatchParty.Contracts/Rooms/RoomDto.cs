namespace WatchParty.Contracts.Rooms;

/// <summary>Summary view of a room (lists, create/join responses).</summary>
public sealed record RoomDto(
    Guid Id,
    string Code,
    string Name,
    Guid HostUserId,
    bool IsPrivate,
    int MaxMembers,
    string Status,
    int OnlineCount,
    DateTimeOffset CreatedAtUtc);
