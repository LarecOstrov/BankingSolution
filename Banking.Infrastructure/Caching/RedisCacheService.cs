using Banking.Infrastructure.Caching;
using StackExchange.Redis;
using System.Text.Json;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDatabase _cache;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _cache = redis.GetDatabase();
    }

    public async Task<decimal?> GetBalanceAsync(Guid accountId)
    {
        var balanceKey = $"balance_{accountId}";
        var balanceString = await _cache.StringGetAsync(balanceKey);

        return balanceString.HasValue ? JsonSerializer.Deserialize<decimal>(balanceString.ToString()) : null;
    }

    public async Task UpdateBalanceAsync(Guid accountId, decimal newBalance)
    {
        var balanceKey = $"balance_{accountId}";
        var balanceString = JsonSerializer.Serialize(newBalance);
        await _cache.StringSetAsync(balanceKey, balanceString);
    }
}
