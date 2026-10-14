using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Services.Abstractions;

/// <summary>Module Marché &amp; Bordereau : formalisation du contrat et éléments.</summary>
public interface IMarcheService
{
    /// <summary>Crée un marché rattaché à un dossier DAO existant.</summary>
    Task<Marche> CreerDepuisDossierAsync(Marche marche, CancellationToken cancellationToken = default);

    /// <summary>Récupère un marché complet (lève si absent).</summary>
    Task<Marche> ObtenirAsync(string refMarche, CancellationToken cancellationToken = default);

    /// <summary>Liste tous les marchés.</summary>
    Task<IReadOnlyList<Marche>> ListerAsync(CancellationToken cancellationToken = default);

    /// <summary>Ajoute un élément (article) au bordereau du marché.</summary>
    Task<ElementMarche> AjouterElementAsync(string refMarche, ElementMarche element, CancellationToken cancellationToken = default);

    /// <summary>Recalcule <c>MontantHT</c> / <c>MontantTTC</c> à partir du bordereau.</summary>
    Task RecalculerMontantsAsync(string refMarche, decimal tauxTVA = 0.18m, CancellationToken cancellationToken = default);

    /// <summary>Met à jour l'en-tête d'un marché existant.</summary>
    Task ModifierAsync(Marche marche, CancellationToken cancellationToken = default);

    /// <summary>Supprime un marché (ainsi que son bordereau et son suivi en cascade).</summary>
    Task SupprimerAsync(string refMarche, CancellationToken cancellationToken = default);

    /// <summary>Met à jour un élément du bordereau.</summary>
    Task ModifierElementAsync(string refMarche, ElementMarche element, CancellationToken cancellationToken = default);

    /// <summary>Supprime un élément du bordereau.</summary>
    Task SupprimerElementAsync(string refMarche, string codeElement, CancellationToken cancellationToken = default);
}
