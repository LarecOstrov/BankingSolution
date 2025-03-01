using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Application.Repositories.Implementations
{
    public class BalanceHistoryRepository : BaseRepository<BalanceHistoryEntity>, IBalanceHistoryRepository
    {
        public BalanceHistoryRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async Task<IEnumerable<BalanceHistoryEntity>> GetBalanceHistoryByAccountIdAsync(Guid accountId)
        {
            return await _dbContext.BalanceHistory
                .Where(bh => bh.AccountId == accountId)
                .ToListAsync();
        }
    }
}
