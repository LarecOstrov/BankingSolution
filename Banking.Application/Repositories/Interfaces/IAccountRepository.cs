using Banking.Infrastructure.Database.Entities;

namespace Banking.Application.Repositories.Interfaces;

public interface IAccountRepository : IBaseRepository<AccountEntity>
{
    Task<AccountEntity?> GetByAccountNumberAsync(string accountNumber);
    Task<AccountEntity?> GetByIdForUpdateWithLockAsync(Guid id);
}
