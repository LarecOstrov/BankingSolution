using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Messaging.Kafka.Helpers;
using Banking.Infrastructure.WebSockets;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

public class KafkaNotificationConsumerServiceTests
{
    /// <summary>
    /// Test if the service can deserialize a notification and notify users
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task ExecuteAsync_ShouldDeserializeNotificationAndNotifyUsers()
    {
        // Arrange
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = "dummy",
            GroupId = "test-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        var kafkaOptions = new KafkaOptions
        {
            BootstrapServers = "dummy",
            NotificationsTopic = "notifications-topic",
            ConsumerGroup = "test-group",
            TransactionsTopic = "dummy-topic"
        };

        var options = Options.Create(kafkaOptions);

        var kafkaHelperMock = new Mock<IKafkaHelper>();
        kafkaHelperMock.Setup(kh => kh.CreateKafkaTopicAsync(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns(Task.CompletedTask);
        kafkaHelperMock.Setup(kh => kh.WaitForTopicAsync(It.IsAny<string>(), It.IsAny<IAdminClient>()))
                       .Returns(Task.CompletedTask);

        var webSocketServiceMock = new Mock<WebSocketService>();
        webSocketServiceMock
            .Setup(ws => ws.SendTransactionNotificationAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var consumerService = new KafkaNotificationConsumerService(
            kafkaHelperMock.Object,
            webSocketServiceMock.Object,
            options,
            consumerConfig);

        var notification = new TransactionNotificationEvent
        {
            FromUserId = Guid.NewGuid(),
            ToUserId = Guid.NewGuid(),
            Amount = 100m,
            FromUserName = "Alice",
            ToUserName = "Bob",
            FromAccountNumber = "ACC123",
            ToAccountNumber = "ACC456",
            FromAccountBalance = 900m,
            ToAccountBalance = 1100m,
            Timestamp = DateTime.UtcNow
        };

        var fakeMessageValue = JsonSerializer.Serialize(notification);
        var fakeConsumeResult = new ConsumeResult<Null, string>
        {
            Message = new Message<Null, string> { Value = fakeMessageValue }
        };

        var consumerMock = new Mock<IConsumer<Null, string>>();
        consumerMock.SetupSequence(c => c.Consume(It.IsAny<CancellationToken>()))
            .Returns(fakeConsumeResult)
            .Throws(new OperationCanceledException());
        consumerMock.Setup(c => c.Commit(fakeConsumeResult));

        var consumerField = typeof(KafkaNotificationConsumerService)
            .GetField("_consumer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        consumerField!.SetValue(consumerService, consumerMock.Object);

        // Act
        using (var cts = new CancellationTokenSource())
        {
            var serviceTask = consumerService.StartAsync(cts.Token);
            await Task.Delay(5000);
            cts.Cancel();
            await consumerService.StopAsync(CancellationToken.None);
            await serviceTask;
        }


        var expectedMessageForFrom = $"You sent {notification.Amount} to {notification.ToUserName}. Current balance is {notification.FromAccountBalance}.";
        var expectedMessageForTo = $"You received {notification.Amount} from account {notification.FromUserName}. Current balance is {notification.ToAccountBalance}.";

        webSocketServiceMock.Verify(ws =>
            ws.SendTransactionNotificationAsync(notification.FromUserId.Value, expectedMessageForFrom),
            Times.Once);
        webSocketServiceMock.Verify(ws =>
            ws.SendTransactionNotificationAsync(notification.ToUserId.Value, expectedMessageForTo),
            Times.Once);

        consumerMock.Verify(c => c.Commit(fakeConsumeResult), Times.Once);
    }
}