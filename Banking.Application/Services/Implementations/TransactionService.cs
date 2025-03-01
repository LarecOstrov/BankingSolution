using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Caching;
using Banking.Infrastructure.Database.Entities;
using Banking.Infrastructure.Messaging.Kafka;
using Serilog;

namespace Banking.Application.Services.Implementations;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IRedisCacheService _redisCacheService;
    private readonly IAccountRepository _accountRepository;

    public TransactionService(
        IKafkaProducer kafkaProducer,
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        IRedisCacheService redisCacheService)
    {
        _kafkaProducer = kafkaProducer;
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _redisCacheService = redisCacheService;
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
                    Log.Warning($"Balance for account {transaction.FromAccountId} not found in cache");
                    throw new Exception("Balance information not available");
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
                    Log.Warning($"Balance for account {transaction.ToAccountId} not found in cache");
                    throw new Exception("Balance information not available");
                }
            }

            var entity = TransactionEntity.FromDomain(transaction);
            await _kafkaProducer.PublishAsync("transactions", entity);

            return entity.ToDomain();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error publishing transaction to Kafka");
            throw;
        }
    }

    public async Task<Transaction?> GetTransactionByIdAsync(Guid transactionId)
    {
        var entity = await _transactionRepository.GetByIdAsync(transactionId);
        return entity?.ToDomain();
    }

    public async Task<bool> IsAccountOwnerAsync(Guid accountId, Guid userId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        return account != null && account.UserId == userId;
    }

    public async Task<bool> ProcessTransactionAsync(Guid? fromAccountId, Guid? toAccountId, decimal amount)
    {
        using (var transaction = await _transactionRepository.BeginTransactionAsync())
        {
            AccountEntity? fromAccount = null;
            if (fromAccountId != null)
            {
                fromAccount = await _accountRepository.GetByIdForUpdateWithLockAsync(fromAccountId.Value);
            }
            AccountEntity? toAccount = null;
            if (toAccountId != null)
            {
                toAccount = await _accountRepository.GetByIdForUpdateWithLockAsync(toAccountId.Value);
            }

            if (fromAccount == null && toAccount == null)
            {
                Log.Error("Required ");
                throw new Exception("One of the accounts not found");
            }

            if (fromAccount != null && fromAccount.Balance < amount)
            {
                Log.Warning("Insufficient funds for transaction");
                await transaction.RollbackAsync();
                return false;
            }

            if (fromAccount != null)
            {
                fromAccount.Balance -= amount;
            }
            if (toAccount != null)
            {
                toAccount.Balance += amount;
            }
            await _transactionRepository.SaveChangesAsync();

            await transaction.CommitAsync();
            return true;
        }
    }
}
