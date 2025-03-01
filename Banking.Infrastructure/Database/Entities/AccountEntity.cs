using Banking.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banking.Infrastructure.Database.Entities;

[Table("Accounts")]
public class AccountEntity
{
    [Key]
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public UserEntity User { get; set; } = null!;
    [MaxLength(20)]
    public required string AccountNumber { get; set; } = string.Empty;
    public required decimal Balance { get; set; } = 0m;
    public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<TransactionEntity> TransactionsFrom { get; set; } = new List<TransactionEntity>();
    public ICollection<TransactionEntity> TransactionsTo { get; set; } = new List<TransactionEntity>();
}
