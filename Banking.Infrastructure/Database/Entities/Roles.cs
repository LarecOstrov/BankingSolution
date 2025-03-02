using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banking.Infrastructure.Database.Entities
{
    [Table("Roles")]
    public class RoleEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [MaxLength(50)]
        public required string Name { get; set; } = string.Empty;
        public ICollection<UserEntity> Users { get; set; } = new List<UserEntity>();
    }
}
