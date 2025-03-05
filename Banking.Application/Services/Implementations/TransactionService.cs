using Banking.Application.Repositories.Implementations;
using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Caching;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Database.Entities;
using Banking.Infrastructure.WebSockets;
using Confluent.Kafka;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;

namespace Banking.Application.Services.Implementations;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFailedTransactionRepository _failedTransactionRepository;
    private readonly IBalanceHistoryRepository _balanceHistoryRepository;
    private readonly IRedisCacheService _redisCacheService;
    private readonly IAccountRepository _accountRepository;
    private readonly IPublishService _publishService;
    private readonly SolutionOptions _solutionOptions;
    private readonly int _maxRetries;
    private readonly int _delayMilliseconds;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IFailedTransactionRepository failedTransactionRepository,
        IBalanceHistoryRepository balanceHistoryRepository,
        IAccountRepository accountRepository,
        IRedisCacheService redisCacheService,
        IPublishService publishService,
        IOptions<SolutionOptions> solutionOptions)
    {
        _transactionRepository = transactionRepository;
        _failedTransactionRepository = failedTransactionRepository;
        _balanceHistoryRepository = balanceHistoryRepository;
        _accountRepository = accountRepository;
        _redisCacheService = redisCacheService;
        _publishService = publishService;
        _solutionOptions = solutionOptions.Value;
        _maxRetries = _solutionOptions.TransactionRetryPolicy.MaxRetries;
        _delayMilliseconds = _solutionOptions.TransactionRetryPolicy.DelayMilliseconds;

    }
    
    /// <summary>
    /// Process the transaction from Queue
    /// </summary>
    /// <param name="consumeResult"></param>
    /// <returns>Bollean indicating if the transaction was successful</returns>
    public async Task<bool> ProcessTransactionAsync(ConsumeResult<Null, string> consumeResult)
    {
        try
        {
            var transactionObject = JsonSerializer.Deserialize<Transaction>(consumeResult.Message.Value);
            if (transactionObject == null)
            {
                Log.Error("Received null transaction from Kafka");
                await SaveFailedTransaction(consumeResult.Message.Value, "Received null transaction from Kafka");
                return false;
            }

            int retryCount = 0;

            while (retryCount < _maxRetries)
            {
                try
                {
                    using (var transaction = await _transactionRepository.BeginTransactionAsync())
                    {
                        try
                        {
                            var transactionEntity = await _transactionRepository.GetByIdAsync(transactionObject.Id);
                            if (transactionEntity == null)
                            {
                                Log.Error($"Transaction {transactionObject.Id} not found.");
                                await transaction.RollbackAsync();
                                await SaveFailedTransaction(consumeResult.Message.Value, "Transaction not found");
                                return false;
                            }

                            if (transactionEntity.Status != TransactionStatus.Pending)
                            {
                                Log.Warning($"Transaction {transactionObject.Id} already processed.");
                                await transaction.RollbackAsync();
                                await SaveFailedTransaction(consumeResult.Message.Value, "Transaction already processed");
                                return false;
                            }

                            var success = await ProcessTransactionInternal(transactionObject, transactionEntity);
                            if (!success)
                            {
                                await transaction.RollbackAsync();
                                transactionEntity.Status = TransactionStatus.Failed;                                
                                await _transactionRepository.SaveChangesAsync();

                                await SaveFailedTransaction(consumeResult.Message.Value,
                                    transactionEntity.FailureReason ?? "Unknown reason");
                                return true;
                            }

                            transactionEntity.Status = TransactionStatus.Completed;
                            await _transactionRepository.SaveChangesAsync();
                            await transaction.CommitAsync();
                            return true;
                        }
                        catch (Exception ex) when (IsTransientError(ex))
                        {
                            Log.Warning($"Transaction {transactionObject.Id} failed due to locking. Retrying {retryCount + 1}/{_maxRetries}...");
                            await transaction.RollbackAsync();
                            retryCount++;
                            await Task.Delay(_delayMilliseconds);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error processing transaction");
                    return false;
                }
            }

            Log.Error($"Transaction {transactionObject.Id} failed after {_maxRetries} attempts.");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing transaction");
            return false;
        }
    }
    /// <summary>
    /// Save the failed transaction
    /// </summary>
    /// <param name="transactionMessage"></param>
    /// <param name="reason"></param>
    /// <returns>Task</returns>
    private async Task SaveFailedTransaction(string? transactionMessage, string reason)
    {
        try
        {
            var failedTransaction = new FailedTransactionEntity
            {
                TransactionMessage = transactionMessage,
                Reason = reason,
            };
            await _failedTransactionRepository.AddAsync(failedTransaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving failed transaction");
        }
    }

    /// <summary>
    /// Check if the exception is a transient error that can be retried
    /// </summary>
    private bool IsTransientError(Exception ex)
    {
        return ex is DbUpdateConcurrencyException ||
               ex.InnerException is SqlException sqlEx &&
               (sqlEx.Number == 1205 || // Deadlock
                sqlEx.Number == 1222);  // Lock request timeout
    }


    /// <summary>
    /// Process the transaction and update the accounts
    /// </summary>
    /// <param name="transactionObject"></param>
    /// <param name="transactionEntity"></param>
    /// <returns>Bollean indicating if the transaction was successful</returns>
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
            await _redisCacheService.UpdateBalanceAsync(fromAccount.Id, fromAccount.Balance);
            await SaveBalanceHistory(fromAccount, transactionObject.Id);
        }
        if (toAccount != null)
        {
            await _redisCacheService.UpdateBalanceAsync(toAccount.Id, toAccount.Balance);
            await SaveBalanceHistory(toAccount, transactionObject.Id);
        }

        await _publishService.PublishTransactionNotificationAsync(FromTransactionData(fromAccount, toAccount, transactionObject.Amount));

        return true;
    }


    #region Private Methods
    /// <summary>
    /// Save the balance history for the account
    /// </summary>
    /// <param name="account"></param>
    /// <param name="transactionId"></param>
    /// <returns>Task</returns>
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

    /// <summary>
    /// Create a TransactionNotificationEvent from the transaction data
    /// </summary>
    /// <param name="fromAccount"></param>
    /// <param name="toAccount"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    private static TransactionNotificationEvent FromTransactionData(AccountEntity? fromAccount, AccountEntity? toAccount, decimal amount)
    {
        return new TransactionNotificationEvent
        {
            FromUserId = fromAccount?.UserId,
            ToUserId = toAccount?.UserId,
            Amount = amount,
            FromAccountNumber = fromAccount?.AccountNumber,
            ToAccountNumber = toAccount?.AccountNumber,
            FromUserName = fromAccount?.User?.FullName,
            ToUserName = toAccount?.User?.FullName,
            FromAccountBalance = fromAccount?.Balance,
            ToAccountBalance = toAccount?.Balance,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Notify the users about the transaction
    /// </summary>
    /// <param name="fromAccount"></param>
    /// <param name="toAccount"></param>
    /// <param name="amount"></param>
    /// <returns>Task</returns>

    #endregion

    
}

