using Banking.Domain.ValueObjects;
using Confluent.Kafka;
namespace Banking.Application.Services.Interfaces;

public interface IPublishService
{
    public Task<Transaction> PublishTransactionAsync(Transaction transaction);
}
