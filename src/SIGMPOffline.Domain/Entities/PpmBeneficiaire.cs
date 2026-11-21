namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// Service bénéficiaire d'une ligne de PPM et montant qui lui est affecté.
/// Table : <c>PPMBeneficiaire</c>.
/// </summary>
public class PpmBeneficiaire
{
    /// <summary>Identifiant technique (clé primaire, auto-incrémenté).</summary>
    public int Id { get; set; }

    /// <summary>Clé étrangère vers la ligne <see cref="PpmDetail"/>.</summary>
    public string NumPPMDetail { get; set; } = string.Empty;

    /// <summary>Nom du service bénéficiaire.</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>Montant affecté à ce service (Ariary).</summary>
    public decimal Montant { get; set; }

    /// <summary>Ligne parente.</summary>
    public PpmDetail? Detail { get; set; }
}
