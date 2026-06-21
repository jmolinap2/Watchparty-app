using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Reports;
using WatchParty.Contracts.Common;
using WatchParty.Contracts.Reports;
using WatchParty.Domain.Reports;

namespace WatchParty.Infrastructure.Persistence.Queries;

public sealed class ReportQueries(WatchPartyDbContext dbContext) : IReportQueries
{
    public async Task<ReportDto?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken)
    {
        var report = await dbContext.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        return report?.ToDto();
    }

    public async Task<IReadOnlyList<ReportDto>> GetByReporterAsync(Guid reporterUserId, int limit, CancellationToken cancellationToken)
    {
        var reports = await dbContext.Reports.AsNoTracking()
            .Where(r => r.ReporterUserId == reporterUserId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return reports.Select(r => r.ToDto()).ToList();
    }

    public async Task<PagedResult<ReportDto>> SearchAsync(string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Reports.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReportStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(r => r.Status == parsed);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var reports = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReportDto>(reports.Select(r => r.ToDto()).ToList(), page, pageSize, total);
    }

    public Task<long> CountOpenAsync(CancellationToken cancellationToken) =>
        dbContext.Reports.AsNoTracking().LongCountAsync(r => r.Status == ReportStatus.Open, cancellationToken);

    public Task<long> CountResolvedAsync(CancellationToken cancellationToken) =>
        dbContext.Reports.AsNoTracking().LongCountAsync(r => r.Status == ReportStatus.Resolved, cancellationToken);
}
