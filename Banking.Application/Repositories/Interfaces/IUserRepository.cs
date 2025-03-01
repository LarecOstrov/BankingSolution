using Banking.Infrastructure.Database.Entities;
namespace Banking.Application.Repositories.Interfaces
{
    public interface IUserRepository : IBaseRepository<UserEntity>
    {
        Task<UserEntity?> GetUserByEmailAsync(string email);
        Task<IEnumerable<UserEntity>> GetUnconfirmedUsersAsync();
        Task<bool> ConfirmUserAsync(Guid userId);
    }
}
