using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using WatchParty.Admin.Localization;
using WatchParty.Admin.Services;

namespace WatchParty.Admin.Components.Shared;

/// <summary>
/// Shared plumbing for every admin page: injected services, localization helpers,
/// a uniform "run an API call with loading + error handling" wrapper, and a first-render
/// load hook. Pages override <see cref="LoadInitialAsync"/> and call <see cref="RunAsync"/>;
/// they don't repeat try/catch, logging, or 401 handling.
/// </summary>
public abstract class AdminPageBase : ComponentBase
{
    [Inject] protected AdminApiClient Api { get; set; } = default!;
    [Inject] protected AdminSession Session { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] protected IStringLocalizer<SharedResource> L { get; set; } = default!;
    [Inject] protected ILogger<AdminPageBase> Logger { get; set; } = default!;

    /// <summary>True while an API call started through <see cref="RunAsync"/> is in flight.</summary>
    protected bool Loading { get; private set; }

    /// <summary>Status/notice text shown by the page (success or failure).</summary>
    protected string? Message { get; private set; }

    protected bool MessageIsError { get; private set; }

    protected string Text(string key) => L[key].Value;

    protected string Text(string key, params object[] args) => L[key, args].Value;

    /// <summary>Localizes an enum-like value (e.g. <c>Enum.Role.Admin</c>), falling back to the raw value.</summary>
    protected string TranslateValue(string prefix, string value)
    {
        var localized = L[$"{prefix}.{value}"];
        return localized.ResourceNotFound ? value : localized.Value;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        // AdminSessionBoundary restores the session before rendering children, so by
        // the time OnAfterRenderAsync fires here the session is already resolved.
        if (!Session.IsAdmin)
        {
            Nav.NavigateTo("login", replace: true);
            return;
        }

        await LoadInitialAsync();
    }

    /// <summary>Override to load the page's data on first render (session is already restored).</summary>
    protected virtual Task LoadInitialAsync() => Task.CompletedTask;

    /// <summary>
    /// Runs an API action with consistent loading state, error mapping and logging.
    /// On success, optionally shows a localized notice keyed by <paramref name="successKey"/>.
    /// </summary>
    protected async Task RunAsync(Func<Task> action, string? successKey = null)
    {
        Loading = true;
        MessageIsError = false;
        StateHasChanged();

        try
        {
            await action();
            if (successKey is not null)
            {
                Message = Text(successKey, DateTime.Now.ToString("t"));
            }
        }
        catch (AdminApiException ex)
        {
            MessageIsError = true;
            Message = FormatApiFailure(ex);
            Logger.LogWarning(
                ex,
                "Admin operation failed. Status={Status}; Code={Code}; CorrelationId={CorrelationId}",
                ex.StatusText,
                ex.ApiCode ?? "-",
                ex.CorrelationId ?? "-");

            if (ex.IsUnauthorized)
            {
                await Session.ClearAsync();
                Nav.NavigateTo("login", replace: true);
            }
        }
        catch (Exception ex)
        {
            MessageIsError = true;
            Message = ex.Message;
            Logger.LogError(ex, "Unexpected admin operation failure.");
        }
        finally
        {
            Loading = false;
            StateHasChanged();
        }
    }

    protected async Task LogoutAsync()
    {
        await Session.ClearAsync();
        Nav.NavigateTo("login", replace: true);
    }

    protected string FormatApiFailure(AdminApiException ex)
    {
        var suffix = string.IsNullOrWhiteSpace(ex.CorrelationId)
            ? string.Empty
            : Text("Errors.CorrelationSuffix", ex.CorrelationId);

        if (!ex.HasHttpResponse)
        {
            return Text("Errors.ApiUnavailable", ex.BackendApi ?? new Uri("about:blank"), suffix);
        }

        if ((int)ex.StatusCode!.Value >= 500)
        {
            return Text("Errors.ApiServer", ex.StatusText, suffix);
        }

        if (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return Text("Errors.Unauthorized", ex.StatusText, suffix);
        }

        return Text("Errors.ApiRejected", ex.StatusText, ex.ApiCode ?? Text("Common.NoCode"), ex.ApiMessage ?? ex.Message, suffix);
    }
}
