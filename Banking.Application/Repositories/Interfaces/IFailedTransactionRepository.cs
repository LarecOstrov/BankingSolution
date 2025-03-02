using Banking.Infrastructure.Database.Entities;

namespace Banking.Application.Repositories.Interfaces;

public interface IFailedTransactionRepository
{
    Task AddAsync(FailedTransactionEntity failedTransaction);
    IQueryable<FailedTransactionEntity> GetAll();
}
