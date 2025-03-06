using Banking.Infrastructure.Database.Entities;

namespace Banking.Application.Repositories.Interfaces;

public interface IAccountRepository : IBaseRepository<AccountEntity>
{
    Task<AccountEntity?> GetByAccountNumberAsync(Guid accountId, Guid userId);
    Task<AccountEntity?> GetByIdForUpdateWithLockAsync(Guid id);
    Task<bool> ExistsAsync(string accountNumber);
}
