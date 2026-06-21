using WatchParty.Domain.Identity;

namespace WatchParty.Application.Abstractions.Persistence;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken);
    void Update(PasswordResetToken token);
}
