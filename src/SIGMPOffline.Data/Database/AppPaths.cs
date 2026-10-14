namespace SIGMPOffline.Data.Database;

/// <summary>
/// Résout les chemins de stockage locaux de l'application (base SQLite, sauvegardes).
/// Compatible Linux / macOS / Windows via <see cref="Environment.SpecialFolder"/>.
/// </summary>
public static class AppPaths
{
    /// <summary>Nom du dossier applicatif dans les données utilisateur.</summary>
    public const string AppFolderName = "SIGMPOffline";

    /// <summary>Nom du fichier de base de données SQLite.</summary>
    public const string DatabaseFileName = "sigmp.db";

    /// <summary>Dossier de données utilisateur (créé si absent).</summary>
    public static string DataDirectory
    {
        get
        {
            // Linux : ~/.local/share/SIGMPOffline
            var baseDir = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create);

            if (string.IsNullOrWhiteSpace(baseDir))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                baseDir = Path.Combine(home, ".local", "share");
            }

            var dir = Path.Combine(baseDir, AppFolderName);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>Chemin complet du fichier de base de données par défaut.</summary>
    public static string DefaultDatabasePath => Path.Combine(DataDirectory, DatabaseFileName);

    /// <summary>Chaîne de connexion SQLite par défaut.</summary>
    public static string DefaultConnectionString => $"Data Source={DefaultDatabasePath}";
}
