using WatchParty.Domain.Common;

namespace WatchParty.Domain.Rooms;

/// <summary>
/// Persistent membership of a user in a room (not the live connection — presence
/// lives in Redis). A member who leaves keeps a row with <see cref="LeftAtUtc"/> set.
/// </summary>
public sealed class RoomMember : Entity
{
    private RoomMember()
    {
    }

    internal RoomMember(Guid id, Guid roomId, Guid userId, RoomRole role) : base(id)
    {
        RoomId = roomId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid RoomId { get; private set; }
    public Guid UserId { get; private set; }
    public RoomRole Role { get; private set; }
    public DateTimeOffset JoinedAtUtc { get; private set; }
    public DateTimeOffset? LeftAtUtc { get; private set; }
    public bool WasKicked { get; private set; }

    public bool IsActive => LeftAtUtc is null;

    internal void Rejoin()
    {
        LeftAtUtc = null;
        WasKicked = false;
        JoinedAtUtc = DateTimeOffset.UtcNow;
    }

    internal void MarkLeft(bool kicked = false)
    {
        LeftAtUtc = DateTimeOffset.UtcNow;
        WasKicked = kicked;
    }

    internal void SetRole(RoomRole role) => Role = role;
}
