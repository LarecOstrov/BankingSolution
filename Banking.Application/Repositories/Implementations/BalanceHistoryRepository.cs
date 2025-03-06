using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Application.Repositories.Implementations
{
    public class BalanceHistoryRepository : BaseRepository<BalanceHistoryEntity>, IBalanceHistoryRepository
    {
        public BalanceHistoryRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        /// <summary>
        /// Get balance history by account Id
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns>Task of IEnumerable of BalanceHistoryEntity</returns>
        public async Task<IEnumerable<BalanceHistoryEntity>> GetBalanceHistoryByAccountIdAsync(Guid accountId, Guid userId)
        {
            return await _dbContext.BalanceHistory
                .Where(bh => bh.AccountId == accountId && bh.Account.UserId == userId)
                .ToListAsync();
        }
    }
}
