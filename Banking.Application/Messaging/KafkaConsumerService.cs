using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Database.Entities;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Banking.Application.Messaging;

public class KafkaConsumerService : BackgroundService
{
    private readonly IConsumer<Null, string> _consumer;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SolutionOptions _solutionOptions;

    public KafkaConsumerService(ConsumerConfig config,
        IOptions<SolutionOptions> solutionOptions,
        IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _solutionOptions = solutionOptions.Value;
        _consumer = new ConsumerBuilder<Null, string>(config)
            .SetErrorHandler((_, e) => Log.Error($"Kafka Consumer error: {e.Reason}"))
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        using (var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _solutionOptions.Kafka.BootstrapServers
        }).Build())
        {
            await WaitForTopicAsync(_solutionOptions.Kafka.TransactionsTopic, adminClient);
        }

        _consumer.Subscribe(_solutionOptions.Kafka.TransactionsTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                if (consumeResult == null) continue;

                using var scope = _serviceScopeFactory.CreateScope();
                var transactionService = scope.ServiceProvider.GetRequiredService<ITransactionService>();
                
                Log.Information($"Received transaction from Kafka: {consumeResult.Message.Value}");

                await transactionService.ProcessTransactionAsync(consumeResult);
            }
            catch (ConsumeException ex)
            {
                Log.Error($"Kafka consume error: {ex.Error.Reason}");
                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                Log.Error($"General error in KafkaConsumerService: {ex.Message}");
            }
        }

        _consumer.Close();
    }

    private async Task WaitForTopicAsync(string topic, IAdminClient adminClient)
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
