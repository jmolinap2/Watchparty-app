using WatchParty.Domain.Common;
using WatchParty.Domain.Rooms.Events;

namespace WatchParty.Domain.Rooms;

/// <summary>
/// A watch room. Always has exactly one host (invariant). Enforces membership,
/// capacity, host transfer and kick rules. Live playback/presence state lives
/// outside the aggregate (Redis).
/// </summary>
public sealed class Room : AggregateRoot
{
    public const int MaxNameLength = 60;

    private readonly List<RoomMember> _members = [];

    private Room()
    {
    }

    private Room(Guid id, string name, RoomCode code, Guid hostUserId, RoomSettings settings) : base(id)
    {
        Name = name;
        Code = code;
        HostUserId = hostUserId;
        Settings = settings;
        Status = RoomStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public string Name { get; private set; } = null!;
    public RoomCode Code { get; private set; } = null!;
    public Guid HostUserId { get; private set; }
    public RoomSettings Settings { get; private set; } = null!;
    public RoomStatus Status { get; private set; }
    public Guid? CurrentMediaId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public IReadOnlyCollection<RoomMember> Members => _members.AsReadOnly();
    public IEnumerable<RoomMember> ActiveMembers => _members.Where(m => m.IsActive);
    public bool IsActive => Status == RoomStatus.Active;

    public static Result<Room> Create(string name, Guid hostUserId, RoomCode code, RoomSettings settings)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return DomainErrors.Rooms.NameRequired;
        }

        if (trimmed.Length > MaxNameLength)
        {
            return DomainErrors.Rooms.NameTooLong;
        }

        var room = new Room(Guid.NewGuid(), trimmed, code, hostUserId, settings);
        room._members.Add(new RoomMember(Guid.NewGuid(), room.Id, hostUserId, RoomRole.Host));
        room.Raise(new RoomCreatedDomainEvent(room.Id, hostUserId, code.Value));
        return room;
    }

    public Result Join(Guid userId)
    {
        if (!IsActive)
        {
            return DomainErrors.Rooms.Closed;
        }

        var existing = _members.FirstOrDefault(m => m.UserId == userId);
        if (existing is not null && existing.IsActive)
        {
            return DomainErrors.Rooms.AlreadyMember;
        }

        if (ActiveMembers.Count() >= Settings.MaxMembers)
        {
            return DomainErrors.Rooms.Full;
        }

        if (existing is not null)
        {
            existing.Rejoin();
        }
        else
        {
            _members.Add(new RoomMember(Guid.NewGuid(), Id, userId, RoomRole.Member));
        }

        return Result.Success();
    }

    public Result Leave(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId && m.IsActive);
        if (member is null)
        {
            return DomainErrors.Rooms.NotMember;
        }

        if (member.UserId == HostUserId)
        {
            // The host may only leave when alone; otherwise transfer or close (invariant: a room always has a host).
            if (ActiveMembers.Count(m => m.UserId != HostUserId) > 0)
            {
                return DomainErrors.Rooms.HostCannotLeave;
            }

            member.MarkLeft();
            CloseInternal(userId);
            return Result.Success();
        }

        member.MarkLeft();
        return Result.Success();
    }

    public Result Close(Guid byUserId)
    {
        if (byUserId != HostUserId)
        {
            return DomainErrors.Rooms.NotHost;
        }

        if (!IsActive)
        {
            return DomainErrors.Rooms.Closed;
        }

        foreach (var member in ActiveMembers.ToList())
        {
            member.MarkLeft();
        }

        CloseInternal(byUserId);
        return Result.Success();
    }

    public Result TransferHost(Guid fromUserId, Guid toUserId)
    {
        if (!IsActive)
        {
            return DomainErrors.Rooms.Closed;
        }

        if (fromUserId != HostUserId)
        {
            return DomainErrors.Rooms.NotHost;
        }

        var target = _members.FirstOrDefault(m => m.UserId == toUserId && m.IsActive);
        if (target is null)
        {
            return DomainErrors.Rooms.TargetNotMember;
        }

        var currentHost = _members.FirstOrDefault(m => m.UserId == fromUserId);
        currentHost?.SetRole(RoomRole.Member);
        target.SetRole(RoomRole.Host);
        HostUserId = toUserId;

        Raise(new HostTransferredDomainEvent(Id, fromUserId, toUserId));
        return Result.Success();
    }

    public Result Kick(Guid byUserId, Guid targetUserId)
    {
        if (!IsActive)
        {
            return DomainErrors.Rooms.Closed;
        }

        if (byUserId != HostUserId)
        {
            return DomainErrors.Rooms.NotHost;
        }

        if (targetUserId == byUserId)
        {
            return DomainErrors.Rooms.CannotKickSelf;
        }

        if (targetUserId == HostUserId)
        {
            return DomainErrors.Rooms.CannotKickHost;
        }

        var target = _members.FirstOrDefault(m => m.UserId == targetUserId && m.IsActive);
        if (target is null)
        {
            return DomainErrors.Rooms.TargetNotMember;
        }

        target.MarkLeft(kicked: true);
        Raise(new MemberKickedDomainEvent(Id, targetUserId, byUserId));
        return Result.Success();
    }

    /// <summary>Force-close performed by a global admin (bypasses the host check). Audited by the use case.</summary>
    public Result ForceCloseByAdmin(Guid adminUserId)
    {
        if (!IsActive)
        {
            return DomainErrors.Rooms.Closed;
        }

        foreach (var member in ActiveMembers.ToList())
        {
            member.MarkLeft();
        }

        CloseInternal(adminUserId);
        return Result.Success();
    }

    public void SetCurrentMedia(Guid? mediaId) => CurrentMediaId = mediaId;

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId && m.IsActive);

    public bool IsHost(Guid userId) => IsActive && HostUserId == userId;

    private void CloseInternal(Guid byUserId)
    {
        Status = RoomStatus.Closed;
        ClosedAtUtc = DateTimeOffset.UtcNow;
        CurrentMediaId = null;
        Raise(new RoomClosedDomainEvent(Id, byUserId));
    }
}
