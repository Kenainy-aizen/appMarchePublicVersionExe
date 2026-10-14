namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// Élément / article du bordereau d'un marché.
/// Table : <c>ElementMarche</c>.
/// </summary>
public class ElementMarche
{
    /// <summary>Code de l'élément (clé primaire).</summary>
    public string CodeElement { get; set; } = string.Empty;

    /// <summary>Clé étrangère vers le <see cref="Marche"/>.</summary>
    public string RefMarche { get; set; } = string.Empty;

    /// <summary>Libellé de la prestation / fourniture.</summary>
    public string Libelle { get; set; } = string.Empty;

    /// <summary>Prix unitaire.</summary>
    public decimal PrixUnitaire { get; set; }

    /// <summary>Quantité commandée.</summary>
    public decimal Quantite { get; set; }

    /// <summary>Unité de mesure (ex. : m², unité, forfait).</summary>
    public string Unite { get; set; } = string.Empty;

    /// <summary>Marché parent.</summary>
    public Marche? Marche { get; set; }

    /// <summary>Montant total de la ligne (calculé, non persisté).</summary>
    public decimal MontantLigne => PrixUnitaire * Quantite;
}
