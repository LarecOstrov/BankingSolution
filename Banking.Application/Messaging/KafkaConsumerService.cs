using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Messaging.Kafka.Helpers;
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
    private readonly KafkaOptions _kafkaOptions;
    private readonly IKafkaHelper _kafkaHelper;

    public KafkaConsumerService(ConsumerConfig config,
        IKafkaHelper kafkaHelper,
        IOptions<KafkaOptions> kafkaOptions,
        IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _kafkaOptions = kafkaOptions.Value;
        _kafkaHelper = kafkaHelper;
        _consumer = new ConsumerBuilder<Null, string>(config)
            .SetErrorHandler((_, e) => Log.Error($"Kafka Consumer error: {e.Reason}"))
            .Build();
    }

    /// <summary>
    /// Executes the background service
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns>Task</returns>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            try
            {

                await _kafkaHelper.CreateKafkaTopicAsync(
                    _kafkaOptions.BootstrapServers,
                    _kafkaOptions.TransactionsTopic);


                using var adminClient = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = _kafkaOptions.BootstrapServers
                }).Build();

                await _kafkaHelper.WaitForTopicAsync(_kafkaOptions.TransactionsTopic, adminClient);


                _consumer.Subscribe(_kafkaOptions.TransactionsTopic);
                Log.Information($"KafkaConsumerService subscribed to {_kafkaOptions.TransactionsTopic}");
            }
            catch (Exception ex)
            {
                Log.Error($"Kafka initialization error: {ex.Message}");
                return;
            }

            try
            {
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

                        _consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        _consumer.Commit();
                        Log.Error($"Kafka consume error: {ex.Error.Reason}");
                        await Task.Delay(1000, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _consumer.Commit();
                        Log.Error($"General error in KafkaConsumerService: {ex.Message}");
                    }

                }
            }
            catch (OperationCanceledException)
            {
                Log.Information("KafkaConsumerService is stopping...");
            }
            finally
            {
                _consumer.Close();
            }
        }, stoppingToken);

    }
}
