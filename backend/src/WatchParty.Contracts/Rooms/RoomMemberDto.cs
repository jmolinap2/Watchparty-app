namespace WatchParty.Contracts.Rooms;

public sealed record RoomMemberDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    bool IsOnline,
    DateTimeOffset JoinedAtUtc);
