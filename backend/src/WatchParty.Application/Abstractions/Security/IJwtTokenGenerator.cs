using WatchParty.Domain.Identity;

namespace WatchParty.Application.Abstractions.Security;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}
