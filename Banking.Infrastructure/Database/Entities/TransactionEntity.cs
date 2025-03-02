using Banking.Domain.Enums;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Database.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banking.Domain.Entities;

[Table("Transactions")]
public class TransactionEntity
{
    [Key]
    public Guid Id { get; set; }
    public Guid? FromAccountId { get; set; }
    public AccountEntity? FromAccount { get; set; } = null!;
    public Guid? ToAccountId { get; set; }
    public AccountEntity? ToAccount { get; set; } = null!;
    public required decimal Amount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public required TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string? FailureReason { get; set; } = null;
    public static TransactionEntity FromDomain(Transaction transaction) =>
        new TransactionEntity
        {
            Id = Guid.NewGuid(),
            FromAccountId = transaction.FromAccountId,
            ToAccountId = transaction.ToAccountId,
            Amount = transaction.Amount,
            Status = TransactionStatus.Pending
        };

    public Transaction ToDomain() =>
        new Transaction(Id, FromAccountId, ToAccountId, Amount, CreatedAt, Status);
}
