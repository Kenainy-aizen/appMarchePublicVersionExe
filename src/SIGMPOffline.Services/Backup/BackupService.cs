using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Scoping;

namespace SIGMPOffline.Services.Backup;

/// <inheritdoc />
/// <remarks>
/// Service Scoped utilisé par un ViewModel Singleton : chaque opération passe par
/// <see cref="IScopeRunner"/> afin de ne jamais conserver un contexte EF Core.
/// </remarks>
public class BackupService : IBackupService
{
    private const string BackupFilePrefix = "sigmp-backup";

    private readonly IScopeRunner _scopeRunner;

    public BackupService(IScopeRunner scopeRunner)
    {
        _scopeRunner = scopeRunner ?? throw new ArgumentNullException(nameof(scopeRunner));
    }

    /// <inheritdoc />
    public string BaseDeDonneesPath
        => _scopeRunner.Executer<AppDbContext, string>(CheminDeLaBase);

    /// <inheritdoc />
    public async Task<string> SauvegarderAsync(
        string? destinationDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _scopeRunner.Executer<AppDbContext, string>(CheminDeConnexion);

        var directory = string.IsNullOrWhiteSpace(destinationDirectory)
            ? DefaultBackupDirectory()
            : destinationDirectory;

        Directory.CreateDirectory(directory);

        var destinationPath = Path.Combine(
            directory,
            $"{BackupFilePrefix}-{DateTime.Now:yyyyMMdd-HHmmss}.db");

        try
        {
            // Sauvegarde cohérente, compatible WAL (contrairement à une simple copie de fichier).
            await using var source = new SqliteConnection(connectionString);
            await source.OpenAsync(cancellationToken);

            await using var destination = new SqliteConnection($"Data Source={destinationPath}");
            await destination.OpenAsync(cancellationToken);

            source.BackupDatabase(destination);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new DomainException($"Échec de la sauvegarde vers « {destinationPath} ».", ex);
        }

        return destinationPath;
    }

    /// <inheritdoc />
    public async Task RestaurerAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
        {
            throw new DomainException($"Fichier de sauvegarde introuvable : « {sourceFilePath} ».");
        }

        var destinationPath = BaseDeDonneesPath;

        try
        {
            // Ferme la connexion du contexte et vide le pool avant d'écraser le fichier.
            await _scopeRunner.ExecuterAsync<AppDbContext>(
                context => context.Database.CloseConnectionAsync());
            SqliteConnection.ClearAllPools();

            await Task.Run(() =>
            {
                File.Copy(sourceFilePath, destinationPath, overwrite: true);

                // Supprime les journaux WAL/SHM résiduels pour éviter une base incohérente.
                foreach (var suffix in new[] { "-wal", "-shm" })
                {
                    var sidecar = destinationPath + suffix;
                    if (File.Exists(sidecar))
                    {
                        File.Delete(sidecar);
                    }
                }
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new DomainException($"Échec de la restauration depuis « {sourceFilePath} ».", ex);
        }
    }

    private static string CheminDeConnexion(AppDbContext context)
        => context.Database.GetConnectionString()
            ?? throw new DomainException("Chaîne de connexion SQLite introuvable.");

    private static string CheminDeLaBase(AppDbContext context)
        => new SqliteConnectionStringBuilder(CheminDeConnexion(context)).DataSource;

    private static string DefaultBackupDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "SIGMPOffline-Backups");
    }
}
