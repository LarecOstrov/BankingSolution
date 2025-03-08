using Banking.Application.Repositories.Implementations;
using Banking.Domain.Entities;
using Banking.Infrastructure.Database;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class TransactionRepositoryTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TransactionRepository _repository;

    public TransactionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new TransactionRepository(_dbContext);
    }
    /// <summary>
    /// Check getting a transaction by Id
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetByTransactionIdAsync_ShouldReturnTransaction_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transaction = new TransactionEntity
        {
            Id = Guid.NewGuid(),
            FromAccountId = userId,
            Amount = 100,
            Status = Banking.Domain.Enums.TransactionStatus.Pending
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTransactionIdAsync(transaction.Id, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(transaction.Id);
        result.FromAccountId.Should().Be(userId);
    }

    /// <summary>
    /// Check receiving null when transaction does not exist
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetByTransactionIdAsync_ShouldReturnNull_WhenTransactionDoesNotExist()
    {
        // Act
        var result = await _repository.GetByTransactionIdAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }
}
