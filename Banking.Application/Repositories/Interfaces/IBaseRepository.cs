using Microsoft.EntityFrameworkCore.Storage;

namespace Banking.Application.Repositories.Interfaces;

public interface IBaseRepository<T> where T : class
{
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task<T?> GetByIdAsync(Guid id);
    IQueryable<T> GetAll();
    Task<int> CountAllAsync();
    Task<T?> AddAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> SaveChangesAsync();
}
