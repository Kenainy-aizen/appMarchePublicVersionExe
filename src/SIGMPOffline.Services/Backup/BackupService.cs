using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Services.Backup;

/// <inheritdoc />
public class BackupService : IBackupService
{
    private const string BackupFilePrefix = "sigmp-backup";

    private readonly AppDbContext _context;

    public BackupService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public string BaseDeDonneesPath
    {
        get
        {
            var connectionString = _context.Database.GetConnectionString()
                ?? throw new DomainException("Chaîne de connexion SQLite introuvable.");

            return new SqliteConnectionStringBuilder(connectionString).DataSource;
        }
    }

    /// <inheritdoc />
    public async Task<string> SauvegarderAsync(
        string? destinationDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new DomainException("Chaîne de connexion SQLite introuvable.");

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
            await _context.Database.CloseConnectionAsync();
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

    private static string DefaultBackupDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "SIGMPOffline-Backups");
    }
}
