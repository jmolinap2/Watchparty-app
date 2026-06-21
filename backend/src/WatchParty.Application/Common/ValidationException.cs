namespace WatchParty.Application.Common;

/// <summary>
/// Thrown by the validation pipeline for contract-level failures (required fields,
/// formats, lengths). The API maps this to a 400 with per-field details.
/// </summary>
public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
