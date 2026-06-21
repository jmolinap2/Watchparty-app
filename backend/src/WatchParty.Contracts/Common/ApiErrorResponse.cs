namespace WatchParty.Contracts.Common;

/// <summary>
/// The stable error envelope returned by the API (architecture §15). The
/// <see cref="Code"/> is what clients switch on; <see cref="Details"/> carries
/// per-field validation errors when present.
/// </summary>
public sealed record ApiErrorResponse(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Details = null,
    string? CorrelationId = null);
