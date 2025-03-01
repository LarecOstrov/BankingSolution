using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Application.Repositories.Implementations
{
    public class RoleRepository : BaseRepository<RoleEntity>, IRoleRepository
    {
        public RoleRepository(ApplicationDbContext dbContext) : base(dbContext) { }
        public async Task<RoleEntity?> GetRoleByNameAsync(string roleName)
        {
            return await _dbContext.Roles
                .Where(r => r.Name == roleName)
                .FirstOrDefaultAsync();
        }
    }
}
