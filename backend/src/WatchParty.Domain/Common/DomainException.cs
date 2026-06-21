namespace WatchParty.Domain.Common;

/// <summary>
/// Thrown when a domain invariant is violated. These represent bugs or illegal
/// state transitions, not expected business outcomes (use <see cref="Result"/> for those).
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(Error error) : base(error.Message) => Error = error;

    public Error Error { get; }
}
