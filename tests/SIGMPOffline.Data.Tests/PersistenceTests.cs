using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data;
using SIGMPOffline.Data.Database;
using SIGMPOffline.Data.Repositories;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Data.Tests;

/// <summary>
/// Vérifie la persistance SQLite, notamment la conversion des montants
/// <see cref="decimal"/> en entiers (centimes).
/// </summary>
public class PersistenceTests
{
    private static async Task<(AppDbContext Context, SqliteConnection Connection)> CreateContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options, new ProvinceAccessor());
        await context.Database.EnsureCreatedAsync();
        return (context, connection);
    }

    [Fact]
    public async Task Un_montant_decimal_est_stocke_en_centimes_et_restitue_a_l_identique()
    {
        var (context, connection) = await CreateContextAsync();
        await using var _ = connection;

        context.PpmEntetes.Add(new PpmEntete
        {
            NumPPM = "PPM-2026",
            AnneeExercice = 2026,
            AutoriteContractante = "PRMP",
            DateElaboration = new DateOnly(2026, 1, 1),
            Details =
            {
                new PpmDetail
                {
                    NumPPMDetail = "D-001",
                    NatureMarche = NatureMarche.Travaux,
                    ObjetProjet = "Route",
                    MontantEstimatif = 1234.56m,
                    ModePassation = ModePassation.AppelOffresOuvert
                }
            }
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var detail = await context.PpmDetails.SingleAsync();

        // Valeur restituée exacte côté C#.
        Assert.Equal(1234.56m, detail.MontantEstimatif);

        // Valeur réellement stockée : entier en centimes.
        var stored = await context.Database
            .SqlQuery<long>($"SELECT \"MontantEstimatif\" AS \"Value\" FROM \"PPMDetail\"")
            .SingleAsync();
        Assert.Equal(123_456L, stored);
    }

    [Fact]
    public async Task SaveChangesAsync_reste_utilisable_apres_un_doublon_de_cle()
    {
        var (context, connection) = await CreateContextAsync();
        await using var _ = connection;
        await using var unitOfWork = new UnitOfWork(context);

        context.Provinces.Add(new Province
        {
            NomProvince = "Analamanga",
            PasswordHash = "hash",
            Salt = "sel",
            DateCreation = new DateTime(2026, 1, 1)
        });
        await unitOfWork.SaveChangesAsync();

        var doublon = new Province
        {
            NomProvince = "Analamanga",
            PasswordHash = "hash",
            Salt = "sel",
            DateCreation = new DateTime(2026, 1, 2)
        };
        context.Provinces.Add(doublon);

        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());

        // L'entité refusée est détachée : le contexte n'est plus « empoisonné ».
        Assert.Equal(EntityState.Detached, context.Entry(doublon).State);

        context.Provinces.Add(new Province
        {
            NomProvince = "Vakinankaratra",
            PasswordHash = "hash",
            Salt = "sel",
            DateCreation = new DateTime(2026, 1, 3)
        });

        var enregistres = await unitOfWork.SaveChangesAsync();

        Assert.Equal(1, enregistres);
        Assert.Equal(2, await context.Provinces.CountAsync());
    }

    [Fact]
    public async Task Le_mode_de_passation_est_stocke_en_texte()
    {
        var (context, connection) = await CreateContextAsync();
        await using var _ = connection;

        context.PpmEntetes.Add(new PpmEntete
        {
            NumPPM = "PPM-2026",
            AnneeExercice = 2026,
            AutoriteContractante = "PRMP",
            DateElaboration = new DateOnly(2026, 1, 1),
            Details =
            {
                new PpmDetail
                {
                    NumPPMDetail = "D-001",
                    NatureMarche = NatureMarche.Travaux,
                    ObjetProjet = "Route",
                    MontantEstimatif = 1m,
                    ModePassation = ModePassation.AppelPrequalification
                }
            }
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.Database
            .SqlQuery<string>($"SELECT \"ModePassation\" AS \"Value\" FROM \"PPMDetail\"")
            .SingleAsync();

        Assert.Equal("AppelPrequalification", stored);
    }
}
