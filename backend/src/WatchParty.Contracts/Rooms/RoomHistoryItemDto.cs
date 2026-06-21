namespace WatchParty.Contracts.Rooms;

/// <summary>An entry in a user's room history (architecture / scope: "Historial de salas").</summary>
public sealed record RoomHistoryItemDto(
    Guid RoomId,
    string Code,
    string Name,
    string Role,
    string Status,
    DateTimeOffset JoinedAtUtc,
    DateTimeOffset? LeftAtUtc);
