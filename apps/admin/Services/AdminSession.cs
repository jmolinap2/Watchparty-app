using Microsoft.JSInterop;

namespace WatchParty.Admin.Services;

public sealed class AdminSession(IJSRuntime jsRuntime, ILogger<AdminSession> logger)
{
    private const string TokenKey = "watchparty.admin.accessToken";
    private const string EmailKey = "watchparty.admin.email";
    private const string ExpiresKey = "watchparty.admin.accessTokenExpiresAtUtc";

    public string? Token { get; private set; }
    public string? Email { get; private set; }
    public DateTimeOffset? AccessTokenExpiresAtUtc { get; private set; }
    public bool HasLoadedFromBrowser { get; private set; }
    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(Token)
        && (AccessTokenExpiresAtUtc is null || AccessTokenExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(1));

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
            var expires = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", ExpiresKey);

            AccessTokenExpiresAtUtc = DateTimeOffset.TryParse(expires, out var parsed)
                ? parsed
                : null;

            HasLoadedFromBrowser = true;

            if (!IsAuthenticated)
            {
                await ClearAsync();
            }
        }
        catch (InvalidOperationException ex)
        {
            logger.LogDebug(ex, "Browser storage is not available yet for admin session restore.");
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Could not restore admin session from browser storage.");
            HasLoadedFromBrowser = true;
            ClearInMemory();
        }
    }

    public async ValueTask SetTokenAsync(AuthResponse auth, string email)
    {
        Token = auth.AccessToken;
        Email = email;
        AccessTokenExpiresAtUtc = auth.AccessTokenExpiresAtUtc;
        HasLoadedFromBrowser = true;

        await jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, auth.AccessToken);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", EmailKey, email);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", ExpiresKey, auth.AccessTokenExpiresAtUtc.ToString("O"));
    }

    public async ValueTask ClearAsync()
    {
        ClearInMemory();
        HasLoadedFromBrowser = true;

        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", EmailKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", ExpiresKey);
    }

    private void ClearInMemory()
    {
        Token = null;
        Email = null;
        AccessTokenExpiresAtUtc = null;
    }
}
