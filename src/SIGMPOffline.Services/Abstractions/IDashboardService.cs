using SIGMPOffline.Services.Models;

namespace SIGMPOffline.Services.Abstractions;

/// <summary>Module Tableau de bord : indicateurs et alertes de dépassement.</summary>
public interface IDashboardService
{
    /// <summary>Calcule les indicateurs clés.</summary>
    Task<DashboardKpi> ObtenirKpiAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default);

    /// <summary>Liste les marchés en dépassement de délai (les plus en retard d'abord).</summary>
    Task<IReadOnlyList<AlerteRetard>> ListerAlertesAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default);

    /// <summary>Liste les échéances dans les 30 prochains jours.</summary>
    Task<IReadOnlyList<AlerteEcheance>> ListerEcheancesAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default);

    /// <summary>Liste les marchés présentant une information de suivi manquante.</summary>
    Task<IReadOnlyList<MarcheIncomplet>> ListerMarchesIncompletsAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default);

    /// <summary>Liste les exercices présents dans les marchés de la province.</summary>
    Task<IReadOnlyList<int>> ListerExercicesAsync(CancellationToken cancellationToken = default);
}
