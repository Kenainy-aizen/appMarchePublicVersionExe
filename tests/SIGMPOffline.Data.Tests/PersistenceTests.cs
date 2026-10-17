using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data;
using SIGMPOffline.Data.Database;
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
                    ModePassation = ModePassation.EntenteDirecte
                }
            }
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.Database
            .SqlQuery<string>($"SELECT \"ModePassation\" AS \"Value\" FROM \"PPMDetail\"")
            .SingleAsync();

        Assert.Equal("EntenteDirecte", stored);
    }
}
