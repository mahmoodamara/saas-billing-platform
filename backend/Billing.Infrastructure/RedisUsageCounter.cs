using Billing.Application;
using StackExchange.Redis;

namespace Billing.Infrastructure;

public sealed class RedisUsageCounter(IConnectionMultiplexer redis) : IUsageCounter
{
    private static string Key(Guid organizationId, DateTime periodStart) => $"usage:{organizationId}:{periodStart:yyyy-MM}";

    public async Task<long?> GetAsync(Guid organizationId, DateTime periodStart)
    {
        var value = await redis.GetDatabase().StringGetAsync(Key(organizationId, periodStart));
        return value.HasValue && long.TryParse(value.ToString(), out var n) ? n : null;
    }

    public async Task<long> IncrementAsync(Guid organizationId, DateTime periodStart, int quantity)
    {
        var db = redis.GetDatabase();
        var result = await db.StringIncrementAsync(Key(organizationId, periodStart), quantity);
        await db.KeyExpireAsync(Key(organizationId, periodStart), TimeSpan.FromDays(62));
        return result;
    }

    public async Task SetAsync(Guid organizationId, DateTime periodStart, long quantity)
    {
        await redis.GetDatabase().StringSetAsync(Key(organizationId, periodStart), quantity, TimeSpan.FromDays(62));
    }
}
