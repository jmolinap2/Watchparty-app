using Microsoft.JSInterop;
using WatchParty.Contracts.Identity;

namespace WatchParty.Admin.Services;

/// <summary>
/// Holds the authenticated admin session for the lifetime of a Blazor circuit and
/// mirrors it to <c>localStorage</c> so a refresh keeps the user signed in. The role
/// is captured here so the rest of the app can gate UI on it.
/// </summary>
public sealed class AdminSession(IJSRuntime jsRuntime, ILogger<AdminSession> logger)
{
    private const string TokenKey = "watchparty.admin.accessToken";
    private const string EmailKey = "watchparty.admin.email";
    private const string ExpiresKey = "watchparty.admin.accessTokenExpiresAtUtc";
    private const string UserIdKey = "watchparty.admin.userId";
    private const string DisplayNameKey = "watchparty.admin.displayName";
    private const string RoleKey = "watchparty.admin.role";

    public const string AdminRole = "Admin";

    /// <summary>Raised whenever the session is set or cleared (drives the auth state provider).</summary>
    public event Action? Changed;

    public string? Token { get; private set; }
    public string? Email { get; private set; }
    public Guid? UserId { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Role { get; private set; }
    public DateTimeOffset? AccessTokenExpiresAtUtc { get; private set; }
    public bool HasLoadedFromBrowser { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(Token)
        && (AccessTokenExpiresAtUtc is null || AccessTokenExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(1));

    public bool IsAdmin =>
        IsAuthenticated && string.Equals(Role, AdminRole, StringComparison.OrdinalIgnoreCase);

    public async ValueTask RestoreAsync()
    {
        if (HasLoadedFromBrowser)
        {
            return;
        }

        try
        {
            Token = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            Email = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", EmailKey);
            DisplayName = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", DisplayNameKey);
            Role = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", RoleKey);

            var userId = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", UserIdKey);
            UserId = Guid.TryParse(userId, out var parsedId) ? parsedId : null;

            var expires = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", ExpiresKey);
            AccessTokenExpiresAtUtc = DateTimeOffset.TryParse(expires, out var parsed) ? parsed : null;

            HasLoadedFromBrowser = true;

            if (!IsAuthenticated)
            {
                await ClearAsync();
            }
            else
            {
                Changed?.Invoke();
            }
        }
        catch (InvalidOperationException ex)
        {
            // JS interop is unavailable during prerender; retried on first interactive render.
            logger.LogDebug(ex, "Browser storage is not available yet for admin session restore.");
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Could not restore admin session from browser storage.");
            HasLoadedFromBrowser = true;
            ClearInMemory();
            Changed?.Invoke();
        }
    }

    public async ValueTask SetTokenAsync(AuthResponse auth)
    {
        Token = auth.AccessToken;
        Email = auth.User.Email;
        UserId = auth.User.Id;
        DisplayName = auth.User.DisplayName;
        Role = auth.User.Role;
        AccessTokenExpiresAtUtc = auth.AccessTokenExpiresAtUtc;
        HasLoadedFromBrowser = true;

        await jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, auth.AccessToken);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", EmailKey, auth.User.Email);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", UserIdKey, auth.User.Id.ToString());
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", DisplayNameKey, auth.User.DisplayName);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", RoleKey, auth.User.Role);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", ExpiresKey, auth.AccessTokenExpiresAtUtc.ToString("O"));

        Changed?.Invoke();
    }

    public async ValueTask ClearAsync()
    {
        ClearInMemory();
        HasLoadedFromBrowser = true;

        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", EmailKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserIdKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", DisplayNameKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", RoleKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", ExpiresKey);

        Changed?.Invoke();
    }

    private void ClearInMemory()
    {
        Token = null;
        Email = null;
        UserId = null;
        DisplayName = null;
        Role = null;
        AccessTokenExpiresAtUtc = null;
    }
}
