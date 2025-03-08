using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banking.Infrastructure.Database.Entities
{
    [Table("RefreshTokens")]
    public class RefreshTokenEntity
    {
        [Key]
        public Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public UserEntity User { get; set; } = null!;
        public required string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public required DateTime ExpiryDate { get; set; }
    }
}
