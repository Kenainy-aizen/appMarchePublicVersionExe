using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Repositories.Interfaces;

/// <summary>Accès aux plans de passation des marchés (PPM).</summary>
public interface IPpmRepository : IRepository<PpmEntete>
{
    /// <summary>Charge un PPM avec ses lignes de détail.</summary>
    Task<PpmEntete?> GetWithDetailsAsync(string numPpm, CancellationToken cancellationToken = default);

    /// <summary>Liste les PPM d'un exercice budgétaire.</summary>
    Task<IReadOnlyList<PpmEntete>> ListByAnneeAsync(int annee, CancellationToken cancellationToken = default);
}
