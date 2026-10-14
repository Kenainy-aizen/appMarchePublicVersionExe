namespace SIGMPOffline.Domain.Enums;

/// <summary>Nature d'un marché / d'une ligne du PPM.</summary>
public enum NatureMarche
{
    /// <summary>Travaux.</summary>
    Travaux = 0,

    /// <summary>Fournitures.</summary>
    Fournitures = 1,

    /// <summary>Services.</summary>
    Services = 2,

    /// <summary>Prestations intellectuelles.</summary>
    PrestationsIntellectuelles = 3,

    /// <summary>Autre nature.</summary>
    Autre = 99
}
