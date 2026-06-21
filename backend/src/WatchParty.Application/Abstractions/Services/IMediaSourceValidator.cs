using WatchParty.Domain.Common;
using WatchParty.Domain.Playback;

namespace WatchParty.Application.Abstractions.Services;

/// <summary>
/// Turns a user-supplied URL into a validated <see cref="MediaSource"/>, enforcing
/// the V1 source policy (scope §4): HTTPS, allowed domain, supported format, and
/// recognising YouTube / Google Drive links.
/// </summary>
public interface IMediaSourceValidator
{
    Task<Result<MediaSource>> ValidateAsync(string url, CancellationToken cancellationToken);
}
