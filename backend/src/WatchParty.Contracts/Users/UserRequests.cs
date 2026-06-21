namespace WatchParty.Contracts.Users;

public sealed record UpdateProfileRequest(string DisplayName, bool IsPrivate);

public sealed record SetAvatarRequest(string? AvatarUrl);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record BlockUserRequest(Guid UserId);
