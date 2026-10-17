using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data;
using SIGMPOffline.Data.Database;

namespace SIGMPOffline.Data.Tests;

/// <summary>
/// Vérifie la chaîne de migrations EF Core sur un vrai fichier SQLite :
/// le schéma doit être complet et cohérent (intégrité référentielle).
/// </summary>
public class MigrationTests
{
    [Fact]
    public async Task Les_migrations_produisent_un_schema_complet_et_coherent()
    {
        var chemin = Path.Combine(Path.GetTempPath(), $"sigmp-migrations-{Guid.NewGuid():N}.db");

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={chemin}")
                .Options;

            await using var context = new AppDbContext(options, new ProvinceAccessor());
            await context.Database.MigrateAsync();

            // Toutes les migrations sont appliquées.
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());

            // Les tables d'authentification existent.
            var tables = await context.Database
                .SqlQuery<string>($"SELECT name AS \"Value\" FROM sqlite_master WHERE type = 'table'")
                .ToListAsync();
            Assert.Contains("Province", tables);
            Assert.Contains("ConfigurationApp", tables);

            // La clé d'isolation multi-provinces est présente sur les agrégats racines.
            foreach (var table in new[] { "PPMEntete", "DossierDAO", "Marche" })
            {
                var colonnes = await context.Database
                    .SqlQuery<string>($"SELECT name AS \"Value\" FROM pragma_table_info({table})")
                    .ToListAsync();
                Assert.Contains("IdProvince", colonnes);
            }

            // Aucune violation de clé étrangère après application des migrations.
            var connexion = context.Database.GetDbConnection();
            await context.Database.OpenConnectionAsync();
            await using var commande = connexion.CreateCommand();
            commande.CommandText = "PRAGMA foreign_key_check";
            await using var lecteur = await commande.ExecuteReaderAsync();
            Assert.False(await lecteur.ReadAsync(), "La base contient des violations de clé étrangère.");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(chemin))
            {
                File.Delete(chemin);
            }
        }
    }
}
