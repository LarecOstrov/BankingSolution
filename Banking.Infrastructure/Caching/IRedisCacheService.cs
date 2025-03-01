namespace Banking.Infrastructure.Caching;

public interface IRedisCacheService
{
    Task<decimal?> GetBalanceAsync(Guid accountId);
    Task UpdateBalanceAsync(Guid accountId, decimal newBalance);
}
