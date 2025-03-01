using Banking.Application.Services.Interfaces;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Caching;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Text.Json;

namespace Banking.Application.Messaging;

public class KafkaConsumerService : BackgroundService
{
    private readonly IConsumer<Null, string> _consumer;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public KafkaConsumerService(ConsumerConfig config, IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _consumer = new ConsumerBuilder<Null, string>(config)
            .SetErrorHandler((_, e) => Log.Error($"Kafka Consumer error: {e.Reason}"))
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("transactions");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                if (consumeResult == null) continue;

                using var scope = _serviceScopeFactory.CreateScope();
                var transactionService = scope.ServiceProvider.GetRequiredService<ITransactionService>();
                var redisCacheService = scope.ServiceProvider.GetRequiredService<IRedisCacheService>();

                Log.Information($"Processing transaction {consumeResult.Message.Value}...");
                var transaction = JsonSerializer.Deserialize<Transaction>(consumeResult.Message.Value);

                if (transaction == null)
                {
                    Log.Error("Received null transaction from Kafka");
                    continue;
                }

                var success = await transactionService.ProcessTransactionAsync(
                    transaction.FromAccountId, transaction.ToAccountId, transaction.Amount);

                if (success)
                {
                    if (transaction.FromAccountId != null)
                    {
                        await redisCacheService.UpdateBalanceAsync(transaction.FromAccountId.Value, -transaction.Amount);
                    }
                    if (transaction.ToAccountId != null)
                    {
                        await redisCacheService.UpdateBalanceAsync(transaction.ToAccountId.Value, transaction.Amount);
                    }
                }
                else
                {
                    Log.Warning($"Transaction {transaction.Id} failed due to insufficient funds");
                }
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
}
