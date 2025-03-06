using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Messaging.Kafka;
using Banking.Infrastructure.WebSockets;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;

public class KafkaNotificationConsumerService : BackgroundService
{
    private readonly IConsumer<Null, string> _consumer;
    private readonly WebSocketService _webSocketService;
    private readonly SolutionOptions _solutionOptions;

    public KafkaNotificationConsumerService(
        WebSocketService webSocketService,
        IOptions<SolutionOptions> solutionOptions,
        ConsumerConfig config)
    {
        _webSocketService = webSocketService;
        _solutionOptions = solutionOptions.Value;
        _consumer = new ConsumerBuilder<Null, string>(config)
            .SetErrorHandler((_, e) => Log.Error($"Kafka Consumer error: {e.Reason}"))
            .Build();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                await KafkaHelper.CreateKafkaTopicAsync(
                    _solutionOptions.Kafka.BootstrapServers,
                    _solutionOptions.Kafka.NotificationsTopic);

                using var adminClient = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = _solutionOptions.Kafka.BootstrapServers
                }).Build();

                await KafkaHelper.WaitForTopicAsync(_solutionOptions.Kafka.NotificationsTopic, adminClient);

                _consumer.Subscribe(_solutionOptions.Kafka.NotificationsTopic);
                Log.Information($"KafkaNotificationConsumerService subscribed to {_solutionOptions.Kafka.NotificationsTopic}");
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

                        Log.Information("Received notification from Kafka: {Value}", consumeResult.Message.Value);

                        var notification = JsonSerializer.Deserialize<TransactionNotificationEvent>(consumeResult.Message.Value);
                        if (notification != null)
                        {
                            await NotifyUsersAsync(notification);
                        }

                        _consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        _consumer.Commit();
                        Log.Error($"Kafka consume notification error: {ex.Error.Reason}");
                        await Task.Delay(1000, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _consumer.Commit();
                        Log.Error($"General error in KafkaNotificationConsumerService: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.Information("KafkaNotificationConsumerService is stopping...");
            }
            finally
            {
                _consumer.Close();
            }
        }, stoppingToken);
    }

    #region Private Methods
    private async Task NotifyUsersAsync(TransactionNotificationEvent e)
    {
        if (e.FromUserId != null)
        {
            if (e.ToUserId != null)
            {
                await _webSocketService.SendTransactionNotificationAsync(
                    e.FromUserId.Value, $"You sent {e.Amount} to {e.ToUserName}." +
                    $" Current balance is {e.FromAccountBalance}.");
            }
            else
            {
                await _webSocketService.SendTransactionNotificationAsync(
                    e.FromUserId.Value, $"Withdrawal of {e.Amount} from account {e.FromAccountNumber}." +
                    $" Current balance is {e.FromAccountBalance}.");
            }
        }

        if (e.ToUserId != null)
        {
            if (e.FromUserId != null)
            {
                await _webSocketService.SendTransactionNotificationAsync(
                e.ToUserId.Value, $"You received {e.Amount} from account {e.FromUserName}." +
                $" Current balance is {e.ToAccountBalance}.");
            }
            else
            {
                await _webSocketService.SendTransactionNotificationAsync(
                    e.ToUserId.Value, $"Your account {e.ToAccountNumber} funds received {e.Amount}." +
                    $" Current balance is {e.ToAccountBalance}!");
            }
        }
    }
    #endregion
}
