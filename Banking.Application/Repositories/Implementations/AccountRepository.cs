using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Application.Repositories.Implementations;

public class AccountRepository : BaseRepository<AccountEntity>, IAccountRepository
{
    public AccountRepository(ApplicationDbContext dbContext) : base(dbContext) { }

    public async Task<AccountEntity?> GetByAccountNumberAsync(string accountNumber)
    {
        return await _dbContext.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<AccountEntity?> GetByIdForUpdateWithLockAsync(Guid id)
        => await _dbContext.Accounts.FromSqlRaw($"SELECT * FROM Accounts WITH (UPDLOCK, ROWLOCK) WHERE Id = 'id'", id)
        .FirstOrDefaultAsync();
}

