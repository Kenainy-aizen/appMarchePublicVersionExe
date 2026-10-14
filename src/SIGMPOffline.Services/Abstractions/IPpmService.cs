using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Services.Abstractions;

/// <summary>Module PPM : saisie et révision du Plan de Passation des Marchés.</summary>
public interface IPpmService
{
    /// <summary>Liste les PPM (filtrés par exercice si précisé).</summary>
    Task<IReadOnlyList<PpmEntete>> ListerAsync(int? anneeExercice = null, CancellationToken cancellationToken = default);

    /// <summary>Récupère un PPM avec ses lignes (lève <c>EntityNotFoundException</c> si absent).</summary>
    Task<PpmEntete> ObtenirAsync(string numPpm, CancellationToken cancellationToken = default);

    /// <summary>Crée un nouvel en-tête de PPM.</summary>
    Task<PpmEntete> CreerAsync(PpmEntete entete, CancellationToken cancellationToken = default);

    /// <summary>Met à jour un en-tête de PPM.</summary>
    Task ModifierAsync(PpmEntete entete, CancellationToken cancellationToken = default);

    /// <summary>Supprime un PPM.</summary>
    Task SupprimerAsync(string numPpm, CancellationToken cancellationToken = default);

    /// <summary>Ajoute une ligne de détail à un PPM.</summary>
    Task<PpmDetail> AjouterDetailAsync(string numPpm, PpmDetail detail, CancellationToken cancellationToken = default);

    /// <summary>Met à jour une ligne de détail existante.</summary>
    Task ModifierDetailAsync(string numPpm, PpmDetail detail, CancellationToken cancellationToken = default);

    /// <summary>Supprime une ligne de détail.</summary>
    Task SupprimerDetailAsync(string numPpm, string numDetail, CancellationToken cancellationToken = default);

    /// <summary>Marque le PPM comme révisé (numéro et date de mise à jour).</summary>
    Task ReviserAsync(string numPpm, string? numMiseAJour, CancellationToken cancellationToken = default);
}
