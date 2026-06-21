namespace WatchParty.Application.Common;

/// <summary>Token lifetimes and auth policy, bound from configuration.</summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
    public int EmailConfirmationHours { get; set; } = 48;
    public int PasswordResetHours { get; set; } = 2;
    public int MinPasswordLength { get; set; } = 8;
}
