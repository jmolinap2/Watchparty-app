using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Admin;
using WatchParty.Contracts.Admin;
using WatchParty.Contracts.Common;
using WatchParty.Domain.Admin;
using WatchParty.Infrastructure.Persistence;

namespace WatchParty.Infrastructure.Auditing;

public sealed class AuditLogReader(WatchPartyDbContext dbContext) : IAuditLogReader
{
    public async Task<PagedResult<AuditLogDto>> SearchAsync(
        AuditLogSearchRequest request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = dbContext.AuditLogs.AsNoTracking();

        if (request.StartDateUtc.HasValue)
        {
            query = query.Where(log => log.CreatedAtUtc >= request.StartDateUtc.Value);
        }

        if (request.EndDateUtc.HasValue)
        {
            query = query.Where(log => log.CreatedAtUtc <= request.EndDateUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Category)
            && Enum.TryParse<AuditCategory>(request.Category, ignoreCase: true, out var category))
        {
            query = query.Where(log => log.Category == category);
        }

        if (request.ActorUserId.HasValue)
        {
            query = query.Where(log => log.ActorUserId == request.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            var action = request.Action.Trim();
            query = query.Where(log => log.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(request.TargetType))
        {
            var targetType = request.TargetType.Trim();
            query = query.Where(log => log.TargetType != null && log.TargetType.Contains(targetType));
        }

        if (!string.IsNullOrWhiteSpace(request.Resource))
        {
            var resource = request.Resource.Trim();
            query = query.Where(log => log.Resource != null && log.Resource.Contains(resource));
        }

        if (!string.IsNullOrWhiteSpace(request.Operation))
        {
            var operation = request.Operation.Trim();
            query = query.Where(log => log.Operation != null && log.Operation.Contains(operation));
        }

        if (request.HasException == true)
        {
            query = query.Where(log => log.Exception != null && log.Exception != string.Empty);
        }
        else if (request.HasException == false)
        {
            query = query.Where(log => log.Exception == null || log.Exception == string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(log =>
                log.Action.Contains(search)
                || (log.TargetType != null && log.TargetType.Contains(search))
                || (log.TargetId != null && log.TargetId.Contains(search))
                || (log.Resource != null && log.Resource.Contains(search))
                || (log.Operation != null && log.Operation.Contains(search))
                || (log.RequestPath != null && log.RequestPath.Contains(search))
                || (log.Details != null && log.Details.Contains(search)));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(log => log.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => ToDto(log))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, page, pageSize, totalCount);
    }

    public async Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.AuditLogs
            .AsNoTracking()
            .Where(log => log.Id == id)
            .Select(log => ToDto(log))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static AuditLogDto ToDto(AuditLog log) =>
        new(
            log.Id,
            log.Category.ToString(),
            log.Action,
            log.ActorUserId,
            log.TargetType,
            log.TargetId,
            log.Details,
            log.IpAddress,
            log.Resource,
            log.Operation,
            log.HttpMethod,
            log.RequestPath,
            log.StatusCode,
            log.DurationMs,
            log.UserAgent,
            log.CorrelationId,
            log.Exception,
            !string.IsNullOrEmpty(log.Exception),
            log.CreatedAtUtc);
}
