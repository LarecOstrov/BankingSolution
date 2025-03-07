using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Caching;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Messaging.Kafka;
using Microsoft.Extensions.Options;
using Serilog;

namespace Banking.Application.Services.Implementations;

public class PublishService : IPublishService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IRedisCacheService _redisCacheService;
    private readonly KafkaOptions _kafkaOptions;

    public PublishService(
        IKafkaProducer kafkaProducer,
        ITransactionRepository transactionRepository,
        IRedisCacheService redisCacheService,
        IOptions<KafkaOptions> kafkaOptions)
    {
        _kafkaProducer = kafkaProducer;
        _transactionRepository = transactionRepository;
        _redisCacheService = redisCacheService;
        _kafkaOptions = kafkaOptions.Value;
    }

    /// <summary>
    /// Publishes a transaction to Kafka
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns>Task of Transaction</returns>
    public async Task<Transaction> PublishTransactionAsync(Transaction transaction)
    {
        if (transaction.FromAccountId == null && transaction.ToAccountId == null)
        {
            Log.Warning("Transaction must have at least one account");
            throw new InvalidOperationException("Transaction must have at least one account");
        }

        if (transaction.FromAccountId == transaction.ToAccountId)
        {
            Log.Warning("Transaction from and to accounts are same");
            throw new InvalidOperationException("Transaction from and to accounts are same");
        }

        if (transaction.Amount <= 0)
        {
            Log.Warning($"Invalid transaction amount: {transaction.Amount}");
            throw new InvalidOperationException("Invalid transaction amount");
        }

        if (transaction.FromAccountId != null)
        {
            var balanceKey = $"balance:{transaction.FromAccountId}";

            var balance = await _redisCacheService.GetBalanceAsync(transaction.FromAccountId.Value);

            if (balance == null)
            {
                Log.Information($"Balance for account {transaction.FromAccountId} not found in cache");
            }

            if (balance < transaction.Amount)
            {
                Log.Warning($"Insufficient funds for account {transaction.FromAccountId}. " +
                    $"Balance: {balance}, Required: {transaction.Amount}");
                throw new InvalidOperationException($"Insufficient funds. Current balance is {balance}");
            }
        }

        if (transaction.ToAccountId != null)
        {
            var balance = await _redisCacheService.GetBalanceAsync(transaction.ToAccountId.Value);

            if (balance == null)
            {
                Log.Information($"Balance for account {transaction.ToAccountId} not found in cache");
            }
        }
        var entity = TransactionEntity.FromDomain(transaction);

        await _transactionRepository.AddAsync(entity);

        var transactionMessage = entity.ToDomain();
        await _kafkaProducer.PublishAsync(_kafkaOptions.TransactionsTopic, transactionMessage);

        return entity.ToDomain();

    }

    /// <summary>
    /// Publishes a transaction notification to Kafka
    /// </summary>
    /// <param name="notificationEvent"></param>
    /// <returns>Task</returns>
    public async Task PublishTransactionNotificationAsync(TransactionNotificationEvent notificationEvent)
    {
        try
        {
            await _kafkaProducer.PublishAsync(_kafkaOptions.NotificationsTopic, notificationEvent);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error publishing transaction notification");
            throw;
        }
    }
}

