using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Repositories.Interfaces;

/// <summary>Accès aux dossiers d'appel d'offres (DAO).</summary>
public interface IDossierDaoRepository : IRepository<DossierDao>
{
    /// <summary>Charge un dossier avec ses candidats.</summary>
    Task<DossierDao?> GetWithCandidatsAsync(string refDossier, CancellationToken cancellationToken = default);

    /// <summary>Charge un dossier complet (ligne PPM, acheteurs, candidats, marché).</summary>
    Task<DossierDao?> GetFullAsync(string refDossier, CancellationToken cancellationToken = default);
}
