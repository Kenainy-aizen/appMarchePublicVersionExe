namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// Acheteur / opérateur ayant acquis un cahier des charges (acheteur du DAO).
/// Table : <c>AcheteurDAO</c>.
/// </summary>
public class AcheteurDao
{
    /// <summary>Identifiant (clé primaire).</summary>
    public int IdAcheteur { get; set; }

    /// <summary>Clé étrangère vers le dossier <see cref="DossierDao"/>.</summary>
    public string RefDossier { get; set; } = string.Empty;

    /// <summary>Numéro de statistique.</summary>
    public string NumStat { get; set; } = string.Empty;

    /// <summary>Numéro d'identification fiscale (NIF).</summary>
    public string NumNIF { get; set; } = string.Empty;

    /// <summary>Raison sociale de l'entreprise.</summary>
    public string RaisonSociale { get; set; } = string.Empty;

    /// <summary>Adresse postale / géographique.</summary>
    public string? Adresse { get; set; }

    /// <summary>Date d'achat du cahier des charges.</summary>
    public DateTime DateAchat { get; set; }

    /// <summary>Dossier parent.</summary>
    public DossierDao? Dossier { get; set; }
}
