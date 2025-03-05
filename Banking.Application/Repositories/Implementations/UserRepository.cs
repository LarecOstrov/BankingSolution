using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Application.Repositories.Implementations;

public class UserRepository : BaseRepository<UserEntity>, IUserRepository
{
    public UserRepository(ApplicationDbContext dbContext) : base(dbContext) { }

    /// <summary>
    /// Get a user by email
    /// </summary>
    /// <param name="email"></param>
    /// <returns>UserEntity</returns>
    public async Task<UserEntity?> GetUserByEmailAsync(string email)
    {
        return await _dbContext.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Get a user by Id
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>UserEntity</returns>
    public async Task<UserEntity?> GetUserByIdAsync(Guid userId)
    {
        return await _dbContext.Users.FindAsync(userId);
    }

    /// <summary>
    /// Get all unconfirmed users
    /// </summary>
    /// <returns>IEnumerable of UserEntity</returns>
    public async Task<IEnumerable<UserEntity>> GetUnconfirmedUsersAsync() =>
            await _dbContext.Users.Where(u => !u.Confirmed).ToListAsync();
    /// <summary>
    /// Confirm a user
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>bool</returns>
    public async Task<bool> ConfirmUserAsync(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.Confirmed = true;
            return await _dbContext.SaveChangesAsync() > 0;
        }
        return false;
    }
    /// <summary>
    /// Assign a role to a user
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="roleId"></param>
    /// <returns>bool</returns>
    public async Task<bool> AssignRoleAsync(Guid userId, Guid roleId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return false;

        var roleExists = await _dbContext.Roles.AnyAsync(r => r.Id == roleId);
        if (!roleExists)
            return false;

        user.RoleId = roleId;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Get a user with role by Id
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>Taks of UserEntity</returns>
    public async Task<UserEntity?> GetUserWithRoileById(Guid userId)
    {        
        return await _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}