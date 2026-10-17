using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Repositories.Interfaces;

/// <summary>Accès à la configuration globale de l'application.</summary>
public interface IConfigurationAppRepository : IRepository<ConfigurationApp>
{
    /// <summary>Retourne la configuration (unique) de l'application, ou <c>null</c> si non configurée.</summary>
    Task<ConfigurationApp?> GetConfigurationAsync(CancellationToken cancellationToken = default);
}
