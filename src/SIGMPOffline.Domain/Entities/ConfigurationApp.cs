namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// Configuration globale de l'application, créée une seule fois au premier lancement.
/// Table : <c>ConfigurationApp</c>.
/// </summary>
public class ConfigurationApp
{
    /// <summary>Identifiant (clé primaire).</summary>
    public int Id { get; set; }

    /// <summary>Nom du responsable PRMP / propriétaire de l'application.</summary>
    public string NomProprietairePRMP { get; set; } = string.Empty;

    /// <summary>Date d'installation (première configuration).</summary>
    public DateTime DateInstallation { get; set; }
}
