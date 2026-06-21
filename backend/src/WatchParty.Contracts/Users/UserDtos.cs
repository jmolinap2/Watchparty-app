namespace WatchParty.Contracts.Users;

/// <summary>The authenticated user's own profile.</summary>
public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    bool IsPrivate,
    string Role,
    bool EmailConfirmed,
    DateTimeOffset CreatedAtUtc);

/// <summary>A user as seen by others (respects privacy).</summary>
public sealed record PublicUserDto(
    Guid Id,
    string DisplayName,
    string? AvatarUrl);
