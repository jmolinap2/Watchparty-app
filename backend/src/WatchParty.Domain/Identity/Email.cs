using System.Text.RegularExpressions;
using WatchParty.Domain.Common;

namespace WatchParty.Domain.Identity;

/// <summary>
/// Email value object. Normalises to lower-case and validates a basic shape.
/// </summary>
public sealed partial class Email : ValueObject
{
    public const int MaxLength = 256;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DomainErrors.Identity.EmailRequired;
        }

        var normalized = input.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            return DomainErrors.Identity.EmailTooLong;
        }

        if (!EmailRegex().IsMatch(normalized))
        {
            return DomainErrors.Identity.EmailInvalid;
        }

        return new Email(normalized);
    }

    /// <summary>Used by the persistence layer to rehydrate a known-valid value.</summary>
    public static Email FromTrusted(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
