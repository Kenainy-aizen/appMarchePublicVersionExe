using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using SIGMPOffline.Data;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Scoping;

namespace SIGMPOffline.Services.Tests;

/// <summary>
/// Vérifie que le module de sauvegarde fonctionne à travers les scopes dédiés,
/// sans conserver de contexte EF Core entre deux opérations.
/// </summary>
public class BackupServiceTests
{
    [Fact]
    public async Task La_restauration_et_la_sauvegarde_passent_par_des_scopes_dedies()
    {
        var dossier = Path.Combine(Path.GetTempPath(), $"sigmp-backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dossier);

        try
        {
            var cheminBase = Path.Combine(dossier, "sigmp.db");
            var cheminRestauration = Path.Combine(dossier, "a-restaurer.db");

            // Crée une vraie base SQLite, puis une copie servant de sauvegarde.
            await using (var creation = new SqliteConnection($"Data Source={cheminBase}"))
            {
                await creation.OpenAsync();
                await using var commande = creation.CreateCommand();
                commande.CommandText = "CREATE TABLE Temoin (Id INTEGER); INSERT INTO Temoin VALUES (1);";
                await commande.ExecuteNonQueryAsync();
            }

            File.Copy(cheminBase, cheminRestauration);

            var services = new ServiceCollection();
            services.AddSigmpDataAccess($"Data Source={cheminBase}");
            services.AddSigmpServices();

            using var provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });

            var runner = provider.GetRequiredService<IScopeRunner>();

            Assert.Equal(
                Path.GetFullPath(cheminBase),
                Path.GetFullPath(runner.Executer<IBackupService, string>(service => service.BaseDeDonneesPath)));

            await runner.ExecuterAsync<IBackupService>(
                service => service.RestaurerAsync(cheminRestauration));
            Assert.Equal(1, await CompterTemoinsAsync(cheminBase));

            var cheminSauvegarde = await runner.ExecuterAsync<IBackupService, string>(
                service => service.SauvegarderAsync(dossier));
            Assert.True(File.Exists(cheminSauvegarde));
            Assert.Equal(1, await CompterTemoinsAsync(cheminSauvegarde));
        }
        finally
        {
            Directory.Delete(dossier, recursive: true);
        }
    }

    private static async Task<long> CompterTemoinsAsync(string chemin)
    {
        await using var connexion = new SqliteConnection($"Data Source={chemin}");
        await connexion.OpenAsync();
        await using var commande = connexion.CreateCommand();
        commande.CommandText = "SELECT COUNT(*) FROM Temoin;";
        return (long)(await commande.ExecuteScalarAsync())!;
    }
}
