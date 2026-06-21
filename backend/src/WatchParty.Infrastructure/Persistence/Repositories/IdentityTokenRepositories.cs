using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Identity;

namespace WatchParty.Infrastructure.Persistence.Repositories;

public sealed class EmailVerificationTokenRepository(WatchPartyDbContext dbContext) : IEmailVerificationTokenRepository
{
    public Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.EmailVerificationTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken) =>
        await dbContext.EmailVerificationTokens.AddAsync(token, cancellationToken);

    public void Update(EmailVerificationToken token) => dbContext.EmailVerificationTokens.Update(token);
}

public sealed class PasswordResetTokenRepository(WatchPartyDbContext dbContext) : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.PasswordResetTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken) =>
        await dbContext.PasswordResetTokens.AddAsync(token, cancellationToken);

    public void Update(PasswordResetToken token) => dbContext.PasswordResetTokens.Update(token);
}

public sealed class RefreshTokenRepository(WatchPartyDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken) =>
        await dbContext.RefreshTokens.AddAsync(token, cancellationToken);

    public void Update(RefreshToken token) => dbContext.RefreshTokens.Update(token);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var activeTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId
                && token.RevokedAtUtc == null
                && token.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke();
        }
    }
}
