namespace WatchParty.Domain.Common;

/// <summary>
/// Central catalogue of stable error codes. Clients (mobile/web) switch on these
/// codes, so they must remain stable once shipped (architecture §15).
/// </summary>
public static class DomainErrors
{
    public static class Identity
    {
        public static readonly Error EmailRequired = Error.Validation("identity.email_required", "Email is required.");
        public static readonly Error EmailInvalid = Error.Validation("identity.email_invalid", "Email format is invalid.");
        public static readonly Error EmailTooLong = Error.Validation("identity.email_too_long", "Email is too long.");
        public static readonly Error PasswordTooWeak = Error.Validation("identity.password_too_weak", "Password does not meet the minimum requirements.");
        public static readonly Error DisplayNameRequired = Error.Validation("identity.display_name_required", "Display name is required.");
        public static readonly Error EmailAlreadyInUse = Error.Conflict("identity.email_in_use", "Email is already registered.");
        public static readonly Error UsernameInvalid = Error.Validation("identity.username_invalid", "Username must be 3-32 characters using letters, digits, dot, underscore or hyphen.");
        public static readonly Error UsernameAlreadyInUse = Error.Conflict("identity.username_in_use", "Username is already taken.");
        public static readonly Error InvalidCredentials = Error.Unauthorized("identity.invalid_credentials", "Email or password is incorrect.");
        public static readonly Error EmailNotConfirmed = Error.Forbidden("identity.email_not_confirmed", "Email address has not been confirmed.");
        public static readonly Error AccountBlocked = Error.Forbidden("identity.account_blocked", "This account has been blocked.");
        public static readonly Error UserNotFound = Error.NotFound("identity.user_not_found", "User was not found.");
        public static readonly Error InvalidRefreshToken = Error.Unauthorized("identity.invalid_refresh_token", "Refresh token is invalid or expired.");
        public static readonly Error InvalidConfirmationToken = Error.Validation("identity.invalid_confirmation_token", "Confirmation token is invalid or expired.");
        public static readonly Error InvalidResetToken = Error.Validation("identity.invalid_reset_token", "Password reset token is invalid or expired.");
    }

    public static class Users
    {
        public static readonly Error NotFound = Error.NotFound("users.not_found", "User was not found.");
        public static readonly Error CannotBlockSelf = Error.Validation("users.cannot_block_self", "You cannot block yourself.");
        public static readonly Error AlreadyBlocked = Error.Conflict("users.already_blocked", "User is already blocked.");
        public static readonly Error DisplayNameTooLong = Error.Validation("users.display_name_too_long", "Display name is too long.");
    }

    public static class Rooms
    {
        public static readonly Error NotFound = Error.NotFound("room.not_found", "Room was not found.");
        public static readonly Error CodeNotFound = Error.NotFound("room.code_not_found", "No room matches that code.");
        public static readonly Error NameRequired = Error.Validation("room.name_required", "Room name is required.");
        public static readonly Error NameTooLong = Error.Validation("room.name_too_long", "Room name is too long.");
        public static readonly Error Closed = Error.Conflict("room.closed", "Room is closed.");
        public static readonly Error AlreadyMember = Error.Conflict("room.already_member", "You are already a member of this room.");
        public static readonly Error NotMember = Error.Forbidden("room.not_member", "You are not a member of this room.");
        public static readonly Error NotHost = Error.Forbidden("room.not_host", "Only the host can perform this action.");
        public static readonly Error HostCannotLeave = Error.Conflict("room.host_cannot_leave", "Transfer host before leaving, or close the room.");
        public static readonly Error TargetNotMember = Error.NotFound("room.target_not_member", "Target user is not a member of this room.");
        public static readonly Error CannotKickSelf = Error.Validation("room.cannot_kick_self", "You cannot kick yourself.");
        public static readonly Error CannotKickHost = Error.Validation("room.cannot_kick_host", "You cannot kick the host.");
        public static readonly Error Full = Error.Conflict("room.full", "Room has reached its maximum number of members.");
        public static readonly Error InviteCodeExhausted = Error.Failure("room.invite_code_exhausted", "Could not generate a unique invite code.");
    }

    public static class Playback
    {
        public static readonly Error MediaUrlRequired = Error.Validation("playback.media_url_required", "A media URL is required.");
        public static readonly Error MediaUrlInvalid = Error.Validation("playback.media_url_invalid", "The media URL is not valid.");
        public static readonly Error InsecureProtocol = Error.Validation("playback.insecure_protocol", "Only HTTPS sources are allowed.");
        public static readonly Error DomainNotAllowed = Error.Forbidden("playback.domain_not_allowed", "This domain is not on the allow-list.");
        public static readonly Error UnsupportedFormat = Error.Validation("playback.unsupported_format", "This media format is not supported.");
        public static readonly Error NoMediaLoaded = Error.Conflict("playback.no_media_loaded", "No media is currently loaded in this room.");
        public static readonly Error InvalidPosition = Error.Validation("playback.invalid_position", "Playback position is invalid.");
        public static readonly Error StaleEvent = Error.Conflict("playback.stale_event", "A newer playback state already exists.");
    }

    public static class Chat
    {
        public static readonly Error MessageEmpty = Error.Validation("chat.message_empty", "Message cannot be empty.");
        public static readonly Error MessageTooLong = Error.Validation("chat.message_too_long", "Message is too long.");
        public static readonly Error MessageNotFound = Error.NotFound("chat.message_not_found", "Message was not found.");
        public static readonly Error CannotDeleteOthers = Error.Forbidden("chat.cannot_delete_others", "You can only delete your own messages.");
        public static readonly Error AlreadyDeleted = Error.Conflict("chat.already_deleted", "Message is already deleted.");
    }

    public static class Reports
    {
        public static readonly Error NotFound = Error.NotFound("reports.not_found", "Report was not found.");
        public static readonly Error ReasonRequired = Error.Validation("reports.reason_required", "A reason is required.");
        public static readonly Error CannotReportSelf = Error.Validation("reports.cannot_report_self", "You cannot report yourself.");
        public static readonly Error AlreadyResolved = Error.Conflict("reports.already_resolved", "Report has already been resolved.");
    }

    public static class Admin
    {
        public static readonly Error DomainRequired = Error.Validation("admin.domain_required", "Domain is required.");
        public static readonly Error DomainInvalid = Error.Validation("admin.domain_invalid", "Domain format is invalid.");
        public static readonly Error DomainAlreadyAllowed = Error.Conflict("admin.domain_already_allowed", "Domain is already on the allow-list.");
        public static readonly Error DomainNotFound = Error.NotFound("admin.domain_not_found", "Domain was not found.");
    }
}
