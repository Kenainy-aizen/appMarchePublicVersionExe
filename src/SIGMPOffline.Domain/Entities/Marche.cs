using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// Marché public formalisé (contrat) issu d'un dossier DAO.
/// Table : <c>Marche</c>.
/// </summary>
public class Marche : IProvinceScoped
{
    /// <summary>Référence du marché (clé primaire).</summary>
    public string RefMarche { get; set; } = string.Empty;

    /// <summary>Clé étrangère vers la province propriétaire des données.</summary>
    public int? IdProvince { get; set; }

    /// <summary>Province propriétaire des données.</summary>
    public Province? Province { get; set; }

    /// <summary>Clé étrangère vers le dossier <see cref="DossierDao"/>.</summary>
    public string RefDossier { get; set; } = string.Empty;

    /// <summary>
    /// Clé étrangère vers l'acheteur titulaire du marché (numéro de statistique),
    /// le cas échéant. Référence <see cref="AcheteurDao.NumStat"/>.
    /// </summary>
    public string? NumStatTitulaire { get; set; }

    /// <summary>Objet du marché.</summary>
    public string ObjetMarche { get; set; } = string.Empty;

    /// <summary>Montant hors taxes.</summary>
    public decimal MontantHT { get; set; }

    /// <summary>Montant toutes taxes comprises.</summary>
    public decimal MontantTTC { get; set; }

    /// <summary>Délai d'exécution contractuel, en jours.</summary>
    public int DelaiExecutionJours { get; set; }

    /// <summary>Statut courant de l'exécution.</summary>
    public StatutExecution StatutExecution { get; set; }

    /// <summary>Dossier DAO d'origine (0 ou 1).</summary>
    public DossierDao? Dossier { get; set; }

    /// <summary>Acheteur titulaire du marché — relation 0..1.</summary>
    public AcheteurDao? Titulaire { get; set; }

    /// <summary>Articles / éléments du bordereau.</summary>
    public ICollection<ElementMarche> Elements { get; set; } = new List<ElementMarche>();

    /// <summary>Suivi d'exécution (0 ou 1).</summary>
    public SuiviExecution? Suivi { get; set; }

    /// <summary>Avenants au marché.</summary>
    public ICollection<Avenant> Avenants { get; set; } = new List<Avenant>();
}
