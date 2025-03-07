using Banking.Domain.ValueObjects;
namespace Banking.Application.Services.Interfaces;

public interface IPublishService
{
    Task<Transaction> PublishTransactionAsync(Transaction transaction);
    Task PublishTransactionNotificationAsync(TransactionNotificationEvent notificationEvent);
}
