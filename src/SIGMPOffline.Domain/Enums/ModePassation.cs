namespace SIGMPOffline.Domain.Enums;

/// <summary>Modes de passation des marchés publics.</summary>
public enum ModePassation
{
    /// <summary>Appel d'offres ouvert.</summary>
    AppelOffresOuvert = 0,

    /// <summary>Appel d'offres restreint.</summary>
    AppelOffresRestreint = 1,

    /// <summary>Consultation de prix.</summary>
    ConsultationPrix = 2,

    /// <summary>Appel à manifestation d'intérêt.</summary>
    AppelManifestationInteret = 3,

    /// <summary>Appel à préqualification.</summary>
    AppelPrequalification = 4
}
