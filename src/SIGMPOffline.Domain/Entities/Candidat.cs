namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// Candidat / soumissionnaire ayant déposé une offre pour un dossier.
/// Table : <c>Candidat</c>.
/// </summary>
public class Candidat
{
    /// <summary>Identifiant (clé primaire).</summary>
    public int IdCandidat { get; set; }

    /// <summary>Clé étrangère vers le dossier <see cref="DossierDao"/>.</summary>
    public string RefDossier { get; set; } = string.Empty;

    /// <summary>Numéro de statistique.</summary>
    public string NumStat { get; set; } = string.Empty;

    /// <summary>Numéro d'identification fiscale (NIF).</summary>
    public string NumNIF { get; set; } = string.Empty;

    /// <summary>Nom de l'entreprise candidate.</summary>
    public string NomEntreprise { get; set; } = string.Empty;

    /// <summary>Montant de l'offre soumise.</summary>
    public decimal MontantOffre { get; set; }

    /// <summary>Indique si le candidat a été retenu (attributaire).</summary>
    public bool EstRetenu { get; set; }

    /// <summary>Dossier parent.</summary>
    public DossierDao? Dossier { get; set; }
}
