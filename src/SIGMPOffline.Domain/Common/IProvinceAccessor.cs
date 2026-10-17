namespace SIGMPOffline.Domain.Common;

/// <summary>
/// Fournit la <b>province active</b> utilisée pour l'isolation des données
/// (filtres de requête EF Core) et l'estampillage automatique des nouvelles entités.
/// </summary>
public interface IProvinceAccessor
{
    /// <summary>
    /// Identifiant de la province active, ou <c>null</c> lorsqu'aucune province
    /// n'est sélectionnée (avant connexion, ou écran d'initialisation).
    /// </summary>
    int? IdProvince { get; }

    /// <summary>Définit la province active (<c>null</c> pour la désélectionner).</summary>
    void DefinirProvince(int? idProvince);
}
