using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Banking.Application.Repositories.Interfaces;

public interface ITransactionRepository : IBaseRepository<TransactionEntity>
{
    Task<IDbContextTransaction> BeginTransactionAsync();
}
