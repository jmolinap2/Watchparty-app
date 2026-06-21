namespace WatchParty.Api.Auditing;

public static class RequestAuditMiddlewareExtensions
{
    public static IApplicationBuilder UseWatchPartyRequestAuditing(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestAuditMiddleware>();
}
