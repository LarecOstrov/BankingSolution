using Banking.Domain.Enums;
namespace Banking.Domain.ValueObjects;

public record Transaction(
    Guid Id,
    Guid? FromAccountId,
    Guid? ToAccountId,
    decimal Amount,
    DateTime CreatedAt,
    TransactionStatus Status = TransactionStatus.Pending
);
