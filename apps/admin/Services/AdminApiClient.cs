using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace WatchParty.Admin.Services;

public sealed class AdminApiClient(HttpClient httpClient, ILogger<AdminApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

    public async Task<AdminSnapshot> LoadAsync(string token, CancellationToken cancellationToken = default)
    {
        var metrics = await GetAsync<MetricsDto>("api/admin/metrics", token, cancellationToken);
        var users = await GetAsync<PagedResult<AdminUserDto>>("api/admin/users?page=1&pageSize=20", token, cancellationToken);
        var rooms = await GetAsync<PagedResult<AdminRoomDto>>("api/admin/rooms?page=1&pageSize=20", token, cancellationToken);
        var reports = await GetAsync<PagedResult<ReportDto>>("api/admin/reports?page=1&pageSize=20", token, cancellationToken);
        var domains = await GetAsync<List<AllowedDomainDto>>("api/admin/allowed-domains", token, cancellationToken);
        var auditLogs = await GetAsync<PagedResult<AuditLogDto>>("api/admin/audit-logs?page=1&pageSize=20", token, cancellationToken);

        return new AdminSnapshot(metrics, users, rooms, reports, domains, auditLogs);
    }

    public Task BlockUserAsync(string token, Guid userId, string reason, CancellationToken cancellationToken = default) =>
        PostAsync($"api/admin/users/{userId}/block", token, new { reason }, cancellationToken);

    public Task UnblockUserAsync(string token, Guid userId, CancellationToken cancellationToken = default) =>
        PostAsync($"api/admin/users/{userId}/unblock", token, null, cancellationToken);

    public Task CloseRoomAsync(string token, Guid roomId, CancellationToken cancellationToken = default) =>
        PostAsync($"api/admin/rooms/{roomId}/close", token, null, cancellationToken);

    public Task ResolveReportAsync(string token, Guid reportId, string note, CancellationToken cancellationToken = default) =>
        PostAsync($"api/admin/reports/{reportId}/resolve", token, new { note }, cancellationToken);

    public Task RejectReportAsync(string token, Guid reportId, string note, CancellationToken cancellationToken = default) =>
        PostAsync($"api/admin/reports/{reportId}/reject", token, new { note }, cancellationToken);

    public Task AddAllowedDomainAsync(string token, string host, CancellationToken cancellationToken = default) =>
        PostAsync("api/admin/allowed-domains", token, new { host }, cancellationToken);

    public Task ToggleDomainAsync(string token, Guid id, bool enabled, CancellationToken cancellationToken = default) =>
        PostAsync($"api/admin/allowed-domains/{id}/{(enabled ? "enable" : "disable")}", token, null, cancellationToken);

    private async Task<T> GetAsync<T>(string path, string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await SendAsync(request, $"GET {path}", cancellationToken);
        await EnsureSuccessAsync(response, $"GET {path}", cancellationToken);

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Empty response from {path}.");
    }

    private async Task PostAsync(string path, string token, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = body is null
            ? new StringContent("{}", Encoding.UTF8, "application/json")
            : JsonContent.Create(body, options: JsonOptions);

        using var response = await SendAsync(request, $"POST {path}", cancellationToken);
        await EnsureSuccessAsync(response, $"POST {path}", cancellationToken);
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

public sealed record AdminSnapshot(
    MetricsDto Metrics,
    PagedResult<AdminUserDto> Users,
    PagedResult<AdminRoomDto> Rooms,
    PagedResult<ReportDto> Reports,
    IReadOnlyList<AllowedDomainDto> Domains,
    PagedResult<AuditLogDto> AuditLogs);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);

public sealed record MetricsDto(
    long RegisteredUsers,
    long ActiveUsers,
    long RoomsCreated,
    long ActiveRooms,
    long MessagesSent,
    long OpenReports,
    long ResolvedReports,
    long PlaybackErrors,
    long SignalrReconnections);

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsBlocked,
    bool EmailConfirmed,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc);

public sealed record AdminRoomDto(
    Guid Id,
    string Code,
    string Name,
    Guid HostUserId,
    string Status,
    int MemberCount,
    int OnlineCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ClosedAtUtc);

public sealed record AllowedDomainDto(Guid Id, string Host, bool IsEnabled, DateTimeOffset CreatedAtUtc);

public sealed record ReportDto(
    Guid Id,
    string Type,
    Guid ReporterUserId,
    Guid? TargetUserId,
    Guid? TargetMessageId,
    Guid? RoomId,
    string Reason,
    string Status,
    DateTimeOffset CreatedAtUtc,
    Guid? ResolvedByUserId,
    DateTimeOffset? ResolvedAtUtc,
    string? ResolutionNote);

public sealed record AuditLogDto(
    Guid Id,
    string Category,
    string Action,
    Guid? ActorUserId,
    string? TargetType,
    string? TargetId,
    string? Details,
    string? IpAddress,
    string? Resource,
    string? Operation,
    string? HttpMethod,
    string? RequestPath,
    int? StatusCode,
    long? DurationMs,
    string? UserAgent,
    string? CorrelationId,
    string? Exception,
    bool HasException,
    DateTimeOffset CreatedAtUtc);
