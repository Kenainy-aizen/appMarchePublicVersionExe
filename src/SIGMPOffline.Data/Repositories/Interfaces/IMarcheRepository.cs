using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Data.Repositories.Interfaces;

/// <summary>Accès aux marchés et à leur exécution.</summary>
public interface IMarcheRepository : IRepository<Marche>
{
    /// <summary>Charge un marché complet (dossier, titulaire, éléments, suivi, avenants).</summary>
    Task<Marche?> GetWithExecutionAsync(string refMarche, CancellationToken cancellationToken = default);

    /// <summary>Liste tous les marchés avec leurs données d'exécution (suivi + avenants).</summary>
    Task<IReadOnlyList<Marche>> ListWithExecutionAsync(CancellationToken cancellationToken = default);
}
