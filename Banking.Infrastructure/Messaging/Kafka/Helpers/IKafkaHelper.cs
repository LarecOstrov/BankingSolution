using Confluent.Kafka;

namespace Banking.Infrastructure.Messaging.Kafka.Helpers;

public interface IKafkaHelper
{
    Task WaitForTopicAsync(string topic, IAdminClient adminClient);
    Task CreateKafkaTopicAsync(string bootstrapServers, string topicName);
}
