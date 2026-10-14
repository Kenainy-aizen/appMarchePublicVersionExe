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
}
