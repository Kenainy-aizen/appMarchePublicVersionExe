using SIGMPOffline.Services.Models;

namespace SIGMPOffline.Services.Abstractions;

/// <summary>Module Tableau de bord : indicateurs et alertes de dépassement.</summary>
public interface IDashboardService
{
    /// <summary>Calcule les indicateurs clés.</summary>
    Task<DashboardKpi> ObtenirKpiAsync(CancellationToken cancellationToken = default);

    /// <summary>Liste les marchés en dépassement de délai (les plus en retard d'abord).</summary>
    Task<IReadOnlyList<AlerteRetard>> ListerAlertesAsync(CancellationToken cancellationToken = default);
}
