using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Caching;
using Banking.Infrastructure.Database.Entities;
using Banking.Infrastructure.Messaging.Kafka;
using Banking.Infrastructure.WebSockets;
using Confluent.Kafka;
using Serilog;
using System.Linq.Expressions;
using System.Text.Json;

namespace Banking.Application.Services.Implementations;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly IRedisCacheService _redisCacheService;
    private readonly IAccountRepository _accountRepository;
    private readonly WebSocketService _webSocketService;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        IAccountRepository accountRepository,
        IRedisCacheService redisCacheService,
        WebSocketService webSocketService)
    {
        _transactionRepository = transactionRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _accountRepository = accountRepository;
        _redisCacheService = redisCacheService;
        _webSocketService = webSocketService;

    }  

    public async Task<Transaction?> GetTransactionByIdAsync(Guid transactionId)
    {
        var entity = await _transactionRepository.GetByIdAsync(transactionId);
        return entity?.ToDomain();
    }

    

    public async Task<bool> ProcessTransactionAsync(ConsumeResult<Null, string> consumeResult)
    {
        try
        {
            var transactionObject = JsonSerializer.Deserialize<Transaction>(consumeResult.Message.Value);
            if (transactionObject == null)
            {
                Log.Error("Received null transaction from Kafka");
                return false;
            }

            using (var transaction = await _transactionRepository.BeginTransactionAsync())
            {
                try
                {
                    var transactionEntity = await _transactionRepository.GetByIdAsync(transactionObject.Id);
                    if (transactionEntity == null)
                    {
                        Log.Error($"Transaction {transactionObject.Id} not found.");
                        await transaction.RollbackAsync();
                        return false;
                    }

                    if (transactionEntity.Status != TransactionStatus.Pending)
                    {
                        Log.Warning($"Transaction {transactionObject.Id} already processed.");
                        return false;
                    }

                    var success = await ProcessTransactionInternal(transactionObject, transactionEntity);
                    if (!success)
                    {
                        await transaction.RollbackAsync();

                        transactionEntity.Status = TransactionStatus.Failed;
                        await _transactionRepository.SaveChangesAsync();
                        return true;
                    }

                    transactionEntity.Status = TransactionStatus.Completed;
                    await _transactionRepository.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing transaction");
                    await transaction.RollbackAsync();
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing transaction");
            return false;
        }
    }


    private async Task<bool> ProcessTransactionInternal(Transaction transactionObject, 
        TransactionEntity transactionEntity)
    {
        AccountEntity? fromAccount = null, toAccount = null;

        if (transactionObject.FromAccountId != null)
        {
            fromAccount = await _accountRepository.GetByIdForUpdateWithLockAsync(transactionObject.FromAccountId.Value);
        }

        if (transactionObject.ToAccountId != null)
        {
            toAccount = await _accountRepository.GetByIdForUpdateWithLockAsync(transactionObject.ToAccountId.Value);
        }

        if (fromAccount == null && toAccount == null)
        {
            Log.Error("Required minimum one account for transaction");
            transactionEntity.FailureReason = "No accounts found for transaction";
            return false;
        }

        if (fromAccount != null && fromAccount.Balance < transactionObject.Amount)
        {
            Log.Warning("Insufficient funds for transaction");
            transactionEntity.FailureReason = "Insufficient funds";
            return false;
        }

        if (fromAccount != null)
        {
            fromAccount.Balance -= transactionObject.Amount;
        }
        if (toAccount != null)
        {
            toAccount.Balance += transactionObject.Amount;
        }

        await _transactionRepository.SaveChangesAsync();

        
        if (fromAccount != null)
        {
            await _redisCacheService.UpdateBalanceAsync(fromAccount.Id, -transactionObject.Amount);
            await SaveBalanceHistory(fromAccount, transactionObject.Id);
        }
        if (toAccount != null)
        {
            await _redisCacheService.UpdateBalanceAsync(toAccount.Id, transactionObject.Amount);
            await SaveBalanceHistory(toAccount, transactionObject.Id);
        }        
        
        await NotifyUsers(fromAccount, toAccount, transactionObject.Amount);

        return true;
    }



    private async Task SaveBalanceHistory(AccountEntity? account, Guid transactionId)
    {
        if (account == null) return;

        var history = new BalanceHistoryEntity
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            TransactionId = transactionId,
            NewBalance = account.Balance,
            CreatedAt = DateTime.UtcNow
        };
        await _balanceHistoryRepository.AddAsync(history);
    }

    private async Task NotifyUsers(AccountEntity? fromAccount, AccountEntity? toAccount, decimal amount)
    {
        if (fromAccount != null)
        {
            if (toAccount != null)
            {
                await _webSocketService.SendTransactionNotificationAsync(
                    fromAccount.User.Id, $"You sent {amount} to account {toAccount?.User.FullName}." +
                    $" Current balance is {fromAccount?.Balance}.");
            }
            else
            {
                await _webSocketService.SendTransactionNotificationAsync(
                    fromAccount.User.Id, $"Withdrawal of {amount} from account {fromAccount?.AccountNumber}." +
                    $" Current balance is {fromAccount?.Balance}.");
            }
        }

        if (toAccount != null)
        {
            if (fromAccount != null)
            {
                await _webSocketService.SendTransactionNotificationAsync(
                toAccount.User.Id, $"You received {amount} from account {fromAccount?.User.FullName}." +
                $" Current balance is {toAccount?.Balance}.");
            }
            else
            {
                await _webSocketService.SendTransactionNotificationAsync(
                toAccount.User.Id, $"Your account {toAccount?.AccountNumber} funds received {amount}." +
                $" Current balance is {toAccount?.Balance}!");
            }
        }
    }
}

