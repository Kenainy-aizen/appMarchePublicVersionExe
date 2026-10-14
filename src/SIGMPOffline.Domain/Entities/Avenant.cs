namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// Avenant modifiant un marché (montant et/ou délai).
/// Table : <c>Avenant</c>.
/// </summary>
public class Avenant
{
    /// <summary>Identifiant (clé primaire).</summary>
    public int IdAvenant { get; set; }

    /// <summary>Clé étrangère vers le <see cref="Marche"/>.</summary>
    public string RefMarche { get; set; } = string.Empty;

    /// <summary>Numéro de l'avenant.</summary>
    public string NumAvenant { get; set; } = string.Empty;

    /// <summary>Motif de l'avenant.</summary>
    public string? Motif { get; set; }

    /// <summary>Variation du montant (positive ou négative).</summary>
    public decimal VariationMontant { get; set; }

    /// <summary>Prolongation du délai en jours (positive ou négative).</summary>
    public int ProlongationDelaiJours { get; set; }

    /// <summary>Date de signature de l'avenant.</summary>
    public DateOnly DateAvenant { get; set; }

    /// <summary>Marché parent.</summary>
    public Marche? Marche { get; set; }
}
