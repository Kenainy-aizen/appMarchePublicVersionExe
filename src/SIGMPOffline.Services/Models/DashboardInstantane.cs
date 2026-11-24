namespace SIGMPOffline.Services.Models;

/// <summary>
/// Photographie complète du tableau de bord pour un filtre donné, calculée à
/// partir d'une seule lecture de la base.
/// </summary>
/// <param name="Kpi">Indicateurs clés.</param>
/// <param name="Alertes">Marchés en retard, les plus graves d'abord.</param>
/// <param name="Echeances">Échéances dans la fenêtre du filtre.</param>
/// <param name="Incomplets">Marchés dont le suivi est incomplet.</param>
/// <param name="Repartition">Répartition des marchés filtrés par statut.</param>
/// <param name="EcheancesParMois">Échéances des trois prochains mois.</param>
/// <param name="Exercices">Exercices présents dans la province (non filtrés).</param>
public sealed record DashboardInstantane(
    DashboardKpi Kpi,
    IReadOnlyList<AlerteRetard> Alertes,
    IReadOnlyList<AlerteEcheance> Echeances,
    IReadOnlyList<MarcheIncomplet> Incomplets,
    IReadOnlyList<RepartitionStatut> Repartition,
    IReadOnlyList<EcheanceMois> EcheancesParMois,
    IReadOnlyList<int> Exercices);
