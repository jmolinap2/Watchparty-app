using WatchParty.Domain.Identity;

namespace WatchParty.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);
    void Update(RefreshToken token);

    /// <summary>Revoke every active token for a user (global sign-out).</summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}
