namespace SIGMPOffline.Domain.Enums;

/// <summary>Modes de passation des marchés publics.</summary>
public enum ModePassation
{
    /// <summary>Appel d'offres ouvert.</summary>
    AppelOffresOuvert = 0,

    /// <summary>Appel d'offres restreint.</summary>
    AppelOffresRestreint = 1,

    /// <summary>Demande de cotation.</summary>
    DemandeCotation = 2,

    /// <summary>Consultation restreinte.</summary>
    ConsultationRestreinte = 3,

    /// <summary>Concours.</summary>
    Concours = 4,

    /// <summary>Entente directe / gré à gré.</summary>
    EntenteDirecte = 5,

    /// <summary>Autre mode non listé.</summary>
    Autre = 99
}
