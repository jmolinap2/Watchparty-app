namespace WatchParty.Domain.Identity;

/// <summary>
/// Global role of a user. Room-level roles (host/member) are modelled separately.
/// Advanced roles are out of scope for V1.
/// </summary>
public enum UserRole
{
    User = 0,
    Admin = 1
}
