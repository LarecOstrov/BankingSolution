using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;

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
            try
            {
                return await _dbContext.Database.BeginTransactionAsync();
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error occurred when beginning a transaction.");
                throw new Exception("Database error occurred.");
            }
        }

        /// <summary>
        /// Get an entity by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Task of T</returns>
        public async Task<T?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _dbSet.FindAsync(id);
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error occurred when getting an entity by Id.");
                throw new Exception("Database error occurred.");
            }
        }

        /// <summary>
        /// Get all entities
        /// </summary>
        /// <returns>IQueryable of T</returns>
        public IQueryable<T> GetAll()
        {
            try
            {
                return _dbSet.AsQueryable();
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error occurred when getting all entities.");
                throw new Exception("Database error occurred.");
            }
        }

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
            try
            {
                await _dbSet.AddAsync(entity);
                if (await _dbContext.SaveChangesAsync() > 0)
                {
                    return entity;
                }
                return null;
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error occurred when adding an entity.");
                throw new Exception("Database error occurred.");
            }
        }

        /// <summary>
        /// Update an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Task of bool</returns>
        public async Task<bool> UpdateAsync(T entity)
        {
            try
            {
                _dbSet.Update(entity);
                return await _dbContext.SaveChangesAsync() > 0;
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error occurred when updating an entity.");
                throw new Exception("Database error occurred.");
            }
        }

        /// <summary>
        /// Delete an entity
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Taks of bool</returns>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var entity = await GetByIdAsync(id);
                if (entity != null)
                {
                    _dbSet.Remove(entity);
                    return await _dbContext.SaveChangesAsync() > 0;
                }
                return false;
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error occurred when deleting an entity.");
                throw new Exception("Database error occurred.");
            }
        }

        /// <summary>
        /// Save changes asynchronously
        /// </summary>
        /// <returns>Taks of bool</returns>
        public async Task<bool> SaveChangesAsync()
        {
            try
            {
                return await _dbContext.SaveChangesAsync() > 0;
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error occurred when saving changes.");
                throw new Exception("Database error occurred.");
            }
        }
    }
}
