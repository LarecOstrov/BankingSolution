using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banking.Infrastructure.Database.Entities
{
    [Table("FailedTransactions")]
    public class FailedTransactionEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? TransactionMessage { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
