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

    /// <summary>En-tête parent.</summary>
    public PpmEntete? Entete { get; set; }

    /// <summary>Dossiers d'appel d'offres rattachés à cette ligne.</summary>
    public ICollection<DossierDao> Dossiers { get; set; } = new List<DossierDao>();
}
