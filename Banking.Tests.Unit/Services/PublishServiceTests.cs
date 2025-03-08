using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Implementations;
using Banking.Domain.Entities;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Caching;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Messaging.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

public class PublishServiceTests
{
    private readonly Mock<IKafkaProducer> _kafkaProducerMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IRedisCacheService> _redisCacheServiceMock;
    private readonly Mock<IOptions<KafkaOptions>> _kafkaOptionsMock;
    private readonly PublishService _publishService;
    private readonly KafkaOptions _kafkaOptions;

    public PublishServiceTests()
    {
        _kafkaProducerMock = new Mock<IKafkaProducer>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _redisCacheServiceMock = new Mock<IRedisCacheService>();
        _kafkaOptionsMock = new Mock<IOptions<KafkaOptions>>();

        _kafkaOptions = new KafkaOptions
        {
            TransactionsTopic = "test-transactions",
            NotificationsTopic = "test-notifications",
            BootstrapServers = "localhost:9092",
            ConsumerGroup = "test-group"
        };

        _kafkaOptionsMock.Setup(o => o.Value).Returns(_kafkaOptions);

        _publishService = new PublishService(
            _kafkaProducerMock.Object,
            _transactionRepositoryMock.Object,
            _redisCacheServiceMock.Object,
            _kafkaOptionsMock.Object
        );
    }

    /// <summary>
    /// Test that an exception is thrown when the from and to accounts are the same
    /// </summary>
    [Fact]
    public async Task PublishTransactionAsync_ShouldThrowException_WhenFromAndToAccountAreSame()
    {
        var accountId = Guid.NewGuid();
        // Arrange
        var transaction = new Transaction
        (
            Guid.NewGuid(),
            accountId,
            accountId, // The same account
            100,
            DateTime.UtcNow
        );

        // Act
        Func<Task> act = async () => await _publishService.PublishTransactionAsync(transaction);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Transaction from and to accounts are same");
    }

    /// <summary>
    /// Test that an exception is thrown when no accounts are provided
    /// </summary>
    [Fact]
    public async Task PublishTransactionAsync_ShouldThrowException_WhenNoAccountsProvided()
    {
        // Arrange
        var transaction = new Transaction
        (
            Guid.NewGuid(),
            null,
            null,
            100,
            DateTime.UtcNow
        );

        // Act
        Func<Task> act = async () => await _publishService.PublishTransactionAsync(transaction);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Transaction must have at least one account");
    }

    /// <summary>
    /// Test that an exception is thrown when the amount is non-positive
    /// </summary>
    [Fact]
    public async Task PublishTransactionAsync_ShouldThrowException_WhenAmountIsNonPositive()
    {
        // Arrange
        var transaction = new Transaction
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            DateTime.UtcNow
        );

        // Act
        Func<Task> act = async () => await _publishService.PublishTransactionAsync(transaction);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid transaction amount");
    }

    /// <summary>
    /// Test that an exception is thrown when there are insufficient funds
    /// </summary>
    [Fact]
    public async Task PublishTransactionAsync_ShouldThrowException_WhenInsufficientFunds()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var transaction = new Transaction
        (
            Guid.NewGuid(),
            fromAccountId,
            Guid.NewGuid(),
            500,
            DateTime.UtcNow
        );

        _redisCacheServiceMock.Setup(service => service.GetBalanceAsync(fromAccountId))
            .ReturnsAsync(100); // Balance is insufficient

        // Act
        Func<Task> act = async () => await _publishService.PublishTransactionAsync(transaction);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient funds. Current balance is 100");
    }

    /// <summary>
    /// Test that a transaction is published when valid
    /// </summary>
    [Fact]
    public async Task PublishTransactionAsync_ShouldPublishTransaction_WhenValid()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var transaction = new Transaction
        (
            Guid.NewGuid(),
            fromAccountId,
            toAccountId,
            200,
            DateTime.UtcNow
        );

        _redisCacheServiceMock.Setup(service => service.GetBalanceAsync(fromAccountId))
            .ReturnsAsync(500); // Balance is sufficient

        _transactionRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<TransactionEntity>()))
            .ReturnsAsync((TransactionEntity entity) => entity);

        _kafkaProducerMock.Setup(producer => producer.PublishAsync(_kafkaOptions.TransactionsTopic, It.IsAny<Transaction>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _publishService.PublishTransactionAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.FromAccountId.Should().Be(fromAccountId);
        result.ToAccountId.Should().Be(toAccountId);
        result.Amount.Should().Be(200);

        _transactionRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<TransactionEntity>()), Times.Once);
        _kafkaProducerMock.Verify(producer => producer.PublishAsync(_kafkaOptions.TransactionsTopic, It.IsAny<Transaction>()), Times.Once);
    }

    /// <summary>
    /// Test that a transaction notification is published to Kafka
    /// </summary>
    [Fact]
    public async Task PublishTransactionNotificationAsync_ShouldPublishToKafka()
    {
        // Arrange
        var notificationEvent = new TransactionNotificationEvent
        {
            FromUserId = Guid.NewGuid(),
            ToUserId = Guid.NewGuid(),
            Amount = 100,
            FromAccountNumber = "123456",
            ToAccountNumber = "654321",
            FromUserName = "John Doe",
            ToUserName = "Jane Doe",
            FromAccountBalance = 500,
            ToAccountBalance = 1000,
            Timestamp = DateTime.UtcNow
        };

        _kafkaProducerMock.Setup(producer => producer.PublishAsync(_kafkaOptions.NotificationsTopic, notificationEvent))
            .Returns(Task.CompletedTask);

        // Act
        await _publishService.PublishTransactionNotificationAsync(notificationEvent);

        // Assert
        _kafkaProducerMock.Verify(producer => producer.PublishAsync(_kafkaOptions.NotificationsTopic, notificationEvent), Times.Once);
    }
}
