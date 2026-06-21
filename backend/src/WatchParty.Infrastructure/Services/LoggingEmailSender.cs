using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WatchParty.Application.Abstractions.Services;

namespace WatchParty.Infrastructure.Services;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Base URL of the web client used to build confirmation / reset links.</summary>
    public string WebBaseUrl { get; set; } = "http://localhost:3000";
}

/// <summary>
/// Development email sender: logs the confirmation/reset link instead of sending.
/// Swap for an SMTP/provider implementation in production (architecture §4: email is infra).
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger, IOptions<EmailOptions> options)
    : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public Task SendEmailConfirmationAsync(string toEmail, string displayName, string token, CancellationToken cancellationToken)
    {
        var link = $"{_options.WebBaseUrl}/confirm-email?token={Uri.EscapeDataString(token)}";
        logger.LogInformation("[EMAIL] Confirm email for {Email} ({Name}): {Link}", toEmail, displayName, link);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string displayName, string token, CancellationToken cancellationToken)
    {
        var link = $"{_options.WebBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        logger.LogInformation("[EMAIL] Password reset for {Email} ({Name}): {Link}", toEmail, displayName, link);
        return Task.CompletedTask;
    }
}
