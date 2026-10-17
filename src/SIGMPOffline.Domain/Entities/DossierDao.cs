using SIGMPOffline.Domain.Common;

namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// Dossier d'Appel d'Offres (DAO) rattaché à une ligne du PPM.
/// Table : <c>DossierDAO</c>.
/// </summary>
public class DossierDao : IProvinceScoped
{
    /// <summary>Référence du dossier (clé primaire).</summary>
    public string RefDossier { get; set; } = string.Empty;

    /// <summary>Clé étrangère vers la province propriétaire des données.</summary>
    public int? IdProvince { get; set; }

    /// <summary>Province propriétaire des données.</summary>
    public Province? Province { get; set; }

    /// <summary>Clé étrangère vers la ligne PPM <see cref="PpmDetail"/>.</summary>
    public string NumPPMDetail { get; set; } = string.Empty;

    /// <summary>Date de création du dossier.</summary>
    public DateTime DateCreation { get; set; }

    /// <summary>Objet du dossier d'appel d'offres.</summary>
    public string ObjetDossier { get; set; } = string.Empty;

    /// <summary>Coût du cahier des charges.</summary>
    public decimal CoutCahierCharges { get; set; }

    /// <summary>Montant de la caution de soumission.</summary>
    public decimal MontantCaution { get; set; }

    /// <summary>Ligne PPM parente.</summary>
    public PpmDetail? PpmDetail { get; set; }

    /// <summary>Acheteurs du dossier (ayant acquis le cahier des charges).</summary>
    public ICollection<AcheteurDao> Acheteurs { get; set; } = new List<AcheteurDao>();

    /// <summary>Soumissionnaires / candidats.</summary>
    public ICollection<Candidat> Candidats { get; set; } = new List<Candidat>();

    /// <summary>Marché issu de ce dossier (0 ou 1).</summary>
    public Marche? Marche { get; set; }
}
