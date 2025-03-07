using Banking.Application.Messaging;
using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.Config;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Banking.Tests.Unit
{
    public class KafkaConsumerServiceTests
    {
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

            // 
            var kafkaOptions = new KafkaOptions
            {
                BootstrapServers = "dummy",
                TransactionsTopic = "test-topic",
                ConsumerGroup = "test-group",
                NotificationsTopic = "test-notifications-topic",

            };

            var options = Options.Create(kafkaOptions);

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

            var consumerService = new KafkaConsumerService(consumerConfig, options, serviceScopeFactoryMock.Object);

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



            // Act: 
            using (var cts = new CancellationTokenSource(1000))
            {
                await consumerService.StartAsync(cts.Token);

                await Task.Delay(500, cts.Token);
                await consumerService.StopAsync(cts.Token);
            }

            // Assert
            transactionServiceMock.Verify(ts => ts.ProcessTransactionAsync(fakeConsumeResult), Times.Once);
            consumerMock.Verify(c => c.Commit(fakeConsumeResult), Times.Once);
        }
    }
}
