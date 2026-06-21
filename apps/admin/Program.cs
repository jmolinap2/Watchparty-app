using WatchParty.Admin.Components;
using WatchParty.Admin.Services;
using WatchParty.Observability.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddWatchPartyFileLogger(builder.Configuration, "WatchParty.Admin", "watchparty-admin.log");

try
{
    builder.Services.AddScoped<AdminSession>();

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
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    app.UseAntiforgery();

    app.MapGet("/health", () => Results.Ok());
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
