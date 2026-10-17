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

    /// <summary>Repository des provinces.</summary>
    IProvinceRepository Provinces { get; }

    /// <summary>Repository de la configuration globale de l'application.</summary>
    IConfigurationAppRepository Configuration { get; }

    /// <summary>
    /// Détache toutes les entités suivies. Utilisé lors d'un changement de province
    /// pour repartir d'un suivi vierge et éviter toute fuite de données entre provinces.
    /// </summary>
    void DetacherTout();

    /// <summary>Persiste les changements en attente.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Démarre une transaction explicite.</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Valide la transaction courante.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Annule la transaction courante.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
