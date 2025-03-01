using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Banking.Infrastructure.Caching;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<decimal?> GetBalanceAsync(Guid accountId)
    {
        var balanceKey = $"balance:{accountId}";
        var balanceString = await _cache.GetStringAsync(balanceKey);
        return balanceString != null ? JsonSerializer.Deserialize<decimal>(balanceString) : null;
    }

    public async Task UpdateBalanceAsync(Guid accountId, decimal newBalance)
    {
        var balanceKey = $"balance:{accountId}";
        var balanceString = JsonSerializer.Serialize(newBalance);
        await _cache.SetStringAsync(balanceKey, balanceString);
    }
}
