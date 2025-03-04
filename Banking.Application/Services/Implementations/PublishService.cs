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
    private readonly SolutionOptions _solutionOptions;

    public PublishService(
        IKafkaProducer kafkaProducer,
        ITransactionRepository transactionRepository,
        IRedisCacheService redisCacheService,
        IAccountService accountService,
        IOptions<SolutionOptions> solutionOptions)
    {
        _kafkaProducer = kafkaProducer;
        _transactionRepository = transactionRepository;
        _redisCacheService = redisCacheService;
        _solutionOptions = solutionOptions.Value;
    }

    public async Task<Transaction> PublishTransactionAsync(Transaction transaction)
    {
        try
        {
            if (transaction.FromAccountId == transaction.ToAccountId)
            {
                Log.Warning("Transaction from and to accounts are same");
                throw new Exception("Transaction from and to accounts are same");
            }
            if (transaction.FromAccountId == null && transaction.ToAccountId == null)
            {
                Log.Warning("Transaction must have at least one account");
                throw new Exception("Transaction must have at least one account");
            }

            if (transaction.Amount <= 0)
            {
                Log.Warning($"Invalid transaction amount: {transaction.Amount}");
                throw new Exception("Invalid transaction amount");
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
                    throw new Exception($"Insufficient funds. Current balance is {balance}");
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

            await _kafkaProducer.PublishAsync(_solutionOptions.Kafka.TransactionsTopic, entity);

            return entity.ToDomain();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error publishing transaction");
            throw;
        }
    }
}

