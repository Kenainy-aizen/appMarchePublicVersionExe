namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// En-tête du Plan de Passation des Marchés (PPM) pour un exercice budgétaire.
/// Table : <c>PPMEntete</c>.
/// </summary>
public class PpmEntete
{
    /// <summary>Numéro du PPM (clé primaire).</summary>
    public string NumPPM { get; set; } = string.Empty;

    /// <summary>Année / exercice budgétaire.</summary>
    public int AnneeExercice { get; set; }

    /// <summary>Autorité contractante responsable.</summary>
    public string AutoriteContractante { get; set; } = string.Empty;

    /// <summary>Date d'élaboration du PPM.</summary>
    public DateOnly DateElaboration { get; set; }

    /// <summary>Date de la dernière mise à jour (révision).</summary>
    public DateTime? DateDerniereMAJ { get; set; }

    /// <summary>Numéro de la dernière mise à jour (révision).</summary>
    public string? NumDerniereMAJ { get; set; }

    /// <summary>Lignes de détail rattachées à cet en-tête.</summary>
    public ICollection<PpmDetail> Details { get; set; } = new List<PpmDetail>();
}
