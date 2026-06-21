using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Common;
using WatchParty.Contracts.Reports;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Reports;

/// <summary>Admin: list reports, optionally filtered by status.</summary>
public sealed record GetReportsQuery(string? Status, int Page, int PageSize) : IQuery<Result<PagedResult<ReportDto>>>;

public sealed class GetReportsQueryHandler(IReportQueries reportQueries)
    : IQueryHandler<GetReportsQuery, Result<PagedResult<ReportDto>>>
{
    public async Task<Result<PagedResult<ReportDto>>> Handle(GetReportsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize < 1 ? 20 : query.PageSize, 1, 100);
        var result = await reportQueries.SearchAsync(query.Status, page, pageSize, cancellationToken);
        return Result.Success(result);
    }
}

public sealed record GetReportDetailQuery(Guid ReportId) : IQuery<Result<ReportDto>>;

public sealed class GetReportDetailQueryHandler(IReportQueries reportQueries)
    : IQueryHandler<GetReportDetailQuery, Result<ReportDto>>
{
    public async Task<Result<ReportDto>> Handle(GetReportDetailQuery query, CancellationToken cancellationToken)
    {
        var report = await reportQueries.GetByIdAsync(query.ReportId, cancellationToken);
        return report is null ? DomainErrors.Reports.NotFound : report;
    }
}

public sealed record GetMyReportsQuery(Guid UserId) : IQuery<Result<IReadOnlyList<ReportDto>>>;

public sealed class GetMyReportsQueryHandler(IReportQueries reportQueries)
    : IQueryHandler<GetMyReportsQuery, Result<IReadOnlyList<ReportDto>>>
{
    public async Task<Result<IReadOnlyList<ReportDto>>> Handle(GetMyReportsQuery query, CancellationToken cancellationToken)
    {
        var reports = await reportQueries.GetByReporterAsync(query.UserId, 100, cancellationToken);
        return Result.Success(reports);
    }
}
