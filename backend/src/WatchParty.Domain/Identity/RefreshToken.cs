using WatchParty.Domain.Common;

namespace WatchParty.Domain.Identity;

/// <summary>
/// A rotating refresh token. Only the hash is stored. Rotation chains tokens via
/// <see cref="ReplacedByTokenHash"/> so reuse of a revoked token can be detected.
/// </summary>
public sealed class RefreshToken : AggregateRoot
{
    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAtUtc, string? createdByIp)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        CreatedByIp = createdByIp;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? CreatedByIp { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    public bool IsActive => RevokedAtUtc is null && !IsExpired;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset expiresAtUtc, string? createdByIp = null)
        => new(Guid.NewGuid(), userId, tokenHash, expiresAtUtc, createdByIp);

    public void Revoke() => RevokedAtUtc ??= DateTimeOffset.UtcNow;

    public void Rotate(string replacedByTokenHash)
    {
        Revoke();
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
