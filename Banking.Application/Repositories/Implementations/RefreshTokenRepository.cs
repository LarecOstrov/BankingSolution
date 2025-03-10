﻿using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Banking.Application.Repositories.Implementations
{
    public class RefreshTokenRepository : BaseRepository<RefreshTokenEntity>, IRefreshTokenRepository
    {

        public RefreshTokenRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        /// <summary>
        /// Get a refresh token by token
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Taks of RefreshTokenEntity</returns>
        public async Task<RefreshTokenEntity?> GetByTokenAsync(string token)
        {
            try
            {
                return await _dbContext.RefreshTokens.Include(t => t.User).ThenInclude(u => u.Role).FirstOrDefaultAsync(t => t.Token == token);
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error when getting a refresh token.");
                throw new Exception("Database error occurred.");
            }
        }

        /// <summary>
        /// Delete a refresh token
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Tasks</returns>
        public async Task DeleteAsync(string token)
        {
            try
            {
                var entity = await GetByTokenAsync(token);
                if (entity != null)
                {
                    _dbContext.RefreshTokens.Remove(entity);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error when deleting a refresh token.");
                throw new Exception("Database error occurred.");
            }
        }
    }
}
