using Banking.Application.Repositories.Interfaces;
using Banking.Domain.Entities;
using Banking.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Banking.Application.Repositories.Implementations
{
    public class TransactionRepository : BaseRepository<TransactionEntity>, ITransactionRepository
    {        
        public TransactionRepository(ApplicationDbContext dbContext) : base(dbContext){ }

        public async Task<TransactionEntity?> GetByTransactionIdAsync(Guid transactionId, Guid userId)
        {
            return await _dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.FromAccountId == userId);
        }
    }
}
