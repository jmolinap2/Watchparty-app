using Microsoft.Extensions.Options;
using WatchParty.Application.Abstractions;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Application.Common;
using WatchParty.Application.Users;
using WatchParty.Contracts.Identity;
using WatchParty.Domain.Identity;

namespace WatchParty.Application.Identity;

/// <summary>
/// Application service that mints an access token + a freshly persisted rotating
/// refresh token and assembles the <see cref="AuthResponse"/>. Shared by register,
/// login and refresh so the token-issuing rules live in one place.
/// </summary>
public sealed class AuthTokenIssuer(
    IJwtTokenGenerator jwtTokenGenerator,
    ISecureTokenGenerator secureTokenGenerator,
    ITokenHasher tokenHasher,
    IRefreshTokenRepository refreshTokenRepository,
    IClock clock,
    IOptions<SecurityOptions> securityOptions)
{
    private readonly SecurityOptions _options = securityOptions.Value;

    public async Task<AuthResponse> IssueAsync(User user, string? createdByIp, CancellationToken cancellationToken)
    {
        var accessToken = jwtTokenGenerator.Generate(user);

        var rawRefreshToken = secureTokenGenerator.Generate();
        var refreshExpiresAt = clock.UtcNow.AddDays(_options.RefreshTokenDays);
        var refreshToken = RefreshToken.Issue(user.Id, tokenHasher.Hash(rawRefreshToken), refreshExpiresAt, createdByIp);
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new AuthResponse(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            rawRefreshToken,
            refreshExpiresAt,
            user.ToProfileDto());
    }

    /// <summary>Rotates an existing refresh token, returning a new auth response.</summary>
    public async Task<AuthResponse> RotateAsync(User user, RefreshToken current, string? createdByIp, CancellationToken cancellationToken)
    {
        var rawRefreshToken = secureTokenGenerator.Generate();
        var newHash = tokenHasher.Hash(rawRefreshToken);
        current.Rotate(newHash);
        refreshTokenRepository.Update(current);

        var refreshExpiresAt = clock.UtcNow.AddDays(_options.RefreshTokenDays);
        var refreshToken = RefreshToken.Issue(user.Id, newHash, refreshExpiresAt, createdByIp);
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        var accessToken = jwtTokenGenerator.Generate(user);
        return new AuthResponse(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            rawRefreshToken,
            refreshExpiresAt,
            user.ToProfileDto());
    }
}
