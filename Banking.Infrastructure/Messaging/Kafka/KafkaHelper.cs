using Confluent.Kafka;
using Serilog;

namespace Banking.Infrastructure.Messaging.Kafka
{
    public static class KafkaHelper
    {
        /// <summary>
        /// Waits for a Kafka topic to be available
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="adminClient"></param>
        /// <returns>Task</returns>
        public static async Task WaitForTopicAsync(string topic, IAdminClient adminClient)
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
    }
}
