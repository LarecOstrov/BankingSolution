using Banking.Infrastructure.Messaging.Kafka;
using Confluent.Kafka;
using Moq;
using System.Reflection;
using System.Text.Json;


public class KafkaProducerTests
{    
    /// <summary>
    /// PublishAsync should serialize and publish message
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task PublishAsync_ShouldSerializeAndPublishMessage()
    {
        // Arrange
        var topic = "test-topic";
        var messageObject = new { Text = "Hello, Kafka!" };
        var expectedMessageString = JsonSerializer.Serialize(messageObject);

        var mockProducer = new Mock<IProducer<Null, string>>();

        mockProducer.Setup(p => p.ProduceAsync(
                topic,
                It.Is<Message<Null, string>>(m => m.Value == expectedMessageString),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<Null, string>
            {
                Topic = topic,
                Message = new Message<Null, string> { Value = expectedMessageString }
            })
            .Verifiable();

        var kafkaProducer = new KafkaProducerForTest(mockProducer.Object);

        // Act
        await kafkaProducer.PublishAsync(topic, messageObject);

        // Assert
        mockProducer.Verify(p => p.ProduceAsync(
            topic,
            It.Is<Message<Null, string>>(m => m.Value == expectedMessageString),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private class KafkaProducerForTest : KafkaProducer
    {
        public KafkaProducerForTest(IProducer<Null, string> testProducer)
            : base(new ProducerConfig())
        {

            var field = typeof(KafkaProducer)
                .GetField("_producer", BindingFlags.NonPublic | BindingFlags.Instance);
            field!.SetValue(this, testProducer);
        }
    }

}
