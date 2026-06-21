namespace WatchParty.Application.Abstractions.Services;

/// <summary>Sends transactional emails. Infrastructure detail (architecture §4).</summary>
public interface IEmailSender
{
    Task SendEmailConfirmationAsync(string toEmail, string displayName, string token, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(string toEmail, string displayName, string token, CancellationToken cancellationToken);
}
