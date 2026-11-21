using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// Ligne de détail du Plan de Passation des Marchés (une opération à passer).
/// Table : <c>PPMDetail</c>.
/// </summary>
public class PpmDetail
{
    /// <summary>Identifiant de la ligne (clé primaire).</summary>
    public string NumPPMDetail { get; set; } = string.Empty;

    /// <summary>Clé étrangère vers l'en-tête <see cref="PpmEntete"/>.</summary>
    public string NumPPMEntete { get; set; } = string.Empty;

    /// <summary>Nature du marché (travaux, fournitures, services…).</summary>
    public NatureMarche NatureMarche { get; set; }

    /// <summary>Objet du projet / de l'opération.</summary>
    public string ObjetProjet { get; set; } = string.Empty;

    /// <summary>Montant estimatif de l'opération.</summary>
    public decimal MontantEstimatif { get; set; }

    /// <summary>Mode de passation prévu.</summary>
    public ModePassation ModePassation { get; set; }

    /// <summary>Source de financement (budget national, bailleur…).</summary>
    public string? SourceFinancement { get; set; }

    /// <summary>Comptes budgétaires imputés (séparés par une virgule).</summary>
    public string? Comptes { get; set; }

    /// <summary>Date prévisionnelle de lancement de la procédure.</summary>
    public DateOnly? DateLancementPrevue { get; set; }

    /// <summary>Date prévisionnelle d'ouverture des plis.</summary>
    public DateOnly? DateOuverturePlisPrevue { get; set; }

    /// <summary>Date prévisionnelle d'attribution du marché.</summary>
    public DateOnly? DateAttributionPrevue { get; set; }

    /// <summary>Responsable de la conduite de la procédure.</summary>
    public string? ResponsableProcedure { get; set; }

    /// <summary>Services bénéficiaires et montants. S'il y en a, le montant estimatif en est la somme.</summary>
    public ICollection<PpmBeneficiaire> Beneficiaires { get; set; } = new List<PpmBeneficiaire>();

    /// <summary>Noms des services bénéficiaires, pour l'affichage (propriété calculée, non enregistrée).</summary>
    public string? ServicesBeneficiairesResume
        => Beneficiaires.Count == 0 ? null : string.Join(", ", Beneficiaires.Select(b => b.Service));

    /// <summary>En-tête parent.</summary>
    public PpmEntete? Entete { get; set; }

    /// <summary>Dossiers d'appel d'offres rattachés à cette ligne.</summary>
    public ICollection<DossierDao> Dossiers { get; set; } = new List<DossierDao>();
}
