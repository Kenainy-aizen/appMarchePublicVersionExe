namespace SIGMPOffline.Domain.Enums;

/// <summary>Statut d'exécution d'un marché.</summary>
public enum StatutExecution
{
    /// <summary>Marché signé mais pas encore démarré (pas d'OS).</summary>
    NonDemarre = 0,

    /// <summary>Ordre de service émis, exécution en cours.</summary>
    EnCours = 1,

    /// <summary>Réception provisoire prononcée.</summary>
    ReceptionProvisoire = 2,

    /// <summary>Réception définitive prononcée.</summary>
    ReceptionDefinitive = 3,

    /// <summary>Marché clôturé.</summary>
    Cloture = 4,

    /// <summary>Marché résilié.</summary>
    Resilie = 5,

    /// <summary>Marché annulé.</summary>
    Annule = 6
}
