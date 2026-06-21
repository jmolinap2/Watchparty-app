using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Extensions;
using WatchParty.Application.Abstractions.Auditing;
using WatchParty.Domain.Admin;

namespace WatchParty.Api.Auditing;

public sealed class RequestAuditMiddleware(
    RequestDelegate next,
    ILogger<RequestAuditMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        IAuditContextAccessor auditContextAccessor,
        IAuditLogWriter auditLogWriter)
    {
        var auditContext = BuildAuditContext(httpContext);
        auditContextAccessor.Current = auditContext;

        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = exception is null
                ? httpContext.Response.StatusCode
                : StatusCodes.Status500InternalServerError;

            LogRequestOutcome(httpContext, auditContext, stopwatch.ElapsedMilliseconds, statusCode, exception);

            if (!ShouldSkip(httpContext))
            {
                await TryWriteRequestLogAsync(
                    httpContext,
                    auditContext,
                    stopwatch.ElapsedMilliseconds,
                    statusCode,
                    exception,
                    auditLogWriter);
            }
        }
    }

    private async Task TryWriteRequestLogAsync(
        HttpContext httpContext,
        AuditContext auditContext,
        long durationMs,
        int statusCode,
        Exception? exception,
        IAuditLogWriter auditLogWriter)
    {
        try
        {
            var endpoint = httpContext.GetEndpoint();
            var action = $"{auditContext.HttpMethod} {auditContext.RequestPath}";

            var auditLog = AuditLog.HttpRequest(
                action,
                auditContext.ActorUserId,
                auditContext.IpAddress,
                endpoint?.DisplayName,
                auditContext.HttpMethod,
                auditContext.HttpMethod,
                auditContext.RequestPath,
                statusCode,
                durationMs,
                auditContext.UserAgent,
                auditContext.CorrelationId,
                exception?.ToString());

            await auditLogWriter.WriteAsync(auditLog, httpContext.RequestAborted);
        }
        catch (Exception auditException)
        {
            logger.LogWarning(auditException, "Failed to persist request audit log.");
        }
    }

    private void LogRequestOutcome(
        HttpContext httpContext,
        AuditContext auditContext,
        long durationMs,
        int statusCode,
        Exception? exception)
    {
        if (httpContext.Request.Path.StartsWithSegments("/health"))
        {
            return;
        }

        const string message =
            "HTTP {Method} {Path} responded {StatusCode} in {DurationMs} ms. CorrelationId={CorrelationId}.";

        if (exception is not null)
        {
            logger.LogError(
                exception,
                message,
                auditContext.HttpMethod,
                auditContext.RequestPath,
                statusCode,
                durationMs,
                auditContext.CorrelationId);
            return;
        }

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                message,
                auditContext.HttpMethod,
                auditContext.RequestPath,
                statusCode,
                durationMs,
                auditContext.CorrelationId);
            return;
        }

        if (statusCode >= StatusCodes.Status400BadRequest)
        {
            logger.LogWarning(
                message,
                auditContext.HttpMethod,
                auditContext.RequestPath,
                statusCode,
                durationMs,
                auditContext.CorrelationId);
            return;
        }

        logger.LogInformation(
            message,
            auditContext.HttpMethod,
            auditContext.RequestPath,
            statusCode,
            durationMs,
            auditContext.CorrelationId);
    }

    private static AuditContext BuildAuditContext(HttpContext httpContext)
    {
        var correlationId = GetOrCreateCorrelationId(httpContext);
        httpContext.Response.Headers.TryAdd("X-Correlation-ID", correlationId);

        return new AuditContext(
            GetActorUserId(httpContext.User),
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString(),
            correlationId,
            httpContext.Request.Method,
            httpContext.Request.GetEncodedPathAndQuery());
    }

    private static bool ShouldSkip(HttpContext httpContext) =>
        httpContext.GetEndpoint()?.Metadata.GetMetadata<DisableRequestAuditingAttribute>() is not null;

    private static Guid? GetActorUserId(ClaimsPrincipal user)
    {
        var rawUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(rawUserId, out var userId) ? userId : null;
    }

    private static string GetOrCreateCorrelationId(HttpContext httpContext)
    {
        const string headerName = "X-Correlation-ID";
        if (httpContext.Request.Headers.TryGetValue(headerName, out var incoming)
            && !string.IsNullOrWhiteSpace(incoming))
        {
            return incoming.ToString();
        }

        return httpContext.TraceIdentifier;
    }
}
