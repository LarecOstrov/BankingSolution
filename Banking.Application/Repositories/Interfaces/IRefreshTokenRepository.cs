using Banking.Infrastructure.Database.Entities;

namespace Banking.Application.Repositories.Interfaces
{
    public interface IRefreshTokenRepository : IBaseRepository<RefreshTokenEntity>
    {
        Task<RefreshTokenEntity?> GetByTokenAsync(string token);
        Task DeleteAsync(string token);
    }
}
