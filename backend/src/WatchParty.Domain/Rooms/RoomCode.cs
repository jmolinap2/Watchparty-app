using WatchParty.Domain.Common;

namespace WatchParty.Domain.Rooms;

/// <summary>
/// Short, shareable, human-friendly invite code (e.g. "K7P2QX"). Generation lives
/// in infrastructure; this value object only guards the shape.
/// </summary>
public sealed class RoomCode : ValueObject
{
    public const int Length = 6;

    // Unambiguous alphabet (no 0/O, 1/I) for easy verbal sharing.
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private RoomCode(string value) => Value = value;

    public string Value { get; }

    public static Result<RoomCode> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DomainErrors.Rooms.CodeNotFound;
        }

        var normalized = input.Trim().ToUpperInvariant();

        if (normalized.Length != Length || normalized.Any(c => !Alphabet.Contains(c)))
        {
            return DomainErrors.Rooms.CodeNotFound;
        }

        return new RoomCode(normalized);
    }

    public static RoomCode FromTrusted(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
