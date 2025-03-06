using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Messaging.Kafka;
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

                await KafkaHelper.CreateKafkaTopicAsync(
                    _solutionOptions.Kafka.BootstrapServers,
                    _solutionOptions.Kafka.TransactionsTopic);


                using var adminClient = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = _solutionOptions.Kafka.BootstrapServers
                }).Build();

                await KafkaHelper.WaitForTopicAsync(_solutionOptions.Kafka.TransactionsTopic, adminClient);


                _consumer.Subscribe(_solutionOptions.Kafka.TransactionsTopic);
                Log.Information($"KafkaConsumerService subscribed to {_solutionOptions.Kafka.TransactionsTopic}");
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
                        Log.Error($"Kafka consume error: {ex.Error.Reason}");
                        await Task.Delay(1000, stoppingToken);
                    }
                    catch (Exception ex)
                    {
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
