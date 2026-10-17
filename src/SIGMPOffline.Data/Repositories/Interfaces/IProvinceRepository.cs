using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Repositories.Interfaces;

/// <summary>Accès aux provinces (et à leurs mots de passe).</summary>
public interface IProvinceRepository : IRepository<Province>
{
    /// <summary>Liste les provinces triées par nom.</summary>
    Task<IReadOnlyList<Province>> ListerAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrouve une province par son nom exact.</summary>
    Task<Province?> GetByNomAsync(string nom, CancellationToken cancellationToken = default);

    /// <summary>Indique si un nom de province existe déjà (hors <paramref name="saufId"/>).</summary>
    Task<bool> NomExisteAsync(string nom, int? saufId = null, CancellationToken cancellationToken = default);
}
