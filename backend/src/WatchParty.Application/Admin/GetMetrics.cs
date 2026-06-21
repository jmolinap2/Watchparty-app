using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.State;
using WatchParty.Contracts.Admin;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Admin;

/// <summary>Composes the minimum V1 metrics (scope §11) from PostgreSQL and Redis.</summary>
public sealed record GetMetricsQuery : IQuery<Result<MetricsDto>>;

public sealed class GetMetricsQueryHandler(
    IUserQueries userQueries,
    IRoomQueries roomQueries,
    IChatQueries chatQueries,
    IReportQueries reportQueries,
    IPresenceStore presenceStore,
    IMetricsCounter metricsCounter)
    : IQueryHandler<GetMetricsQuery, Result<MetricsDto>>
{
    public async Task<Result<MetricsDto>> Handle(GetMetricsQuery query, CancellationToken cancellationToken)
    {
        var metrics = new MetricsDto(
            RegisteredUsers: await userQueries.CountUsersAsync(cancellationToken),
            ActiveUsers: await presenceStore.GetGlobalOnlineUserCountAsync(cancellationToken),
            RoomsCreated: await roomQueries.CountRoomsAsync(cancellationToken),
            ActiveRooms: await presenceStore.GetActiveRoomCountAsync(cancellationToken),
            MessagesSent: await chatQueries.CountMessagesAsync(cancellationToken),
            OpenReports: await reportQueries.CountOpenAsync(cancellationToken),
            ResolvedReports: await reportQueries.CountResolvedAsync(cancellationToken),
            PlaybackErrors: await metricsCounter.GetAsync(MetricKeys.PlaybackErrors, cancellationToken),
            SignalrReconnections: await metricsCounter.GetAsync(MetricKeys.SignalrReconnections, cancellationToken));

        return Result.Success(metrics);
    }
}
