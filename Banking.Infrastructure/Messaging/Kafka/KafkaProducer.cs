using Confluent.Kafka;
using Serilog;
using System.Text.Json;

namespace Banking.Infrastructure.Messaging.Kafka;

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<Null, string> _producer;

    public KafkaProducer(ProducerConfig config)
    {
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, T message)
    {
        var messageString = JsonSerializer.Serialize(message);
        await _producer.ProduceAsync(topic, new Message<Null, string> { Value = messageString });
        Log.Information($"Published message to Kafka topic {topic}: {messageString}");
    }
}
