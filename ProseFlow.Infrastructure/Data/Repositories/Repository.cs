using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProseFlow.Core.Abstracts;
using ProseFlow.Core.Interfaces.Repositories;

namespace ProseFlow.Infrastructure.Data.Repositories;

/// <summary>
/// A generic base repository providing common data access functionality using EF Core.
/// </summary>
/// <typeparam name="TEntity">The type of the entity this repository handles.</typeparam>
public class Repository<TEntity>(AppDbContext context) : IRepository<TEntity> where TEntity : EntityBase
{
    protected readonly AppDbContext Context = context;

    /// <inheritdoc />
    public async Task<List<TEntity>> GetAllAsync()
    {
        return await Context.Set<TEntity>().ToListAsync();
    }
    
    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(int id, bool asNoTracking = false)
    {
        return await Context.Set<TEntity>().FindAsync(id);
    }
    
    /// <inheritdoc />
    public async Task<List<TEntity>> GetByExpressionAsync(Expression<Func<TEntity, bool>> expression)
    {
        return await Context.Set<TEntity>().Where(expression).ToListAsync();
    }

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity)
    {
        await Context.Set<TEntity>().AddAsync(entity);
    }

    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        Context.Set<TEntity>().Update(entity);
    }

    /// <inheritdoc />
    public void Delete(TEntity entity)
    {
        Context.Set<TEntity>().Remove(entity);
    }
}