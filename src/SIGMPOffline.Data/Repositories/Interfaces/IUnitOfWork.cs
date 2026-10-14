using SIGMPOffline.Data.Repositories.Interfaces;

namespace SIGMPOffline.Data.Repositories.Interfaces;

/// <summary>
/// Unité de travail : point d'accès aux repositories et gestion de transaction.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>Repository des PPM.</summary>
    IPpmRepository Ppm { get; }

    /// <summary>Repository des dossiers DAO.</summary>
    IDossierDaoRepository Dossiers { get; }

    /// <summary>Repository des marchés.</summary>
    IMarcheRepository Marches { get; }

    /// <summary>Persiste les changements en attente.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Démarre une transaction explicite.</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Valide la transaction courante.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Annule la transaction courante.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
