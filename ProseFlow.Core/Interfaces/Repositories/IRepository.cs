using System.Linq.Expressions;

namespace ProseFlow.Core.Interfaces.Repositories;

/// <summary>
/// A generic repository interface for basic CRUD operations.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    Task<List<TEntity>> GetAllAsync();
    Task<TEntity?> GetByIdAsync(int id, bool asNoTracking = false);
    Task<List<TEntity>> GetByExpressionAsync(Expression<Func<TEntity, bool>> expression);
    Task AddAsync(TEntity entity);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}