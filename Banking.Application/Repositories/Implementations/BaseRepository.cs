using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Banking.Application.Repositories.Implementations
{
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _dbContext;
        private readonly DbSet<T> _dbSet;

        protected BaseRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = dbContext.Set<T>();
        }

        /// <summary>
        /// Begin a transaction
        /// </summary>
        /// <returns>Task of IDbContextTransaction</returns>
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _dbContext.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// Get an entity by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Task of T</returns>
        public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

        /// <summary>
        /// Get all entities
        /// </summary>
        /// <returns>IQueryable of T</returns>
        public IQueryable<T> GetAll() => _dbSet.AsQueryable();

        /// <summary>
        /// Count all entities asynchronously
        /// </summary>
        /// <returns>Task of int</returns>
        public async Task<int> CountAllAsync() => await _dbSet.CountAsync();

        /// <summary>
        /// Add an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Task of T</returns>
        public async Task<T?> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            if (await _dbContext.SaveChangesAsync() > 0)
            {
                return entity;
            }
            return null;
        }

        /// <summary>
        /// Update an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Task of bool</returns>
        public async Task<bool> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _dbContext.SaveChangesAsync();
            return await _dbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Delete an entity
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Taks of bool</returns>
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                return await _dbContext.SaveChangesAsync() > 0;
            }
            return false;
        }

        /// <summary>
        /// Save changes asynchronously
        /// </summary>
        /// <returns>Taks of bool</returns>
        public async Task<bool> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync() > 0;
        }
    }
}
