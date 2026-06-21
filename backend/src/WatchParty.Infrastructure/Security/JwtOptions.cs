namespace WatchParty.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "watchparty";
    public string Audience { get; set; } = "watchparty.clients";

    /// <summary>HMAC signing key. Must be at least 32 bytes; supply via configuration/secret.</summary>
    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
}
