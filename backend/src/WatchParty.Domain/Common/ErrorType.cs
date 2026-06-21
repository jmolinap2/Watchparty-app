namespace WatchParty.Domain.Common;

/// <summary>
/// Classifies an <see cref="Error"/> so outer layers (API) can map it to the
/// correct transport status without leaking domain details.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}
