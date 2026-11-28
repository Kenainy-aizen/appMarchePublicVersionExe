using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
            Assert.Contains("Utilisateur", tables);
            Assert.Contains("PermissionProvince", tables);

            var colonnesUtilisateur = await context.Database
                .SqlQuery<string>($"SELECT name AS \"Value\" FROM pragma_table_info('Utilisateur')")
                .ToListAsync();
            Assert.Contains("NomUtilisateur", colonnesUtilisateur);
            Assert.Contains("EstAdministrateur", colonnesUtilisateur);

            var colonnesDroits = await context.Database
                .SqlQuery<string>($"SELECT name AS \"Value\" FROM pragma_table_info('PermissionProvince')")
                .ToListAsync();
            Assert.Contains("IdProvince", colonnesDroits);
            Assert.Contains("PeutModifier", colonnesDroits);

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

    [Fact]
    public async Task Migration_des_comptes_preserve_le_compte_prmp_et_les_visiteurs_existants()
    {
        var chemin = Path.Combine(Path.GetTempPath(), $"sigmp-comptes-{Guid.NewGuid():N}.db");

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={chemin}")
                .Options;
            await using var context = new AppDbContext(options, new ProvinceAccessor());
            var migrateur = context.GetService<IMigrator>();
            await migrateur.MigrateAsync("20261125185738_AccesVisiteur");

            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO ConfigurationApp (NomProprietairePRMP, DateInstallation) VALUES ('PRMP Jean', '2026-01-01 00:00:00')");
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO Province (NomProvince, PasswordHash, Salt, DateCreation, VisitorPasswordHash, VisitorSalt) " +
                "VALUES ('Province A', 'admin-hash', 'admin-salt', '2026-01-01 00:00:00', NULL, NULL)");
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO Province (NomProvince, PasswordHash, Salt, DateCreation, VisitorPasswordHash, VisitorSalt) " +
                "VALUES ('Province B', 'autre-hash', 'autre-salt', '2026-01-02 00:00:00', 'visiteur-hash', 'visiteur-salt')");

            await migrateur.MigrateAsync(null);

            var admin = await context.Utilisateurs.AsNoTracking().SingleAsync(x => x.EstAdministrateur);
            Assert.Equal("PRMP Jean", admin.NomUtilisateur);
            Assert.Equal("admin-hash", admin.PasswordHash);
            Assert.Equal("admin-salt", admin.Salt);

            var ancienCompteProvince = await context.Utilisateurs.AsNoTracking()
                .Include(x => x.PermissionsProvinces)
                .SingleAsync(x => x.NomUtilisateur == "Province-2");
            Assert.Equal("autre-hash", ancienCompteProvince.PasswordHash);
            Assert.Single(ancienCompteProvince.PermissionsProvinces);
            Assert.True(ancienCompteProvince.PermissionsProvinces.Single().PeutModifier);

            var visiteur = await context.Utilisateurs.AsNoTracking()
                .Include(x => x.PermissionsProvinces)
                .SingleAsync(x => x.NomUtilisateur == "Visiteur-2");
            Assert.Equal("visiteur-hash", visiteur.PasswordHash);
            Assert.Single(visiteur.PermissionsProvinces);
            Assert.Equal(2, visiteur.PermissionsProvinces.Single().IdProvince);
            Assert.False(visiteur.PermissionsProvinces.Single().PeutModifier);

            await migrateur.MigrateAsync("20261125185738_AccesVisiteur");
            var motDePasseAdminRestaure = await context.Database
                .SqlQuery<string>($"SELECT PasswordHash AS \"Value\" FROM Province WHERE IdProvince = 1")
                .SingleAsync();
            var motDePasseProvinceBRestaure = await context.Database
                .SqlQuery<string>($"SELECT PasswordHash AS \"Value\" FROM Province WHERE IdProvince = 2")
                .SingleAsync();
            var motDePasseVisiteurRestaure = await context.Database
                .SqlQuery<string>($"SELECT VisitorPasswordHash AS \"Value\" FROM Province WHERE IdProvince = 2")
                .SingleAsync();
            Assert.Equal("admin-hash", motDePasseAdminRestaure);
            Assert.Equal("autre-hash", motDePasseProvinceBRestaure);
            Assert.Equal("visiteur-hash", motDePasseVisiteurRestaure);
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
