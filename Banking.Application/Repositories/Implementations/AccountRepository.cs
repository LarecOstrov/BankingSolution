using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Banking.Application.Repositories.Implementations;

public class AccountRepository : BaseRepository<AccountEntity>, IAccountRepository
{
    public AccountRepository(ApplicationDbContext dbContext) : base(dbContext) { }

    public async Task<AccountEntity?> GetByAccountNumberAsync(Guid accountId, Guid userId)
    {
        return await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);
    }
    /// <summary>
    /// Get account by Id with lock
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Task AccountEntity</returns>
    public async Task<AccountEntity?> GetByIdForUpdateWithLockAsync(Guid id)
    {
        try
        {
            return await _dbContext.Accounts.FromSqlRaw("SELECT * FROM Accounts WITH (UPDLOCK, ROWLOCK) WHERE Id = @Id",
                        new SqlParameter("@Id", id))
            .Include(a => a.User)
            .FirstOrDefaultAsync();
        }
        catch (DbUpdateException ex)
        {
            Log.Error(ex, "Database error when adding a user.");
            throw new Exception("Database error occurred.");
        }
    }

    /// <summary>
    /// Get account by Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Task AccountEntity</returns>
    public async Task<AccountEntity?> GetByIdForUpdateAsync(Guid id)
    {
        try
        {
            return await _dbContext.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync();
        }
        catch (DbUpdateException ex)
        {
            Log.Error(ex, "Database error when adding a user.");
            throw new Exception("Database error occurred.");
        }
    }

    /// <summary>
    /// Check if account exists
    /// </summary>
    /// <param name="accountNumber"></param>
    /// <returns>Taks bool</returns>
    public async Task<bool> ExistsAsync(string accountNumber)
    {
        try
        {
            return await _dbContext.Accounts.AnyAsync(a => a.AccountNumber == accountNumber);
        }
        catch (DbUpdateException ex)
        {
            Log.Error(ex, "Database error when checking if account exists.");
            throw new Exception("Database error occurred.");
        }
    }
}

