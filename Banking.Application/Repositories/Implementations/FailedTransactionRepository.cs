using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Application.Repositories.Implementations;

public class FailedTransactionRepository : IFailedTransactionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public FailedTransactionRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    /// <summary>
    /// Add a failed transaction
    /// </summary>
    /// <param name="failedTransaction"></param>
    /// <returns>Task</returns>
    public async Task AddAsync(FailedTransactionEntity failedTransaction)
    {
        await _dbContext.FailedTransactions.AddAsync(failedTransaction);
        await _dbContext.SaveChangesAsync();
    }
    /// <summary>
    /// Get all failed transactions
    /// </summary>
    /// <returns>IQueryable of FailedTransactionEntity</returns>
    public IQueryable<FailedTransactionEntity> GetAll()
    {
        return _dbContext.FailedTransactions.AsQueryable();
    }
}
