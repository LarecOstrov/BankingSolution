using Banking.Infrastructure.Database.Entities;

namespace Banking.Application.Repositories.Interfaces;

public interface IBalanceHistoryRepository : IBaseRepository<BalanceHistoryEntity>
{
    Task<IEnumerable<BalanceHistoryEntity>> GetBalanceHistoryByAccountIdAsync(Guid accountId, Guid userId);
}
