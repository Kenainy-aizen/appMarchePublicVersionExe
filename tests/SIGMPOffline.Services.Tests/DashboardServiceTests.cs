using SIGMPOffline.Domain.Enums;
using SIGMPOffline.Services.Dashboard;
using SIGMPOffline.Services.Tests.Infrastructure;

namespace SIGMPOffline.Services.Tests;

/// <summary>Tests du tableau de bord (KPI et alertes).</summary>
public class DashboardServiceTests
{
    [Fact]
    public async Task Kpi_compte_les_marches_et_le_montant_total()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-001");
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-002");
        var dashboard = new DashboardService(db.UnitOfWork);

        var kpi = await dashboard.ObtenirKpiAsync();

        Assert.Equal(2, kpi.TotalMarches);
        Assert.Equal(1_800_000m, kpi.MontantTotalHt); // 2 × 900 000
    }

    [Fact]
    public async Task Alertes_retournent_les_marches_en_retard_tries_par_gravite()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-LENT", delaiJours: 1);
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-OK", delaiJours: 365);

        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        var aujourdhui = DateOnly.FromDateTime(DateTime.Today);
        await suivi.EmettreOrdreServiceAsync("M-LENT", aujourdhui.AddDays(-10));
        await suivi.EmettreOrdreServiceAsync("M-OK", aujourdhui);

        var dashboard = new DashboardService(db.UnitOfWork);
        var alertes = await dashboard.ListerAlertesAsync();

        Assert.Single(alertes);
        Assert.Equal("M-LENT", alertes[0].RefMarche);
        Assert.True(alertes[0].JoursDeRetard > 0);
    }

    [Fact]
    public async Task Un_marche_cloture_n_apparait_pas_dans_les_alertes()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-001", delaiJours: 1);

        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        await suivi.EmettreOrdreServiceAsync("M-001", DateOnly.FromDateTime(DateTime.Today).AddDays(-10));
        await suivi.CloturerAsync("M-001", DateOnly.FromDateTime(DateTime.Today));

        var dashboard = new DashboardService(db.UnitOfWork);
        var alertes = await dashboard.ListerAlertesAsync();
        var kpi = await dashboard.ObtenirKpiAsync();

        Assert.Empty(alertes);
        Assert.Equal(1, kpi.MarchesClotures);
        Assert.Equal(0, kpi.MarchesEnRetard);
    }
}
