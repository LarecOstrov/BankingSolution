using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Application.Repositories.Implementations;

public class UserRepository : BaseRepository<UserEntity>, IUserRepository
{
    public UserRepository(ApplicationDbContext dbContext) : base(dbContext) { }
    public async Task<UserEntity?> GetUserByEmailAsync(string email)
    {
        return await _dbContext.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }
    public async Task<UserEntity?> GetUserByIdAsync(Guid userId)
    {
        return await _dbContext.Users.FindAsync(userId);
    }

    public async Task<IEnumerable<UserEntity>> GetUnconfirmedUsersAsync() =>
            await _dbContext.Users.Where(u => !u.Confirmed).ToListAsync();

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
}