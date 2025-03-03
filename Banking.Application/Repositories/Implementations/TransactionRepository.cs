using Banking.Application.Repositories.Interfaces;
using Banking.Domain.Entities;
using Banking.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Storage;

namespace Banking.Application.Repositories.Implementations
{
    public class TransactionRepository : BaseRepository<TransactionEntity>, ITransactionRepository
    {
        public TransactionRepository(ApplicationDbContext dbContext) : base(dbContext) { }
        
    }
}
