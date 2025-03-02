using Banking.Domain.ValueObjects;
using Confluent.Kafka;
namespace Banking.Application.Services.Interfaces;

public interface ITransactionService
{
    public Task<Transaction?> GetTransactionByIdAsync(Guid transactionId);
    Task<bool> ProcessTransactionAsync(ConsumeResult<Null, string> consumeResult);
}
