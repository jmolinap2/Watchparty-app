using WatchParty.Contracts.Users;
using WatchParty.Domain.Identity;

namespace WatchParty.Application.Users;

public static class UserMappings
{
    public static UserProfileDto ToProfileDto(this User user) => new(
        user.Id,
        user.Email.Value,
        user.DisplayName,
        user.AvatarUrl,
        user.IsPrivate,
        user.Role.ToString(),
        user.EmailConfirmed,
        user.CreatedAtUtc);

    public static PublicUserDto ToPublicDto(this User user) => new(
        user.Id,
        user.DisplayName,
        user.AvatarUrl);
}
