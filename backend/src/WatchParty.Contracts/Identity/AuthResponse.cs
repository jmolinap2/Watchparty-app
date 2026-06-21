using WatchParty.Contracts.Users;

namespace WatchParty.Contracts.Identity;

/// <summary>
/// Returned on register / login / refresh. The access token is short-lived; the
/// refresh token is rotating (architecture §19).
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    UserProfileDto User);
