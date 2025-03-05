using Banking.Domain.Entities;

namespace Banking.Application.Repositories.Interfaces;

public interface ITransactionRepository : IBaseRepository<TransactionEntity>
{
    Task<TransactionEntity?> GetByTransactionIdAsync(Guid transactionId, Guid userId);
}
