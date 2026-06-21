using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Reports;

namespace WatchParty.Infrastructure.Persistence.Repositories;

public sealed class ReportRepository(WatchPartyDbContext dbContext) : IReportRepository
{
    public Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Reports.FirstOrDefaultAsync(report => report.Id == id, cancellationToken);

    public async Task AddAsync(Report report, CancellationToken cancellationToken) =>
        await dbContext.Reports.AddAsync(report, cancellationToken);

    public void Update(Report report) => dbContext.Reports.Update(report);
}
