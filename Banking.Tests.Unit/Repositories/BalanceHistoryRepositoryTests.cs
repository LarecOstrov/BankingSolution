using Banking.Application.Repositories.Implementations;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class BalanceHistoryRepositoryTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BalanceHistoryRepository _repository;

    public BalanceHistoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new BalanceHistoryRepository(_dbContext);
    }

    /// <summary>
    /// Check balance history when exists
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetBalanceHistoryByAccountIdAsync_ShouldReturnBalanceHistory_WhenExists()
    {
        // Arrange
        var user = new UserEntity 
        { 
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@test.com",
            PasswordHash = "hash"
        };

        var account = new AccountEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AccountNumber = "UA2038080552523943628122",
            Balance = 1000,
            User = user
        };

        var history1 = new BalanceHistoryEntity
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            TransactionId = Guid.NewGuid(),
            NewBalance = 900,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Account = account
        };

        var history2 = new BalanceHistoryEntity
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            TransactionId = Guid.NewGuid(),
            NewBalance = 800,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            Account = account
        };

        _dbContext.Users.Add(user);
        _dbContext.Accounts.Add(account);
        _dbContext.BalanceHistory.AddRange(history1, history2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetBalanceHistoryByAccountIdAsync(account.Id, user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(x => x.NewBalance == 900);
        result.Should().Contain(x => x.NewBalance == 800);
    }

    /// <summary>
    /// Check balance history when no history
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetBalanceHistoryByAccountIdAsync_ShouldReturnEmpty_WhenNoHistory()
    {
        // Arrange
        var user = new UserEntity 
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@test.com",
            PasswordHash = "hash" 
        };

        var account = new AccountEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AccountNumber = "UA2038080552523943628122",
            Balance = 1000,
            User = user
        };

        _dbContext.Users.Add(user);
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetBalanceHistoryByAccountIdAsync(account.Id, user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
