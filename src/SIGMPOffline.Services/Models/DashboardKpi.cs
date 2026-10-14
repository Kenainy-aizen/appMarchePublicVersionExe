namespace SIGMPOffline.Services.Models;

/// <summary>Indicateurs clés du tableau de bord.</summary>
/// <param name="TotalMarches">Nombre total de marchés.</param>
/// <param name="MarchesEnCours">Marchés en cours d'exécution.</param>
/// <param name="MarchesClotures">Marchés clôturés.</param>
/// <param name="MarchesEnRetard">Marchés en dépassement de délai.</param>
/// <param name="MontantTotalHt">Montant HT cumulé de tous les marchés.</param>
public sealed record DashboardKpi(
    int TotalMarches,
    int MarchesEnCours,
    int MarchesClotures,
    int MarchesEnRetard,
    decimal MontantTotalHt);
