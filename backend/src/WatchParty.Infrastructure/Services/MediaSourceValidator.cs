using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Services;
using WatchParty.Application.Common;
using WatchParty.Domain.Common;
using WatchParty.Domain.Playback;

namespace WatchParty.Infrastructure.Services;

/// <summary>
/// Validates a user-supplied URL and resolves it to a permitted <see cref="MediaSource"/>.
/// Enforces the V1 source policy (scope §4 / §5): HTTPS only, allowed domains, supported
/// formats, plus first-class support for YouTube, Google Drive and MEGA (embed/stream only —
/// no download, no DRM bypass). Recognised hosts and formats are parameterised through
/// <see cref="MediaSourceOptions"/>.
/// </summary>
public sealed partial class MediaSourceValidator(
    IAllowedDomainRepository allowedDomainRepository,
    IOptions<MediaSourceOptions> options)
    : IMediaSourceValidator
{
    private readonly MediaSourceOptions _options = options.Value;

    public async Task<Result<MediaSource>> ValidateAsync(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return DomainErrors.Playback.MediaUrlRequired;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return DomainErrors.Playback.MediaUrlInvalid;
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            return DomainErrors.Playback.InsecureProtocol;
        }

        var host = uri.Host.ToLowerInvariant();

        // YouTube — recognised provider (played via the embedded player).
        if (HostMatches(host, _options.YouTubeHosts))
        {
            var videoId = ExtractYouTubeId(uri);
            return videoId is null
                ? DomainErrors.Playback.MediaUrlInvalid
                : MediaSource.YouTube(videoId, url);
        }

        // Google Drive — the user's own file, streamed with their authorization on the client.
        if (HostMatches(host, _options.GoogleDriveHosts))
        {
            var fileId = ExtractDriveFileId(uri);
            return fileId is null
                ? DomainErrors.Playback.MediaUrlInvalid
                : MediaSource.GoogleDrive(fileId, url);
        }

        // MEGA — the user's own file, streamed via MEGA's embedded player. The decryption
        // key lives in the link fragment the user supplies (no key, no playback).
        if (HostMatches(host, _options.MegaHosts))
        {
            var mega = ExtractMega(uri);
            return mega is null
                ? DomainErrors.Playback.MediaUrlInvalid
                : MediaSource.Mega(mega.Value.FileId, mega.Value.Key, url);
        }

        // Direct / HLS URLs must come from an allowed domain.
        var allowedHosts = await allowedDomainRepository.GetEnabledHostsAsync(cancellationToken);
        if (!IsHostAllowed(host, allowedHosts))
        {
            return DomainErrors.Playback.DomainNotAllowed;
        }

        var path = uri.AbsolutePath.ToLowerInvariant();
        if (_options.HlsExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            return MediaSource.Hls(url);
        }

        if (_options.DirectExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            return MediaSource.Direct(url);
        }

        return DomainErrors.Playback.UnsupportedFormat;
    }

    /// <summary>Case-insensitive exact match against a configured host list.</summary>
    private static bool HostMatches(string host, IEnumerable<string> hosts) =>
        hosts.Any(h => string.Equals(host, h.Trim(), StringComparison.OrdinalIgnoreCase));

    private static bool IsHostAllowed(string host, IReadOnlyCollection<string> allowedHosts) =>
        allowedHosts.Any(allowed => host == allowed || host.EndsWith("." + allowed, StringComparison.Ordinal));

    private static string? ExtractYouTubeId(Uri uri)
    {
        if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            var id = uri.AbsolutePath.Trim('/');
            return IsValidYouTubeId(id) ? id : null;
        }

        var fromQuery = GetQueryValue(uri.Query, "v");
        if (IsValidYouTubeId(fromQuery))
        {
            return fromQuery;
        }

        // /embed/{id} or /shorts/{id} or /live/{id}
        var match = YouTubePathId().Match(uri.AbsolutePath);
        return match.Success && IsValidYouTubeId(match.Groups["id"].Value) ? match.Groups["id"].Value : null;
    }

    private static string? ExtractDriveFileId(Uri uri)
    {
        // /file/d/{id}/view
        var pathMatch = DrivePathId().Match(uri.AbsolutePath);
        if (pathMatch.Success)
        {
            return pathMatch.Groups["id"].Value;
        }

        // ?id={id} (open?id=, uc?id=)
        var id = GetQueryValue(uri.Query, "id");
        return string.IsNullOrWhiteSpace(id) ? null : id;
    }

    /// <summary>
    /// Resolves a MEGA single-file link to its (fileId, key) pair. Supports the modern
    /// <c>/file/{id}#{key}</c> and <c>/embed/{id}#{key}</c> forms and the legacy
    /// <c>/#!{id}!{key}</c> form. Folder links are not supported (single file only).
    /// </summary>
    private static (string FileId, string Key)? ExtractMega(Uri uri)
    {
        var fragment = uri.Fragment.TrimStart('#');

        // Legacy: https://mega.nz/#!{id}!{key}
        var legacy = MegaLegacy().Match(fragment);
        if (legacy.Success)
        {
            return (legacy.Groups["id"].Value, legacy.Groups["key"].Value);
        }

        // Modern: https://mega.nz/file/{id}#{key} or /embed/{id}#{key}
        var pathMatch = MegaPathId().Match(uri.AbsolutePath);
        if (pathMatch.Success && !string.IsNullOrWhiteSpace(fragment) && MegaKey().IsMatch(fragment))
        {
            return (pathMatch.Groups["id"].Value, fragment);
        }

        return null;
    }

    /// <summary>Minimal query-string lookup so this library needs no ASP.NET dependency.</summary>
    private static string? GetQueryValue(string query, string key)
    {
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            if (pair[..separator].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[(separator + 1)..]);
            }
        }

        return null;
    }

    private static bool IsValidYouTubeId(string? id) =>
        !string.IsNullOrWhiteSpace(id) && YouTubeId().IsMatch(id);

    [GeneratedRegex(@"^[A-Za-z0-9_-]{11}$")]
    private static partial Regex YouTubeId();

    [GeneratedRegex(@"/(embed|shorts|live|v)/(?<id>[A-Za-z0-9_-]{11})")]
    private static partial Regex YouTubePathId();

    [GeneratedRegex(@"/file/d/(?<id>[A-Za-z0-9_-]+)")]
    private static partial Regex DrivePathId();

    [GeneratedRegex(@"^/(?:file|embed)/(?<id>[A-Za-z0-9_-]+)")]
    private static partial Regex MegaPathId();

    [GeneratedRegex(@"^!(?<id>[A-Za-z0-9_-]+)!(?<key>[A-Za-z0-9_-]+)$")]
    private static partial Regex MegaLegacy();

    [GeneratedRegex(@"^[A-Za-z0-9_-]+$")]
    private static partial Regex MegaKey();
}
