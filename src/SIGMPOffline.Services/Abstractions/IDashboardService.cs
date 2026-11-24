using SIGMPOffline.Services.Models;

namespace SIGMPOffline.Services.Abstractions;

/// <summary>Module Tableau de bord : indicateurs et alertes de dépassement.</summary>
public interface IDashboardService
{
    /// <summary>
    /// Calcule en une seule lecture de la base toutes les données du tableau de bord
    /// pour le filtre indiqué.
    /// </summary>
    Task<DashboardInstantane> ObtenirTableauDeBordAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default);

    /// <summary>Calcule les indicateurs clés.</summary>
    Task<DashboardKpi> ObtenirKpiAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default);

    /// <summary>Liste les marchés en dépassement de délai (les plus en retard d'abord).</summary>
    Task<IReadOnlyList<AlerteRetard>> ListerAlertesAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default);

    /// <summary>Liste les échéances dans la fenêtre définie par le filtre.</summary>
    Task<IReadOnlyList<AlerteEcheance>> ListerEcheancesAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default);

    /// <summary>Liste les marchés présentant au moins une information de suivi manquante.</summary>
    Task<IReadOnlyList<MarcheIncomplet>> ListerMarchesIncompletsAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default);

    /// <summary>Répartit les marchés filtrés par statut d'exécution.</summary>
    Task<IReadOnlyList<RepartitionStatut>> ListerRepartitionAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default);

    /// <summary>Compte les échéances des trois prochains mois, mois par mois.</summary>
    Task<IReadOnlyList<EcheanceMois>> ListerEcheancesParMoisAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default);

    /// <summary>Liste les exercices présents dans les marchés de la province.</summary>
    Task<IReadOnlyList<int>> ListerExercicesAsync(CancellationToken cancellationToken = default);
}
