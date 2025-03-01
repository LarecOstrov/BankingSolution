using Banking.Domain.ValueObjects;
namespace Banking.Application.Services.Interfaces;

public interface ITransactionService
{
    public Task<Transaction> PublishTransactionAsync(Transaction transaction);
    public Task<Transaction?> GetTransactionByIdAsync(Guid transactionId);
    Task<bool> ProcessTransactionAsync(Guid? fromAccountId, Guid? toAccountId, decimal amount);
    Task<bool> IsAccountOwnerAsync(Guid accountId, Guid userId);
}
