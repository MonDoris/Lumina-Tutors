using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Interfaces.Repositories;
using LuminaTutors.Infrastructure.Data;

namespace LuminaTutors.Infrastructure.Repositories;

/// <summary>
/// Generic EF Core repository — implements all standard CRUD operations.
/// Used as the base for all concrete repositories.
/// </summary>
public class GenericRepository<TEntity> : IRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly LuminaTutorsDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(LuminaTutorsDbContext context)
    {
        _context = context;
        _dbSet   = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _dbSet.FindAsync([id], ct);

    public virtual async Task<TEntity?> GetByIdAsync(
        int id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> include,
        CancellationToken ct = default)
        => await include(_dbSet).FirstOrDefaultAsync(x => x.Id == id, ct);

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.AsNoTracking().ToListAsync(ct);

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include,
        CancellationToken ct = default)
    {
        IQueryable<TEntity> query = _dbSet.AsNoTracking();
        if (include is not null) query = include(query);
        return await query.Where(predicate).ToListAsync(ct);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public virtual async Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, ct);

    public virtual async Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> include,
        CancellationToken ct = default)
        => await include(_dbSet).FirstOrDefaultAsync(predicate, ct);

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.AnyAsync(predicate, ct);

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
        => predicate is null
            ? await _dbSet.CountAsync(ct)
            : await _dbSet.CountAsync(predicate, ct);

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken ct = default)
    {
        IQueryable<TEntity> query = _dbSet.AsNoTracking();

        if (include is not null) query = include(query);
        if (filter is not null)  query = query.Where(filter);

        var totalCount = await query.CountAsync(ct);

        if (orderBy is not null) query = orderBy(query);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PagedResult<TEntity>.Create(items, totalCount, pageNumber, pageSize);
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default)
        => await _dbSet.AddAsync(entity, ct);

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
        => await _dbSet.AddRangeAsync(entities, ct);

    public virtual void Update(TEntity entity)
        => _dbSet.Update(entity);

    public virtual void UpdateRange(IEnumerable<TEntity> entities)
        => _dbSet.UpdateRange(entities);

    public virtual void Remove(TEntity entity)
        => _dbSet.Remove(entity);

    public virtual void RemoveRange(IEnumerable<TEntity> entities)
        => _dbSet.RemoveRange(entities);

    public virtual IQueryable<TEntity> AsQueryable()
        => _dbSet.AsQueryable();
}
