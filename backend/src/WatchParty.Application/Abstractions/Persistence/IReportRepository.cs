using WatchParty.Domain.Reports;

namespace WatchParty.Application.Abstractions.Persistence;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Report report, CancellationToken cancellationToken);
    void Update(Report report);
}
