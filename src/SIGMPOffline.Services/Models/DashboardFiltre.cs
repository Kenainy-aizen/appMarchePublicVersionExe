using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Services.Models;

/// <summary>Filtres métier appliqués aux indicateurs du tableau de bord.</summary>
public sealed record DashboardFiltre(int? AnneeExercice = null, StatutExecution? Statut = null);
