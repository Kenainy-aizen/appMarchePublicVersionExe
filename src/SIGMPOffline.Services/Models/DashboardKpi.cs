namespace SIGMPOffline.Services.Models;

/// <summary>Indicateurs clés du tableau de bord.</summary>
/// <param name="TotalMarches">Nombre total de marchés.</param>
/// <param name="MarchesEnCours">Marchés en cours d'exécution.</param>
/// <param name="MarchesClotures">Marchés clôturés.</param>
/// <param name="MarchesEnRetard">Marchés en dépassement de délai.</param>
/// <param name="MarchesNonDemarres">Marchés dont l'ordre de service n'a pas encore été émis.</param>
public sealed record DashboardKpi(
    int TotalMarches,
    int MarchesEnCours,
    int MarchesClotures,
    int MarchesEnRetard,
    int MarchesNonDemarres)
{
    /// <summary>Taux de clôture des marchés, en pourcentage (0 à 100).</summary>
    public decimal TauxCloture => TotalMarches == 0
        ? 0
        : Math.Round((decimal)MarchesClotures * 100m / TotalMarches, 1);
}
