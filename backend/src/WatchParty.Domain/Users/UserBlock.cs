using WatchParty.Domain.Common;

namespace WatchParty.Domain.Users;

/// <summary>Records that one user has blocked another (basic privacy / moderation).</summary>
public sealed class UserBlock : AggregateRoot
{
    private UserBlock()
    {
    }

    private UserBlock(Guid id, Guid blockerUserId, Guid blockedUserId) : base(id)
    {
        BlockerUserId = blockerUserId;
        BlockedUserId = blockedUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid BlockerUserId { get; private set; }
    public Guid BlockedUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Result<UserBlock> Create(Guid blockerUserId, Guid blockedUserId)
    {
        if (blockerUserId == blockedUserId)
        {
            return DomainErrors.Users.CannotBlockSelf;
        }

        return new UserBlock(Guid.NewGuid(), blockerUserId, blockedUserId);
    }
}
