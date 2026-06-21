using WatchParty.Domain.Identity;

namespace WatchParty.Application.Abstractions.Persistence;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken);
    void Update(EmailVerificationToken token);
}
