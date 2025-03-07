using System;
using System.Text.Json;
using System.Threading.Tasks;
using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Implementations;
using Banking.Application.Services.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Caching;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Database.Entities;
using Banking.Infrastructure.WebSockets;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Banking.Tests
{
    public class TransactionServiceTests
    {
        private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
        private readonly Mock<IFailedTransactionRepository> _failedTransactionRepositoryMock;
        private readonly Mock<IBalanceHistoryRepository> _balanceHistoryRepositoryMock;
        private readonly Mock<IAccountRepository> _accountRepositoryMock;
        private readonly Mock<IRedisCacheService> _redisCacheServiceMock;
        private readonly Mock<IPublishService> _publishServiceMock;
        private readonly TransactionService _transactionService;

        public TransactionServiceTests()
        {
            _transactionRepositoryMock = new Mock<ITransactionRepository>();
            _failedTransactionRepositoryMock = new Mock<IFailedTransactionRepository>();
            _balanceHistoryRepositoryMock = new Mock<IBalanceHistoryRepository>();
            _accountRepositoryMock = new Mock<IAccountRepository>();
            _redisCacheServiceMock = new Mock<IRedisCacheService>();
            _publishServiceMock = new Mock<IPublishService>();

            var retryPolicy = Options.Create(new TransactionRetryPolicy { MaxRetries = 3, DelayMilliseconds = 10 });

            _transactionService = new TransactionService(
                _transactionRepositoryMock.Object,
                _failedTransactionRepositoryMock.Object,
                _balanceHistoryRepositoryMock.Object,
                _accountRepositoryMock.Object,
                _redisCacheServiceMock.Object,
                _publishServiceMock.Object,
                retryPolicy);
        }

        /// <summary>
        /// Creates a ConsumeResult with a Transaction message
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns>CosumeResult</returns>
        private ConsumeResult<Null, string> CreateConsumeResult(Transaction transaction)
        {
            var message = new Message<Null, string> { Value = JsonSerializer.Serialize(transaction) };
            return new ConsumeResult<Null, string> { Message = message };
        }

        /// <summary>
        /// Test that a transaction is processed successfully
        /// </summary>
        /// <returns>Taks</returns>
        [Fact]
        public async Task ProcessTransactionAsync_Success()
        {
            // Arrange
            var transaction = new Transaction
            (
                Guid.NewGuid(),               
                Guid.NewGuid(),
                Guid.NewGuid(),
                100,
                DateTime.UtcNow,
                TransactionStatus.Pending
            );

            var consumeResult = CreateConsumeResult(transaction);

            var transactionEntity = new TransactionEntity
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Status = TransactionStatus.Pending
            };

            _transactionRepositoryMock.Setup(x => x.BeginTransactionAsync())
                .ReturnsAsync(Mock.Of<IDbContextTransaction>());
            _transactionRepositoryMock.Setup(x => x.GetByIdAsync(transaction.Id))
                .ReturnsAsync(transactionEntity);
            _transactionRepositoryMock.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            var fromAccount = new AccountEntity
            {
                Id = transaction.FromAccountId!.Value,
                Balance = 200,
                UserId = Guid.NewGuid(),
                AccountNumber = "FR1234567891234567891234",
                User = new UserEntity { FullName = "From User", Email = "fromuser@example.com", PasswordHash = "hash" }
            };

            var toAccount = new AccountEntity
            {
                Id = transaction.ToAccountId!.Value,
                Balance = 50,
                UserId = Guid.NewGuid(),
                AccountNumber = "TO1234567891234567891234",
                User = new UserEntity { FullName = "To User", Email = "fromuser@example.com", PasswordHash = "hash" }
            };

            _accountRepositoryMock.Setup(x => x.GetByIdForUpdateWithLockAsync(transaction.FromAccountId.Value))
                .ReturnsAsync(fromAccount);
            _accountRepositoryMock.Setup(x => x.GetByIdForUpdateWithLockAsync(transaction.ToAccountId.Value))
                .ReturnsAsync(toAccount);

            _balanceHistoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<BalanceHistoryEntity>()))
                .ReturnsAsync((BalanceHistoryEntity entity) => entity);
            _redisCacheServiceMock.Setup(x => x.UpdateBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>()))
                .Returns(Task.CompletedTask);
            _publishServiceMock.Setup(x => x.PublishTransactionNotificationAsync(It.IsAny<TransactionNotificationEvent>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _transactionService.ProcessTransactionAsync(consumeResult);

            // Assert
            Assert.True(result);
            Assert.Equal(TransactionStatus.Completed, transactionEntity.Status);
            _transactionRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.AtLeastOnce);
        }

        /// <summary>
        /// Test transaction processing when transaction not found
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProcessTransactionAsync_TransactionNotFound()
        {
            // Arrange
            var transaction = new Transaction
            (
                Guid.NewGuid(),               
                Guid.NewGuid(),
                Guid.NewGuid(),
                100,
                DateTime.UtcNow,
                TransactionStatus.Pending
            );

            var consumeResult = CreateConsumeResult(transaction);

            _transactionRepositoryMock.Setup(x => x.BeginTransactionAsync())
                .ReturnsAsync(Mock.Of<IDbContextTransaction>());
            _transactionRepositoryMock.Setup(x => x.GetByIdAsync(transaction.Id))
                .ReturnsAsync((TransactionEntity?)null);

            // Act
            var result = await _transactionService.ProcessTransactionAsync(consumeResult);

            // Assert
            Assert.False(result);
            _failedTransactionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<FailedTransactionEntity>()), Times.Once);
        }

        /// <summary>
        /// Test transaction processing when unufficient funds
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProcessTransactionAsync_InsufficientFunds()
        {
            // Arrange
            var transaction = new Transaction
            (
                Guid.NewGuid(),                
                Guid.NewGuid(),
                Guid.NewGuid(),
                500, // Amount is greater than balance
                DateTime.UtcNow
            );

            var consumeResult = CreateConsumeResult(transaction);

            var transactionEntity = new TransactionEntity
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Status = TransactionStatus.Pending
            };

            _transactionRepositoryMock.Setup(x => x.BeginTransactionAsync())
                .ReturnsAsync(Mock.Of<IDbContextTransaction>());
            _transactionRepositoryMock.Setup(x => x.GetByIdAsync(transaction.Id))
                .ReturnsAsync(transactionEntity);
            _transactionRepositoryMock.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            var fromAccount = new AccountEntity
            {
                Id = transaction.FromAccountId!.Value,
                Balance = 100, // unufficient funds
                UserId = Guid.NewGuid(),
                AccountNumber = "FR1234567891234567891234",
                User = new UserEntity {FullName = "From User", Email = "fromuser@example.com", PasswordHash = "hash" }
            };

            _accountRepositoryMock.Setup(x => x.GetByIdForUpdateWithLockAsync(transaction.FromAccountId.Value))
                .ReturnsAsync(fromAccount);
            _accountRepositoryMock.Setup(x => x.GetByIdForUpdateWithLockAsync(transaction.ToAccountId!.Value))
                .ReturnsAsync(new AccountEntity
                {
                    Id = transaction.ToAccountId!.Value,
                    Balance = 50,
                    UserId = Guid.NewGuid(),
                    AccountNumber = "TO1234567891234567891234",
                    User = new UserEntity {FullName = "To User", Email = "fromuser@example.com", PasswordHash = "hash" }
                });

            // Act
            var result = await _transactionService.ProcessTransactionAsync(consumeResult);

            // Assert
            Assert.False(result);
            Assert.Equal(TransactionStatus.Failed, transactionEntity.Status);
            _failedTransactionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<FailedTransactionEntity>()), Times.Once);
        }

        /// <summary>
        /// Test transaction processing when queue message deserialization failure
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProcessTransactionAsync_DeserializationFailure()
        {
            // Arrange: Create a message with invalid JSON
            var message = new Message<Null, string> { Value = "Invalid JSON" };
            var consumeResult = new ConsumeResult<Null, string> { Message = message };

            // Act
            var result = await _transactionService.ProcessTransactionAsync(consumeResult);

            // Assert
            Assert.False(result);
            _failedTransactionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<FailedTransactionEntity>()), Times.Once);
        }
    }
}
