using StackExchange.Redis;
using WatchParty.Application.Abstractions.State;

namespace WatchParty.Infrastructure.State;

public sealed class RedisMetricsCounter(IConnectionMultiplexer redis) : IMetricsCounter
{
    private readonly IDatabase _db = redis.GetDatabase();

    public Task IncrementAsync(string key, CancellationToken cancellationToken) =>
        _db.StringIncrementAsync(key);

    public async Task<long> GetAsync(string key, CancellationToken cancellationToken)
    {
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? 0 : (long)value;
    }
}
