using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;

namespace Banking.Application.Repositories.Implementations;

public class FailedTransactionRepository : BaseRepository<FailedTransactionEntity>, IFailedTransactionRepository
{
    public FailedTransactionRepository(ApplicationDbContext dbContext) : base(dbContext) { }
}
