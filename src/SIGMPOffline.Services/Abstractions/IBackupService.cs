namespace SIGMPOffline.Services.Abstractions;

/// <summary>Module Utilitaire : sauvegarde / restauration du fichier SQLite.</summary>
public interface IBackupService
{
    /// <summary>Chemin du fichier de base de données actif.</summary>
    string BaseDeDonneesPath { get; }

    /// <summary>
    /// Sauvegarde la base vers un fichier <c>.db</c> (support externe / clé USB).
    /// </summary>
    /// <param name="destinationDirectory">Dossier de destination (clé USB). Si <c>null</c>, dossier par défaut.</param>
    /// <returns>Le chemin complet du fichier de sauvegarde créé.</returns>
    Task<string> SauvegarderAsync(string? destinationDirectory = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restaure la base depuis un fichier de sauvegarde.
    /// ⚠️ Un redémarrage de l'application est recommandé après restauration.
    /// </summary>
    Task RestaurerAsync(string sourceFilePath, CancellationToken cancellationToken = default);
}
