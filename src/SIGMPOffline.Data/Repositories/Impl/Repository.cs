using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data.Repositories.Interfaces;

namespace SIGMPOffline.Data.Repositories.Impl;

/// <summary>
/// Implémentation générique de <see cref="IRepository{TEntity}"/> sur EF Core.
/// </summary>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    /// <summary>Contexte EF Core partagé.</summary>
    protected readonly AppDbContext Context;

    /// <summary>Jeu d'entités EF Core.</summary>
    protected readonly DbSet<TEntity> Set;

    public Repository(AppDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Set = context.Set<TEntity>();
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        => await Set.FindAsync([id], cancellationToken);

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Set.AsNoTracking().ToListAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await Set.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await Set.FirstOrDefaultAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public virtual IQueryable<TEntity> Query() => Set;

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => predicate is null
            ? await Set.CountAsync(cancellationToken)
            : await Set.CountAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entity, cancellationToken);

    /// <inheritdoc />
    public virtual void Update(TEntity entity) => Set.Update(entity);

    /// <inheritdoc />
    public virtual void Remove(TEntity entity) => Set.Remove(entity);
}
