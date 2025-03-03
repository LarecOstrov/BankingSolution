﻿using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Application.Repositories.Implementations
{
    public class RefreshTokenRepository : BaseRepository<RefreshTokenEntity>, IRefreshTokenRepository
    {

        public RefreshTokenRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async Task<RefreshTokenEntity?> GetByTokenAsync(string token) =>
            await _dbContext.RefreshTokens.Include(t => t.User).ThenInclude(u => u.Role).FirstOrDefaultAsync(t => t.Token == token);

        public async Task DeleteAsync(string token)
        {
            var entity = await GetByTokenAsync(token);
            if (entity != null)
            {
                _dbContext.RefreshTokens.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
