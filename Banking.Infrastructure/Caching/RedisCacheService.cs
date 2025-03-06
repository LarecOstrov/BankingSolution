using Banking.Infrastructure.Caching;
using Banking.Infrastructure.Config;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDatabase _cache;
    private readonly SolutionOptions _solutionOptions;
    private readonly int _balanceLifetimeMinutes;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IOptions<SolutionOptions> solutionOptions)
    {
        _cache = redis.GetDatabase();
        _solutionOptions = solutionOptions.Value;
        _balanceLifetimeMinutes = _solutionOptions.Redis.BalanceLifetimeMinutes;
    }

    /// <summary>
    /// Get balance from cache
    /// </summary>
    /// <param name="accountId"></param>
    /// <returns>decimal balance</returns>
    public async Task<decimal?> GetBalanceAsync(Guid accountId)
    {
        var balanceKey = $"balance_{accountId}";
        var balanceString = await _cache.StringGetAsync(balanceKey);

        return balanceString.HasValue ? JsonSerializer.Deserialize<decimal>(balanceString.ToString()) : null;
    }

    /// <summary>
    /// Update balance in cache
    /// </summary>
    /// <param name="accountId"></param>
    /// <param name="newBalance"></param>
    /// <returns>Task</returns>
    public async Task UpdateBalanceAsync(Guid accountId, decimal newBalance)
    {
        var balanceKey = $"balance_{accountId}";
        var balanceString = JsonSerializer.Serialize(newBalance);
        await _cache.StringSetAsync(balanceKey, balanceString, TimeSpan.FromHours(_balanceLifetimeMinutes));
    }
}
