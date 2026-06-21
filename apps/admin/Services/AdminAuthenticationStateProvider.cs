using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace WatchParty.Admin.Services;

/// <summary>
/// Projects <see cref="AdminSession"/> into a <see cref="ClaimsPrincipal"/> so the app
/// can use <c>AuthorizeView</c> / <c>[Authorize]</c> for role-based visibility. The
/// session is restored from the browser by <c>AdminSessionBoundary</c>; this provider
/// just reacts to <see cref="AdminSession.Changed"/>.
/// </summary>
public sealed class AdminAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));
    private readonly AdminSession session;

    public AdminAuthenticationStateProvider(AdminSession session)
    {
        this.session = session;
        session.Changed += OnSessionChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(BuildState());

    private AuthenticationState BuildState()
    {
        if (!session.IsAuthenticated)
        {
            return Anonymous;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId?.ToString() ?? string.Empty),
            new(ClaimTypes.Name, session.DisplayName ?? session.Email ?? string.Empty),
            new(ClaimTypes.Email, session.Email ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(session.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, session.Role));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "WatchPartyAdmin");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private void OnSessionChanged() =>
        NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));

    public void Dispose() => session.Changed -= OnSessionChanged;
}
