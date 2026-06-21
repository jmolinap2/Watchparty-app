using System.Text.Json;
using WatchParty.Application.Common;
using WatchParty.Contracts.Common;

namespace WatchParty.Api.Middleware;

/// <summary>
/// Outermost middleware: converts a contract <see cref="ValidationException"/> to a
/// 400 with per-field details, and any unhandled exception to a generic 500 — both
/// using the stable <see cref="ApiErrorResponse"/> envelope.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException validationException)
        {
            var correlationId = GetOrCreateCorrelationId(context);
            await WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                new ApiErrorResponse(
                    "validation_failed",
                    "One or more validation errors occurred.",
                    validationException.Errors,
                    correlationId));
        }
        catch (Exception exception)
        {
            var correlationId = GetOrCreateCorrelationId(context);
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                new ApiErrorResponse("server_error", "An unexpected error occurred.", CorrelationId: correlationId));
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, ApiErrorResponse body)
    {
        // The audit middleware (inner) has already observed the thrown exception;
        // here we just render the response if nothing has been written yet.
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.Headers.TryAdd("X-Correlation-ID", body.CorrelationId ?? context.TraceIdentifier);
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        const string headerName = "X-Correlation-ID";
        if (context.Response.Headers.TryGetValue(headerName, out var outgoing)
            && !string.IsNullOrWhiteSpace(outgoing))
        {
            return outgoing.ToString();
        }

        if (context.Request.Headers.TryGetValue(headerName, out var incoming)
            && !string.IsNullOrWhiteSpace(incoming))
        {
            return incoming.ToString();
        }

        return context.TraceIdentifier;
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseWatchPartyExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
