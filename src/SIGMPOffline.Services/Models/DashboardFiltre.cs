using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Services.Models;

/// <summary>Filtres métier appliqués aux indicateurs du tableau de bord.</summary>
/// <param name="AnneeExercice">Exercice PPM, ou <c>null</c> pour tous.</param>
/// <param name="Statut">Statut d'exécution, ou <c>null</c> pour tous.</param>
/// <param name="JoursEcheances">Fenêtre d'affichage des échéances, en jours.</param>
public sealed record DashboardFiltre(
    int? AnneeExercice = null,
    StatutExecution? Statut = null,
    int JoursEcheances = 30);
