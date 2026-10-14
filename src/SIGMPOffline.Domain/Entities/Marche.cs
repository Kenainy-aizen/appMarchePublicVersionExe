using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// Marché public formalisé (contrat) issu d'un dossier DAO.
/// Table : <c>Marche</c>.
/// </summary>
public class Marche
{
    /// <summary>Référence du marché (clé primaire).</summary>
    public string RefMarche { get; set; } = string.Empty;

    /// <summary>Clé étrangère vers le dossier <see cref="DossierDao"/>.</summary>
    public string RefDossier { get; set; } = string.Empty;

    /// <summary>Clé étrangère vers le candidat retenu (titulaire du marché), le cas échéant.</summary>
    public int? IdCandidatTitulaire { get; set; }

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

    /// <summary>Candidat retenu (titulaire) — relation 0..1.</summary>
    public Candidat? Titulaire { get; set; }

    /// <summary>Articles / éléments du bordereau.</summary>
    public ICollection<ElementMarche> Elements { get; set; } = new List<ElementMarche>();

    /// <summary>Suivi d'exécution (0 ou 1).</summary>
    public SuiviExecution? Suivi { get; set; }

    /// <summary>Avenants au marché.</summary>
    public ICollection<Avenant> Avenants { get; set; } = new List<Avenant>();
}
