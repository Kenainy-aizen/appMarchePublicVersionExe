using Avalonia.Media;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// Géométries vectorielles (allure Fluent) utilisées par l'interface.
/// Chaque tracé est défini dans une zone de 24 × 24.
/// </summary>
internal static class Icones
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
}
