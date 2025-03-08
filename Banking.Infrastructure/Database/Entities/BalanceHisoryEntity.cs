using Banking.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banking.Infrastructure.Database.Entities;

[Table("BalanceHistory")]
public class BalanceHistoryEntity
{
    [Key]
    public Guid Id { get; set; }
    public required Guid AccountId { get; set; }
    public AccountEntity Account { get; set; } = null!;
    public required Guid TransactionId { get; set; }
    public TransactionEntity Transaction { get; set; } = null!;
    public required decimal NewBalance { get; set; }
    public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
