using WatchParty.Domain.Common;

namespace WatchParty.Domain.Admin;

/// <summary>
/// A host that media URLs are allowed to come from (architecture: only permitted
/// content may play). Managed from the admin panel.
/// </summary>
public sealed class AllowedDomain : AggregateRoot
{
    private AllowedDomain()
    {
    }

    private AllowedDomain(Guid id, string host, Guid? addedByUserId) : base(id)
    {
        Host = host;
        AddedByUserId = addedByUserId;
        IsEnabled = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public string Host { get; private set; } = null!;
    public bool IsEnabled { get; private set; }
    public Guid? AddedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Result<AllowedDomain> Create(string? host, Guid? addedByUserId)
    {
        var normalized = Normalize(host);
        if (normalized is null)
        {
            return DomainErrors.Admin.DomainInvalid;
        }

        return new AllowedDomain(Guid.NewGuid(), normalized, addedByUserId);
    }

    public void Enable() => IsEnabled = true;

    public void Disable() => IsEnabled = false;

    /// <summary>Lower-cases and strips scheme/path so only the host remains, or null if invalid.</summary>
    public static string? Normalize(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var value = host.Trim().ToLowerInvariant();

        if (value.Contains("://") && Uri.TryCreate(value, UriKind.Absolute, out var asUri))
        {
            value = asUri.Host;
        }

        value = value.TrimEnd('/');

        // Reject anything that still looks like it has a path, port or spaces.
        if (value.Length == 0 || value.Contains('/') || value.Contains(' ') || value.Contains(':') || !value.Contains('.'))
        {
            return null;
        }

        return value;
    }
}
