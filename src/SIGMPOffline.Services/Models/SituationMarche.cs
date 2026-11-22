using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Services.Models;

/// <summary>Situation d'exécution synthétique d'un marché (pour l'affichage / le tableau de bord).</summary>
public sealed record SituationMarche
{
    /// <summary>Référence du marché.</summary>
    public required string RefMarche { get; init; }

    /// <summary>Objet du marché.</summary>
    public required string ObjetMarche { get; init; }

    /// <summary>Date de notification de l'Ordre de Service.</summary>
    public DateOnly? DateOS { get; init; }

    /// <summary>Date de réception provisoire.</summary>
    public DateOnly? DateReceptionProvisoire { get; init; }

    /// <summary>Date de réception définitive.</summary>
    public DateOnly? DateReceptionDefinitive { get; init; }

    /// <summary>Date de clôture.</summary>
    public DateOnly? DateCloture { get; init; }

    /// <summary>Référence du Titre d'Engagement Financier (TEF), le cas échéant.</summary>
    public string? RefTef { get; init; }

    /// <summary>Date limite d'exécution calculée.</summary>
    public DateOnly? DateLimiteExecution { get; init; }

    /// <summary>Jours restants (négatif si dépassé).</summary>
    public int? JoursRestants { get; init; }

    /// <summary>Vrai si le délai est dépassé et le marché non clôturé.</summary>
    public bool EstEnRetard { get; init; }

    /// <summary>Statut courant.</summary>
    public StatutExecution Statut { get; init; }

    /// <summary>Vrai si le marché est clôturé.</summary>
    public bool EstCloture { get; init; }

    /// <summary>Délai contractuel initial en jours.</summary>
    public int DelaiContractuelJours { get; init; }

    /// <summary>Prolongation cumulée des avenants en jours.</summary>
    public int ProlongationAvenantsJours { get; init; }

    /// <summary>Délai total après avenants.</summary>
    public int DelaiTotalJours => DelaiContractuelJours + ProlongationAvenantsJours;

    /// <summary>Montant HT initial du marché.</summary>
    public decimal MontantInitialHt { get; init; }

    /// <summary>Variation HT cumulée des avenants.</summary>
    public decimal VariationAvenants { get; init; }

    /// <summary>Montant HT après avenants.</summary>
    public decimal MontantFinalHt => MontantInitialHt + VariationAvenants;

    /// <summary>Libellés prêts à afficher dans la chronologie.</summary>
    public string EtatOs => DateOS is null ? "À faire" : "Fait";
    public string EtatTef => string.IsNullOrWhiteSpace(RefTef) ? "À faire" : "Fait";
    public string EtatReceptionProvisoire => DateReceptionProvisoire is null ? "À faire" : "Fait";
    public string EtatReceptionDefinitive => DateReceptionDefinitive is null ? "À faire" : "Fait";
    public string EtatCloture => EstCloture ? "Fait" : "À faire";
}
