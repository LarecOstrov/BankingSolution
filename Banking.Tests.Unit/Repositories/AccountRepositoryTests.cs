using Banking.Application.Repositories.Implementations;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class AccountRepositoryTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AccountRepository _accountRepository;

    public AccountRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _accountRepository = new AccountRepository(_dbContext);
    }

    /// <summary>
    /// Check receiving an account by account number
    /// </summary>
    [Fact]
    public async Task GetByAccountNumberAsync_ShouldReturnAccount_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var account = new AccountEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = "UA2038080552523943628122",
            Balance = 1000
        };

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _accountRepository.GetByAccountNumberAsync(account.Id, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id);
        result.UserId.Should().Be(userId);
    }

    /// <summary>
    /// Check receiving an account by Id with lock
    /// </summary>
    [Fact]
    public async Task GetByIdForUpdateWithLockAsync_ShouldReturnAccount_WhenExists()
    {
        // Arrange
        var user = new UserEntity { Id = Guid.NewGuid(), FullName = "Test User", Email = "test@example.com", PasswordHash = "hash" };
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
        var result = await _accountRepository.GetByIdForUpdateAsync(account.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id);
        result.User.Should().NotBeNull();
        result.User.FullName.Should().Be("Test User");
    }

    /// <summary>
    /// Check if the account exists
    /// </summary>
    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenAccountExists()
    {
        // Arrange
        var account = new AccountEntity
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AccountNumber = "1234567890",
            Balance = 1000
        };

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        // Act
        var exists = await _accountRepository.ExistsAsync("1234567890");

        // Assert
        exists.Should().BeTrue();
    }

    /// <summary>
    /// Check if the account does not exist
    /// </summary>
    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenAccountDoesNotExist()
    {
        // Act
        var exists = await _accountRepository.ExistsAsync("nonexistent_account");

        // Assert
        exists.Should().BeFalse();
    }
}
