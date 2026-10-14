using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Models;

namespace SIGMPOffline.Services.Abstractions;

/// <summary>Module Suivi d'Exécution (cœur) : TEF, OS, avenants, réceptions, clôture.</summary>
public interface ISuiviExecutionService
{
    /// <summary>Retourne la situation d'exécution calculée d'un marché.</summary>
    Task<SituationMarche> ObtenirSituationAsync(string refMarche, CancellationToken cancellationToken = default);

    /// <summary>Récupère le suivi d'exécution, ou le crée s'il n'existe pas encore.</summary>
    Task<SuiviExecution> ObtenirOuCreerSuiviAsync(string refMarche, CancellationToken cancellationToken = default);

    /// <summary>Renseigne le Titre d'Engagement Financier (TEF).</summary>
    Task EnregistrerTefAsync(string refMarche, string refTef, CancellationToken cancellationToken = default);

    /// <summary>Émet l'Ordre de Service et passe le marché « en cours ».</summary>
    Task EmettreOrdreServiceAsync(string refMarche, DateOnly dateOs, CancellationToken cancellationToken = default);

    /// <summary>Ajoute un avenant au marché.</summary>
    Task<Avenant> AjouterAvenantAsync(string refMarche, Avenant avenant, CancellationToken cancellationToken = default);

    /// <summary>Met à jour un avenant existant.</summary>
    Task ModifierAvenantAsync(string refMarche, Avenant avenant, CancellationToken cancellationToken = default);

    /// <summary>Supprime un avenant.</summary>
    Task SupprimerAvenantAsync(string refMarche, int idAvenant, CancellationToken cancellationToken = default);

    /// <summary>Enregistre la réception provisoire.</summary>
    Task EnregistrerReceptionProvisoireAsync(string refMarche, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Enregistre la réception définitive.</summary>
    Task EnregistrerReceptionDefinitiveAsync(string refMarche, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Clôture le marché.</summary>
    Task CloturerAsync(string refMarche, DateOnly dateCloture, CancellationToken cancellationToken = default);
}
