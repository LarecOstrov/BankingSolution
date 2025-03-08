using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Serilog;

namespace Banking.Infrastructure.Messaging.Kafka.Helpers;

public class KafkaHelper : IKafkaHelper
{
    /// <summary>
    /// Waits for a Kafka topic to be available
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="adminClient"></param>
    /// <returns>Task</returns>
    public async Task WaitForTopicAsync(string topic, IAdminClient adminClient)
    {
        Log.Information($"Kafka start wait for topic {topic}");

        while (true)
        {
            try
            {
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
                if (metadata.Topics.Any(t => t.Topic == topic))
                {
                    Log.Information($"Topic {topic} is available.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Waiting for topic {topic}: {ex.Message}");
                await Task.Delay(5000);
                continue;
            }
        }
    }

    /// <summary>
    /// Creates a Kafka topic
    /// </summary>
    /// <param name="bootstrapServers"></param>
    /// <param name="topicName"></param>
    /// <returns>Task</returns>
    public async Task CreateKafkaTopicAsync(string bootstrapServers, string topicName)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();

        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            if (metadata.Topics.Any(t => t.Topic == topicName))
            {
                Log.Information($"Topic '{topicName}' already exists.");
                return;
            }

            await adminClient.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = topicName,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            });

            Log.Information($"Topic '{topicName}' created successfully.");
        }
        catch (CreateTopicsException e)
        {
            Log.Error($"Error creating topic '{topicName}': {e.Results[0].Error.Reason}");
        }
    }
}

