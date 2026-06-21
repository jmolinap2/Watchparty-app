using WatchParty.Domain.Common;

namespace WatchParty.Domain.Rooms;

/// <summary>Configurable room options grouped as a value object.</summary>
public sealed class RoomSettings : ValueObject
{
    public const int DefaultMaxMembers = 20;
    public const int HardMaxMembers = 100;

    private RoomSettings(bool isPrivate, int maxMembers)
    {
        IsPrivate = isPrivate;
        MaxMembers = maxMembers;
    }

    public bool IsPrivate { get; }
    public int MaxMembers { get; }

    public static RoomSettings Create(bool isPrivate, int? maxMembers = null)
    {
        var capacity = maxMembers ?? DefaultMaxMembers;
        capacity = Math.Clamp(capacity, 2, HardMaxMembers);
        return new RoomSettings(isPrivate, capacity);
    }

    public static RoomSettings Default => Create(isPrivate: false);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsPrivate;
        yield return MaxMembers;
    }
}
