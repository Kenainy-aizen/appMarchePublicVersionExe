namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// Suivi d'exécution d'un marché : TEF, Ordre de Service, réceptions, clôture.
/// Table : <c>SuiviExecution</c>.
/// </summary>
public class SuiviExecution
{
    /// <summary>Identifiant (clé primaire).</summary>
    public int IdSuivi { get; set; }

    /// <summary>Clé étrangère vers le <see cref="Marche"/> (relation 1-1).</summary>
    public string RefMarche { get; set; } = string.Empty;

    /// <summary>Référence du Titre d'Engagement Financier (TEF).</summary>
    public string? RefTEF { get; set; }

    /// <summary>Date de notification de l'Ordre de Service.</summary>
    public DateOnly? DateOS { get; set; }

    /// <summary>Date de réception provisoire.</summary>
    public DateOnly? DateReceptionProvisoire { get; set; }

    /// <summary>Date de réception définitive.</summary>
    public DateOnly? DateReceptionDefinitive { get; set; }

    /// <summary>Indique si le marché est clôturé.</summary>
    public bool EstCloture { get; set; }

    /// <summary>Date de clôture (renseignée si <see cref="EstCloture"/> est vrai).</summary>
    public DateOnly? DateCloture { get; set; }

    /// <summary>Marché parent.</summary>
    public Marche? Marche { get; set; }
}
