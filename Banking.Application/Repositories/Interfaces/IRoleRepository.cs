using Banking.Infrastructure.Database.Entities;

namespace Banking.Application.Repositories.Interfaces;

public interface IRoleRepository : IBaseRepository<RoleEntity>
{
    Task<RoleEntity?> GetRoleByNameAsync(string roleName);
}
