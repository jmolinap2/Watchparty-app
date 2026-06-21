using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WatchParty.Application.Abstractions;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Domain.Identity;

namespace WatchParty.Infrastructure.Security;

/// <summary>
/// Issues short-lived HS256 access tokens (architecture §19). Embeds the user's
/// security stamp so a password change / global sign-out can be detected.
/// </summary>
public sealed class JwtTokenGenerator(IOptions<JwtOptions> options, IClock clock) : IJwtTokenGenerator
{
    public const string SecurityStampClaim = "security_stamp";

    private readonly JwtOptions _options = options.Value;

    public AccessToken Generate(User user)
    {
        var expiresAt = clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("name", user.DisplayName),
            new(SecurityStampClaim, user.SecurityStamp.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: clock.UtcNow.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var value = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(value, expiresAt);
    }
}
