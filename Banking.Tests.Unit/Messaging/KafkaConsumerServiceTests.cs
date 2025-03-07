using Banking.Application.Messaging;
using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Messaging.Kafka.Helpers;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;


public class KafkaConsumerServiceTests
{
    /// <summary>
    /// Test if the service can consume a message and process it
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task ExecuteAsync_ShouldConsumeProcessMessageAndCommit()
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
            TransactionsTopic = "test-topic",
            ConsumerGroup = "test-group",
            NotificationsTopic = "test-notifications-topic",
        };

        var options = Options.Create(kafkaOptions);

        var kafkaHelperMock = new Mock<IKafkaHelper>();
        kafkaHelperMock.Setup(kh => kh.CreateKafkaTopicAsync(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns(Task.CompletedTask);
        kafkaHelperMock.Setup(kh => kh.WaitForTopicAsync(It.IsAny<string>(), It.IsAny<IAdminClient>()))
                       .Returns(Task.CompletedTask);

        var transactionServiceMock = new Mock<ITransactionService>();
        transactionServiceMock
            .Setup(ts => ts.ProcessTransactionAsync(It.IsAny<ConsumeResult<Null, string>>()))
            .ReturnsAsync(true);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ITransactionService)))
            .Returns(transactionServiceMock.Object);

        var serviceScopeMock = new Mock<IServiceScope>();
        serviceScopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        serviceScopeFactoryMock
            .Setup(s => s.CreateScope())
            .Returns(serviceScopeMock.Object);

        var consumerService = new KafkaConsumerService(consumerConfig, kafkaHelperMock.Object, options, serviceScopeFactoryMock.Object);

        var fakeMessageValue = "{\"some\":\"data\"}";
        var fakeConsumeResult = new ConsumeResult<Null, string>
        {
            Message = new Message<Null, string> { Value = fakeMessageValue }
        };

        var consumerMock = new Mock<IConsumer<Null, string>>();
        consumerMock.SetupSequence(c => c.Consume(It.IsAny<CancellationToken>()))
            .Returns(fakeConsumeResult)
            .Throws(new OperationCanceledException());
        consumerMock.Setup(c => c.Commit(fakeConsumeResult));

        var consumerField = typeof(KafkaConsumerService)
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

        // Assert
        transactionServiceMock.Verify(ts => ts.ProcessTransactionAsync(
            It.Is<ConsumeResult<Null, string>>(cr => cr.Message.Value == fakeMessageValue)), Times.Once);

        consumerMock.Verify(c => c.Commit(fakeConsumeResult), Times.Once);
    }

}
