using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banking.Infrastructure.Database.Entities;

[Table("Users")]
public class UserEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public required string FullName { get; set; } = string.Empty;
    [MaxLength(255)]
    [EmailAddress]
    public required string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<AccountEntity> Accounts { get; set; } = new List<AccountEntity>();
    public required string PasswordHash { get; set; }
    public bool Confirmed { get; set; } = false;
    public Guid RoleId { get; set; }
    public RoleEntity Role { get; set; } = null!;
}
