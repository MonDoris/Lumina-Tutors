using System.Linq.Expressions;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Domain.Interfaces.Repositories;

/// <summary>
/// Generic repository interface — covers standard CRUD + query operations.
/// Concrete implementations live in Infrastructure layer.
/// </summary>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    // ── Queries ───────────────────────────────────────────────────────────────

    Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<TEntity?> GetByIdAsync(int id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> include,
        CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include,
        CancellationToken ct = default);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> include,
        CancellationToken ct = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    /// <summary>Paginated query with optional filtering and sorting.</summary>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken ct = default);

    // ── Commands ──────────────────────────────────────────────────────────────

    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    void Update(TEntity entity);
    void UpdateRange(IEnumerable<TEntity> entities);
    void Remove(TEntity entity);
    void RemoveRange(IEnumerable<TEntity> entities);

    // ── Raw / Advanced ────────────────────────────────────────────────────────

    IQueryable<TEntity> AsQueryable();
}

/// <summary>
/// Tenant-scoped repository: automatically applies SchoolId filter on all queries.
/// All business repositories should implement this variant.
/// </summary>
public interface ITenantRepository<TEntity> : IRepository<TEntity>
    where TEntity : TenantEntity
{
    Task<IReadOnlyList<TEntity>> GetBySchoolAsync(int schoolId, CancellationToken ct = default);
}
