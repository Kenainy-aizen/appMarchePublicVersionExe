using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Services.Models;

/// <summary>Répartition des marchés par statut d'exécution.</summary>
/// <param name="Statut">Statut d'exécution.</param>
/// <param name="Nombre">Nombre de marchés dans ce statut.</param>
/// <param name="Pourcentage">Part du statut dans l'ensemble filtré, en pourcentage.</param>
public sealed record RepartitionStatut(
    StatutExecution Statut,
    int Nombre,
    double Pourcentage);
