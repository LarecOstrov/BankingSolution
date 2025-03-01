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
    public required decimal BalanceAfterTransaction { get; set; }
    public required DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
