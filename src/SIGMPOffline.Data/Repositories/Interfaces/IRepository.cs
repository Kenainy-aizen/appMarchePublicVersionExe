using System.Linq.Expressions;

namespace SIGMPOffline.Data.Repositories.Interfaces;

/// <summary>
/// Contrat générique d'accès aux données pour une entité.
/// </summary>
/// <typeparam name="TEntity">Type de l'entité.</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>Récupère une entité par sa clé primaire (ou <c>null</c>).</summary>
    Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>Retourne toutes les entités (lecture seule, non suivies).</summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Retourne les entités correspondant au prédicat.</summary>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Retourne la première entité correspondant au prédicat, ou <c>null</c>.</summary>
    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Expose une requête composable (suivie).</summary>
    IQueryable<TEntity> Query();

    /// <summary>Compte les entités (toutes, ou filtrées par un prédicat).</summary>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>Ajoute une entité.</summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Marque une entité comme modifiée.</summary>
    void Update(TEntity entity);

    /// <summary>Marque une entité pour suppression.</summary>
    void Remove(TEntity entity);
}
