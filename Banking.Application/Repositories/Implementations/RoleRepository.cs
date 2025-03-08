using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Banking.Application.Repositories.Implementations
{
    public class RoleRepository : BaseRepository<RoleEntity>, IRoleRepository
    {
        public RoleRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        /// <summary>
        /// Get a role by name
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns>Task of RoleEntity</returns>
        public async Task<RoleEntity?> GetRoleByNameAsync(string roleName)
        {
            try
            {
                return await _dbContext.Roles
                    .Where(r => r.Name == roleName)
                    .FirstOrDefaultAsync();
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error when getting role by name.");
                throw new Exception("Database error occurred.");
            }
        }
    }
}
