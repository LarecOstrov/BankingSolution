using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Messaging.Kafka.Helpers;
using Banking.Infrastructure.WebSockets;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;

public class KafkaNotificationConsumerService : BackgroundService
{
    private readonly IConsumer<Null, string> _consumer;
    private readonly IWebSocketService _webSocketService;
    private readonly KafkaOptions _kafkaOptions;
    private readonly IKafkaHelper _kafkaHelper;

    public KafkaNotificationConsumerService(
        IKafkaHelper kafkaHelper,
        IWebSocketService webSocketService,
        IOptions<KafkaOptions> kafkaOptions,
        ConsumerConfig config)
    {
        _webSocketService = webSocketService;
        _kafkaOptions = kafkaOptions.Value;
        _kafkaHelper = kafkaHelper;
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
                await _kafkaHelper.CreateKafkaTopicAsync(
                    _kafkaOptions.BootstrapServers,
                    _kafkaOptions.NotificationsTopic);

                using var adminClient = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = _kafkaOptions.BootstrapServers
                }).Build();

                await _kafkaHelper.WaitForTopicAsync(_kafkaOptions.NotificationsTopic, adminClient);

                _consumer.Subscribe(_kafkaOptions.NotificationsTopic);
                Log.Information($"KafkaNotificationConsumerService subscribed to {_kafkaOptions.NotificationsTopic}");
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

                        var notification = JsonSerializer
                            .Deserialize<TransactionNotificationEvent>(consumeResult.Message.Value);
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
