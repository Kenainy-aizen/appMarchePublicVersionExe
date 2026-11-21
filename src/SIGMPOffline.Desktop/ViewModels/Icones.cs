using Avalonia.Media;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// Géométries vectorielles (allure Fluent) utilisées par l'interface.
/// Chaque tracé est défini dans une zone de 24 × 24.
/// </summary>
public static class Icones
{
    /// <summary>Tableau de bord : grille 2 × 2.</summary>
    public static readonly Geometry TableauDeBord =
        Geometry.Parse("M4 4 H11 V11 H4 Z M13 4 H20 V11 H13 Z M4 13 H11 V20 H4 Z M13 13 H20 V20 H13 Z");

    /// <summary>Plan de passation : liste de lignes.</summary>
    public static readonly Geometry Ppm =
        Geometry.Parse("M5 4 H19 V7 H5 Z M5 10.5 H19 V13.5 H5 Z M5 17 H14 V20 H5 Z");

    /// <summary>DAO & Attribution : dossier.</summary>
    public static readonly Geometry Dao =
        Geometry.Parse("M3 6 H9.5 L11.5 8.5 H21 V19 H3 Z");

    /// <summary>Marchés : document à coin replié.</summary>
    public static readonly Geometry Marche =
        Geometry.Parse("M6 3 H14 L18 7 V21 H6 Z");

    /// <summary>Suivi d'exécution : histogramme.</summary>
    public static readonly Geometry Suivi =
        Geometry.Parse("M4 20 V12 H8 V20 Z M10 20 V6 H14 V20 Z M16 20 V9 H20 V20 Z");

    /// <summary>Sauvegarde : flèche vers un support.</summary>
    public static readonly Geometry Sauvegarde =
        Geometry.Parse("M11 4 H13 V11 H16.5 L12 15.5 L7.5 11 H11 Z M5 17 H19 V20 H5 Z");

    /// <summary>Province : repère de localisation.</summary>
    public static readonly Geometry Province =
        Geometry.Parse("M12 2 C8.7 2 6 4.7 6 8 C6 12.2 12 22 12 22 C12 22 18 12.2 18 8 C18 4.7 15.3 2 12 2 Z M12 10.5 A2.5 2.5 0 1 0 12 5.5 A2.5 2.5 0 1 0 12 10.5 Z");

    /// <summary>Nouvelle province : signe plus.</summary>
    public static readonly Geometry Ajouter =
        Geometry.Parse("M11 5 H13 V11 H19 V13 H13 V19 H11 V13 H5 V11 H11 Z");

    /// <summary>Mot de passe : cadenas.</summary>
    public static readonly Geometry Cadenas =
        Geometry.Parse("M8 10 V7 A4 4 0 0 1 16 7 V10 H18.5 V20.5 H5.5 V10 Z");

    /// <summary>Changer de province : double flèche.</summary>
    public static readonly Geometry Echange =
        Geometry.Parse("M4 8 H16 V5 L21 9.5 L16 14 V11 H4 Z M20 16 H8 V13 L3 17.5 L8 22 V19 H20 Z");

    /// <summary>Déconnexion : sortie.</summary>
    public static readonly Geometry Deconnexion =
        Geometry.Parse("M11 4 H5 V20 H11 V17.5 H7.5 V6.5 H11 Z M14.5 7 L20 12 L14.5 17 V13.5 H10 V10.5 H14.5 Z");

    /// <summary>Mot de passe visible : œil ouvert, tracé au trait (Stroke).</summary>
    public static readonly Geometry OeilTrait = Geometry.Parse(
        "M2 12 C4.5 7 8 5 12 5 C16 5 19.5 7 22 12 " +
        "C19.5 17 16 19 12 19 C8 19 4.5 17 2 12 Z " +
        "M15 12 A3 3 0 1 1 9 12 A3 3 0 1 1 15 12 Z");

    /// <summary>Mot de passe masqué : œil barré, tracé au trait (Stroke).</summary>
    public static readonly Geometry OeilBarreTrait = Geometry.Parse(
        "M2 12 C4.5 7 8 5 12 5 C16 5 19.5 7 22 12 " +
        "C19.5 17 16 19 12 19 C8 19 4.5 17 2 12 Z " +
        "M15 12 A3 3 0 1 1 9 12 A3 3 0 1 1 15 12 Z " +
        "M3 3 L21 21");

    /// <summary>Alerte : pastille avec point d'exclamation évidé.</summary>
    public static readonly Geometry Alerte = Geometry.Parse(
        "F0 M12 2 A10 10 0 1 1 12 22 A10 10 0 1 1 12 2 Z " +
        "M10.9 6.6 H13.1 L12.6 14.2 H11.4 Z " +
        "M11 16 H13 V18.6 H11 Z");

    /// <summary>Validation : coche.</summary>
    public static readonly Geometry Validation =
        Geometry.Parse("M9 16.2 L4.8 12 L3.4 13.4 L9 19 L21 7 L19.6 5.6 Z");

    /// <summary>Sécurité : bouclier.</summary>
    public static readonly Geometry Bouclier = Geometry.Parse(
        "M12 2 L20.4 5.2 V11.3 C20.4 16.4 17.1 20.6 12 22 C6.9 20.6 3.6 16.4 3.6 11.3 V5.2 Z");

    /// <summary>Utilisateur : silhouette.</summary>
    public static readonly Geometry Utilisateur = Geometry.Parse(
        "M12 3.4 A4.1 4.1 0 1 1 12 11.6 A4.1 4.1 0 1 1 12 3.4 Z " +
        "M12 13.6 C16.6 13.6 20.2 16.2 20.2 20.8 H3.8 C3.8 16.2 7.4 13.6 12 13.6 Z");

    /// <summary>Modifier : crayon.</summary>
    public static readonly Geometry Modifier = Geometry.Parse(
        "M4 16.8 V20 H7.2 L17.9 9.3 L14.7 6.1 Z M19.7 7.5 " +
        "C20.1 7.1 20.1 6.5 19.7 6.1 L17.9 4.3 C17.5 3.9 16.9 3.9 16.5 4.3 L15.1 5.7 L18.3 8.9 Z");

    /// <summary>Supprimer : corbeille.</summary>
    public static readonly Geometry Supprimer = Geometry.Parse(
        "M9 3 H15 L16 5 H20 V7 H4 V5 H8 Z M6 9 H18 L17 21 H7 Z " +
        "M9 11 V18 H11 V11 Z M13 11 V18 H15 V11 Z");

    /// <summary>Chargement : arc de cercle animé.</summary>
    public static readonly Geometry Actualiser = Geometry.Parse(
        "M12 2.4 A9.6 9.6 0 0 1 21.6 12 L18.4 12 A6.4 6.4 0 0 0 12 5.6 Z");
}
