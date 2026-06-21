using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using WatchParty.Admin.Components;
using WatchParty.Admin.Services;
using WatchParty.Observability.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddWatchPartyFileLogger(builder.Configuration, "WatchParty.Admin", "watchparty-admin.log");

try
{
    var supportedCultures = new[] { new CultureInfo("es"), new CultureInfo("en") };

    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
    builder.Services.AddScoped<AdminSession>();
    builder.Services.AddScoped<AuthenticationStateProvider, AdminAuthenticationStateProvider>();
    builder.Services.AddAuthorizationCore();
    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddHttpClient<AdminApiClient>((serviceProvider, client) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["BackendApi:BaseUrl"] ?? "https://localhost:7001";
        client.BaseAddress = new Uri(baseUrl);
    }).ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        if (builder.Environment.IsDevelopment())
        {
            // IIS Express uses a self-signed cert that the server-side HttpClient rejects.
            // Safe to skip in local dev only.
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        return handler;
    });

    var app = builder.Build();

    app.Logger.LogInformation(
        "WatchParty Admin starting. Environment={Environment}; BackendApi={BackendApi}; LogFile={LogFile}.",
        app.Environment.EnvironmentName,
        app.Configuration["BackendApi:BaseUrl"] ?? "https://localhost:7001",
        WatchPartyLogPaths.ResolveLogFilePath(app.Configuration, "watchparty-admin.log"));

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture("es"),
        SupportedCultures = supportedCultures,
        SupportedUICultures = supportedCultures
    });

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    app.UseAntiforgery();

    app.MapGet("/health", () => Results.Ok());
    app.MapGet("/culture/set", (string culture, string? redirectUri, HttpContext context) =>
    {
        if (!supportedCultures.Any(item => string.Equals(item.Name, culture, StringComparison.OrdinalIgnoreCase)))
        {
            culture = "es";
        }

        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps
            });

        return Results.LocalRedirect(string.IsNullOrWhiteSpace(redirectUri) ? "/" : redirectUri);
    });
    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    await app.RunAsync();
}
catch (Exception exception)
{
    WatchPartyStartupLogger.LogFatal(builder.Configuration, "WatchParty.Admin", "watchparty-admin.log", exception);
    throw;
}
