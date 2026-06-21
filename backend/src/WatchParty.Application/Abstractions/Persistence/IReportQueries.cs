using WatchParty.Contracts.Common;
using WatchParty.Contracts.Reports;

namespace WatchParty.Application.Abstractions.Persistence;

public interface IReportQueries
{
    Task<ReportDto?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportDto>> GetByReporterAsync(Guid reporterUserId, int limit, CancellationToken cancellationToken);

    /// <summary>Admin listing. <paramref name="status"/> filters by "Open"/"Resolved"/"Rejected" (null = all).</summary>
    Task<PagedResult<ReportDto>> SearchAsync(string? status, int page, int pageSize, CancellationToken cancellationToken);

    Task<long> CountOpenAsync(CancellationToken cancellationToken);
    Task<long> CountResolvedAsync(CancellationToken cancellationToken);
}
