namespace WatchParty.Contracts.Identity;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed class LoginRequest
{
    public string? Identifier { get; init; }
    public string? Email { get; init; }
    public string? Username { get; init; }
    public string Password { get; init; } = string.Empty;

    public string LoginIdentifier => FirstNonEmpty(Identifier, Username, Email);

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }
}

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ConfirmEmailRequest(string Token);

public sealed record ResendConfirmationRequest(string Email);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);
