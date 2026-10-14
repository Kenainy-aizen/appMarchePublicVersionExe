using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Services.Abstractions;

/// <summary>Module DAO &amp; Attribution : dossiers, acheteurs et candidats.</summary>
public interface IDossierDaoService
{
    /// <summary>Crée un dossier d'appel d'offres rattaché à une ligne PPM.</summary>
    Task<DossierDao> CreerAsync(DossierDao dossier, CancellationToken cancellationToken = default);

    /// <summary>Liste tous les dossiers.</summary>
    Task<IReadOnlyList<DossierDao>> ListerAsync(CancellationToken cancellationToken = default);

    /// <summary>Récupère un dossier complet (lève si absent).</summary>
    Task<DossierDao> ObtenirAsync(string refDossier, CancellationToken cancellationToken = default);

    /// <summary>Enregistre un acheteur de DAO.</summary>
    Task<AcheteurDao> AjouterAcheteurAsync(string refDossier, AcheteurDao acheteur, CancellationToken cancellationToken = default);

    /// <summary>Enregistre un candidat / soumissionnaire.</summary>
    Task<Candidat> AjouterCandidatAsync(string refDossier, Candidat candidat, CancellationToken cancellationToken = default);

    /// <summary>Met à jour un dossier existant.</summary>
    Task ModifierAsync(DossierDao dossier, CancellationToken cancellationToken = default);

    /// <summary>Supprime un dossier (et, en cascade, ses candidats / acheteurs).</summary>
    Task SupprimerAsync(string refDossier, CancellationToken cancellationToken = default);

    /// <summary>Met à jour un candidat existant.</summary>
    Task ModifierCandidatAsync(string refDossier, Candidat candidat, CancellationToken cancellationToken = default);

    /// <summary>Supprime un candidat.</summary>
    Task SupprimerCandidatAsync(string refDossier, int idCandidat, CancellationToken cancellationToken = default);

    /// <summary>Met à jour un acheteur existant.</summary>
    Task ModifierAcheteurAsync(string refDossier, AcheteurDao acheteur, CancellationToken cancellationToken = default);

    /// <summary>Supprime un acheteur.</summary>
    Task SupprimerAcheteurAsync(string refDossier, int idAcheteur, CancellationToken cancellationToken = default);

    /// <summary>Désigne le candidat retenu (et démarque les autres).</summary>
    Task<Candidat> DesignerRetenuAsync(string refDossier, int idCandidat, CancellationToken cancellationToken = default);
}
