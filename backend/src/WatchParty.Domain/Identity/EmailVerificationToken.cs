using WatchParty.Domain.Common;

namespace WatchParty.Domain.Identity;

/// <summary>One-time token used to confirm a user's email address.</summary>
public sealed class EmailVerificationToken : AggregateRoot
{
    private EmailVerificationToken()
    {
    }

    private EmailVerificationToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAtUtc) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    public bool IsConsumed => ConsumedAtUtc is not null;
    public bool IsUsable => !IsConsumed && !IsExpired;

    public static EmailVerificationToken Issue(Guid userId, string tokenHash, DateTimeOffset expiresAtUtc)
        => new(Guid.NewGuid(), userId, tokenHash, expiresAtUtc);

    public void Consume() => ConsumedAtUtc ??= DateTimeOffset.UtcNow;
}
