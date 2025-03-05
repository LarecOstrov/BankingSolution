using Banking.Domain.ValueObjects;
using Confluent.Kafka;
namespace Banking.Application.Services.Interfaces;

public interface ITransactionService
{    
    Task<bool> ProcessTransactionAsync(ConsumeResult<Null, string> consumeResult);
}
