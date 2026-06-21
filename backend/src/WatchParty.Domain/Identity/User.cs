using System.Text.RegularExpressions;
using WatchParty.Domain.Common;
using WatchParty.Domain.Identity.Events;

namespace WatchParty.Domain.Identity;

/// <summary>
/// The account aggregate. Owns authentication state (Identity module) and basic
/// profile fields (Users module). Session tokens are modelled as separate
/// aggregates that reference <see cref="Id"/>.
/// </summary>
public sealed partial class User : AggregateRoot
{
    public const int MaxDisplayNameLength = 40;
    public const int MinUsernameLength = 3;
    public const int MaxUsernameLength = 32;

    private User()
    {
    }

    private User(Guid id, Email email, string passwordHash, string displayName) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        Role = UserRole.User;
        EmailConfirmed = false;
        IsBlocked = false;
        SecurityStamp = Guid.NewGuid();
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Email Email { get; private set; } = null!;

    /// <summary>
    /// Optional unique login handle (lowercase). Lets accounts — primarily the
    /// admin — sign in with a username instead of an email. Null for accounts that
    /// only authenticate by email (the default for self-registered users).
    /// </summary>
    public string? Username { get; private set; }

    public string PasswordHash { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public bool IsPrivate { get; private set; }
    public UserRole Role { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public bool IsBlocked { get; private set; }
    public string? BlockedReason { get; private set; }
    public DateTimeOffset? BlockedAtUtc { get; private set; }

    /// <summary>Rotated on password change / global sign-out to invalidate issued JWTs.</summary>
    public Guid SecurityStamp { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public static Result<User> Register(Email email, string passwordHash, string displayName)
    {
        var trimmedName = displayName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return DomainErrors.Identity.DisplayNameRequired;
        }

        if (trimmedName.Length > MaxDisplayNameLength)
        {
            return DomainErrors.Users.DisplayNameTooLong;
        }

        var user = new User(Guid.NewGuid(), email, passwordHash, trimmedName);
        user.Raise(new UserRegisteredDomainEvent(user.Id, email.Value, trimmedName));
        return user;
    }

    public void ConfirmEmail()
    {
        if (EmailConfirmed)
        {
            return;
        }

        EmailConfirmed = true;
        Touch();
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        RotateSecurityStamp();
        Raise(new UserPasswordChangedDomainEvent(Id));
    }

    public Result UpdateProfile(string displayName, bool isPrivate)
    {
        var trimmedName = displayName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return DomainErrors.Identity.DisplayNameRequired;
        }

        if (trimmedName.Length > MaxDisplayNameLength)
        {
            return DomainErrors.Users.DisplayNameTooLong;
        }

        DisplayName = trimmedName;
        IsPrivate = isPrivate;
        Touch();
        return Result.Success();
    }

    public void SetAvatar(string? avatarUrl)
    {
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        Touch();
    }

    /// <summary>
    /// Assigns (or clears) the login username. The value is normalised to lowercase;
    /// uniqueness is enforced by the caller (repository / DB unique index).
    /// </summary>
    public Result SetUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            Username = null;
            Touch();
            return Result.Success();
        }

        var normalized = NormalizeUsername(username);
        if (normalized is null)
        {
            return DomainErrors.Identity.UsernameInvalid;
        }

        Username = normalized;
        Touch();
        return Result.Success();
    }

    /// <summary>
    /// Normalises a username to its canonical form (trimmed, lowercase) and returns
    /// null when it does not satisfy the format rules (3-32 chars, letters/digits/._-).
    /// </summary>
    public static string? NormalizeUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var candidate = username.Trim().ToLowerInvariant();
        return UsernamePattern().IsMatch(candidate) ? candidate : null;
    }

    [GeneratedRegex(@"^[a-z0-9._-]{3,32}$")]
    private static partial Regex UsernamePattern();

    public void RecordLogin() => LastLoginAtUtc = DateTimeOffset.UtcNow;

    public void BlockByAdmin(string? reason)
    {
        IsBlocked = true;
        BlockedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        BlockedAtUtc = DateTimeOffset.UtcNow;
        RotateSecurityStamp();
        Touch();
    }

    public void UnblockByAdmin()
    {
        IsBlocked = false;
        BlockedReason = null;
        BlockedAtUtc = null;
        Touch();
    }

    public void SetRole(UserRole role)
    {
        Role = role;
        Touch();
    }

    /// <summary>Invalidate all issued access tokens (global sign-out).</summary>
    public void RotateSecurityStamp()
    {
        SecurityStamp = Guid.NewGuid();
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
