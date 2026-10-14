using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Une valeur d'énumération accompagnée de son libellé d'affichage.</summary>
/// <typeparam name="T">Type de l'énumération.</typeparam>
public sealed record OptionEnum<T>(T Valeur, string Libelle) where T : struct, Enum
{
    /// <inheritdoc />
    public override string ToString() => Libelle;
}

/// <summary>Listes d'options (libellés français) pour les listes déroulantes de l'IHM.</summary>
public static class OptionsEnums
{
    /// <summary>Natures de marché.</summary>
    public static IReadOnlyList<OptionEnum<NatureMarche>> Natures { get; } =
    [
        new(NatureMarche.Travaux, "Travaux"),
        new(NatureMarche.Fournitures, "Fournitures"),
        new(NatureMarche.Services, "Services"),
        new(NatureMarche.PrestationsIntellectuelles, "Prestations intellectuelles"),
        new(NatureMarche.Autre, "Autre")
    ];

    /// <summary>Modes de passation.</summary>
    public static IReadOnlyList<OptionEnum<ModePassation>> ModesPassation { get; } =
    [
        new(ModePassation.AppelOffresOuvert, "Appel d'offres ouvert"),
        new(ModePassation.AppelOffresRestreint, "Appel d'offres restreint"),
        new(ModePassation.DemandeCotation, "Demande de cotation"),
        new(ModePassation.ConsultationRestreinte, "Consultation restreinte"),
        new(ModePassation.Concours, "Concours"),
        new(ModePassation.EntenteDirecte, "Entente directe"),
        new(ModePassation.Autre, "Autre")
    ];

    /// <summary>Statuts d'exécution d'un marché.</summary>
    public static IReadOnlyList<OptionEnum<StatutExecution>> Statuts { get; } =
    [
        new(StatutExecution.NonDemarre, "Non démarré"),
        new(StatutExecution.EnCours, "En cours"),
        new(StatutExecution.ReceptionProvisoire, "Réception provisoire"),
        new(StatutExecution.ReceptionDefinitive, "Réception définitive"),
        new(StatutExecution.Cloture, "Clôturé"),
        new(StatutExecution.Resilie, "Résilié"),
        new(StatutExecution.Annule, "Annulé")
    ];

    /// <summary>Retrouve l'option correspondant à une valeur donnée.</summary>
    public static OptionEnum<NatureMarche> Nature(NatureMarche valeur)
        => Natures.First(o => o.Valeur == valeur);

    /// <summary>Retrouve l'option correspondant à une valeur donnée.</summary>
    public static OptionEnum<ModePassation> Mode(ModePassation valeur)
        => ModesPassation.First(o => o.Valeur == valeur);

    /// <summary>Retrouve l'option correspondant à une valeur donnée.</summary>
    public static OptionEnum<StatutExecution> Statut(StatutExecution valeur)
        => Statuts.First(o => o.Valeur == valeur);
}

/// <summary>Traduit les valeurs d'énumération en libellés lisibles.</summary>
public static class Libelles
{
    /// <summary>Libellé français d'une valeur d'énumération.</summary>
    public static string Pour(Enum valeur) => valeur switch
    {
        NatureMarche nature => OptionsEnums.Nature(nature).Libelle,
        ModePassation mode => OptionsEnums.Mode(mode).Libelle,
        StatutExecution statut => OptionsEnums.Statut(statut).Libelle,
        _ => valeur.ToString()
    };
}
