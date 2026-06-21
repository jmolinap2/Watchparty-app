using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using WatchParty.Contracts.Admin;
using WatchParty.Contracts.Common;
using WatchParty.Contracts.Identity;
using WatchParty.Contracts.Reports;

namespace WatchParty.Admin.Services;

/// <summary>
/// Thin typed wrapper over the WatchParty backend admin API. The bearer token is
/// taken from <see cref="AdminSession"/> so callers never thread it through by hand.
/// DTOs are reused from <c>WatchParty.Contracts</c> to keep a single source of truth.
/// </summary>
public sealed class AdminApiClient(HttpClient httpClient, AdminSession session, ILogger<AdminApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // ---- Auth --------------------------------------------------------------

    public async Task<AuthResponse> LoginAsync(string identifier, string password, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(new { identifier, password }, options: JsonOptions)
        };

        using var response = await SendAsync(request, "login", cancellationToken);
        await EnsureSuccessAsync(response, "login", cancellationToken);
        return await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions, cancellationToken)
               ?? throw new InvalidOperationException("Empty response from login.");
    }

    // ---- Reads (parallel-friendly, each call is independent) ---------------

    public Task<MetricsDto> GetMetricsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<MetricsDto>("api/admin/metrics", cancellationToken);

    public Task<PagedResult<AdminUserDto>> GetUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default) =>
        GetAsync<PagedResult<AdminUserDto>>(
            BuildQuery("api/admin/users", ("search", search), ("page", page), ("pageSize", pageSize)),
            cancellationToken);

    public Task<AdminUserDetailDto> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken = default) =>
        GetAsync<AdminUserDetailDto>($"api/admin/users/{userId}", cancellationToken);

    public Task<PagedResult<AdminRoomDto>> GetRoomsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default) =>
        GetAsync<PagedResult<AdminRoomDto>>(
            BuildQuery("api/admin/rooms", ("status", status), ("page", page), ("pageSize", pageSize)),
            cancellationToken);

    public Task<AdminRoomDto> GetRoomDetailAsync(Guid roomId, CancellationToken cancellationToken = default) =>
        GetAsync<AdminRoomDto>($"api/admin/rooms/{roomId}", cancellationToken);

    public Task<PagedResult<ReportDto>> GetReportsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default) =>
        GetAsync<PagedResult<ReportDto>>(
            BuildQuery("api/admin/reports", ("status", status), ("page", page), ("pageSize", pageSize)),
            cancellationToken);

    public Task<ReportDto> GetReportDetailAsync(Guid reportId, CancellationToken cancellationToken = default) =>
        GetAsync<ReportDto>($"api/admin/reports/{reportId}", cancellationToken);

    public Task<IReadOnlyList<AllowedDomainDto>> GetDomainsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<AllowedDomainDto>>("api/admin/allowed-domains", cancellationToken);

    public Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogSearchRequest request, CancellationToken cancellationToken = default) =>
        GetAsync<PagedResult<AuditLogDto>>(
            BuildQuery(
                "api/admin/audit-logs",
                ("search", request.Search),
                ("category", request.Category),
                ("action", request.Action),
                ("resource", request.Resource),
                ("hasException", request.HasException),
                ("page", request.Page),
                ("pageSize", request.PageSize)),
            cancellationToken);

    /// <summary>Loads everything the dashboard needs in parallel (single round-trip latency).</summary>
    public async Task<DashboardData> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var metrics = GetMetricsAsync(cancellationToken);
        var users = GetUsersAsync(null, 1, 5, cancellationToken);
        var rooms = GetRoomsAsync(null, 1, 5, cancellationToken);
        var reports = GetReportsAsync("Open", 1, 5, cancellationToken);

        await Task.WhenAll(metrics, users, rooms, reports);
        return new DashboardData(metrics.Result, users.Result, rooms.Result, reports.Result);
    }

    // ---- Commands ----------------------------------------------------------

    public Task BlockUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default) =>
        PostAsync($"api/admin/users/{userId}/block", new { reason }, cancellationToken);

    public Task UnblockUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        PostAsync($"api/admin/users/{userId}/unblock", null, cancellationToken);

    public Task SetUserRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default) =>
        SendBodyAsync(HttpMethod.Put, $"api/admin/users/{userId}/role", new { role }, cancellationToken);

    public Task CloseRoomAsync(Guid roomId, CancellationToken cancellationToken = default) =>
        PostAsync($"api/admin/rooms/{roomId}/close", null, cancellationToken);

    public Task ResolveReportAsync(Guid reportId, string note, CancellationToken cancellationToken = default) =>
        PostAsync($"api/admin/reports/{reportId}/resolve", new { note }, cancellationToken);

    public Task RejectReportAsync(Guid reportId, string note, CancellationToken cancellationToken = default) =>
        PostAsync($"api/admin/reports/{reportId}/reject", new { note }, cancellationToken);

    public Task AddAllowedDomainAsync(string host, CancellationToken cancellationToken = default) =>
        PostAsync("api/admin/allowed-domains", new { host }, cancellationToken);

    public Task ToggleDomainAsync(Guid id, bool enabled, CancellationToken cancellationToken = default) =>
        PostAsync($"api/admin/allowed-domains/{id}/{(enabled ? "enable" : "disable")}", null, cancellationToken);

    // ---- Transport ---------------------------------------------------------

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = Authorized(HttpMethod.Get, path);
        using var response = await SendAsync(request, $"GET {path}", cancellationToken);
        await EnsureSuccessAsync(response, $"GET {path}", cancellationToken);

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Empty response from {path}.");
    }

    private Task PostAsync(string path, object? body, CancellationToken cancellationToken) =>
        SendBodyAsync(HttpMethod.Post, path, body, cancellationToken);

    private async Task SendBodyAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = Authorized(method, path);
        request.Content = body is null
            ? new StringContent("{}", Encoding.UTF8, "application/json")
            : JsonContent.Create(body, options: JsonOptions);

        using var response = await SendAsync(request, $"{method} {path}", cancellationToken);
        await EnsureSuccessAsync(response, $"{method} {path}", cancellationToken);
    }

    private HttpRequestMessage Authorized(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(session.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        }

        return request;
    }

    private static string BuildQuery(string path, params (string Key, object? Value)[] parameters)
    {
        var builder = new StringBuilder(path);
        var first = true;

        foreach (var (key, value) in parameters)
        {
            var text = value switch
            {
                null => null,
                string s => string.IsNullOrWhiteSpace(s) ? null : s,
                bool b => b ? "true" : "false",
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };

            if (text is null)
            {
                continue;
            }

            builder.Append(first ? '?' : '&');
            builder.Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(text));
            first = false;
        }

        return builder.ToString();
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        string operation,
        CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                exception,
                "Admin API operation {Operation} timed out. BackendApi={BackendApi}.",
                operation,
                httpClient.BaseAddress);
            throw AdminApiException.Unavailable(
                "El API no respondió a tiempo.",
                operation,
                httpClient.BaseAddress,
                exception);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "Admin API operation {Operation} could not reach BackendApi={BackendApi}.",
                operation,
                httpClient.BaseAddress);
            throw AdminApiException.Unavailable(
                "No se pudo conectar con el API.",
                operation,
                httpClient.BaseAddress,
                exception);
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiError = TryReadApiError(body);
        var correlationId = apiError?.CorrelationId ?? GetCorrelationId(response);

        logger.LogWarning(
            "Admin API operation {Operation} failed. Status={StatusCode}; Code={Code}; CorrelationId={CorrelationId}; Body={Body}",
            operation,
            (int)response.StatusCode,
            apiError?.Code ?? "-",
            correlationId ?? "-",
            body);

        throw new AdminApiException(
            apiError?.Message ?? $"API returned {(int)response.StatusCode} {response.ReasonPhrase}.",
            operation,
            httpClient.BaseAddress,
            response.StatusCode,
            apiError?.Code,
            apiError?.Message,
            correlationId,
            body);
    }

    private static ApiErrorResponse? TryReadApiError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetCorrelationId(HttpResponseMessage response) =>
        response.Headers.TryGetValues("X-Correlation-ID", out var values)
            ? values.FirstOrDefault()
            : null;

    private sealed record ApiErrorResponse(
        string Code,
        string Message,
        IReadOnlyDictionary<string, string[]>? Details = null,
        string? CorrelationId = null);
}

public sealed class AdminApiException : Exception
{
    public AdminApiException(
        string message,
        string operation,
        Uri? backendApi,
        HttpStatusCode? statusCode = null,
        string? apiCode = null,
        string? apiMessage = null,
        string? correlationId = null,
        string? responseBody = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Operation = operation;
        BackendApi = backendApi;
        StatusCode = statusCode;
        ApiCode = apiCode;
        ApiMessage = apiMessage;
        CorrelationId = correlationId;
        ResponseBody = responseBody;
    }

    public string Operation { get; }
    public Uri? BackendApi { get; }
    public HttpStatusCode? StatusCode { get; }
    public string? ApiCode { get; }
    public string? ApiMessage { get; }
    public string? CorrelationId { get; }
    public string? ResponseBody { get; }
    public bool HasHttpResponse => StatusCode.HasValue;
    public bool IsUnauthorized => StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    public string StatusText => StatusCode is null
        ? "Sin respuesta HTTP"
        : $"{(int)StatusCode.Value} {StatusCode.Value}";

    public static AdminApiException Unavailable(
        string message,
        string operation,
        Uri? backendApi,
        Exception innerException) =>
        new(
            message,
            operation,
            backendApi,
            innerException: innerException);
}

/// <summary>Aggregate loaded in parallel for the dashboard overview.</summary>
public sealed record DashboardData(
    MetricsDto Metrics,
    PagedResult<AdminUserDto> RecentUsers,
    PagedResult<AdminRoomDto> ActiveRooms,
    PagedResult<ReportDto> OpenReports);
