using SIGMPOffline.Domain.Enums;
using SIGMPOffline.Services.Dashboard;
using SIGMPOffline.Services.Models;
using SIGMPOffline.Services.Tests.Infrastructure;

namespace SIGMPOffline.Services.Tests;

/// <summary>Tests du tableau de bord (KPI et alertes).</summary>
public class DashboardServiceTests
{
    [Fact]
    public async Task Dashboard_filtre_par_exercice_et_statut_les_indicateurs()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-001");
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-002");
        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        await suivi.EmettreOrdreServiceAsync("M-001", DateOnly.FromDateTime(DateTime.Today));
        var dashboard = new DashboardService(db.UnitOfWork);

        var kpi = await dashboard.ObtenirKpiAsync(new DashboardFiltre(2026, StatutExecution.EnCours));

        Assert.Equal(1, kpi.TotalMarches);
        Assert.Equal(1, kpi.MarchesEnCours);
    }

    [Fact]
    public async Task Dashboard_liste_les_echeances_proches_et_les_donnees_incompletes()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-001", delaiJours: 10);
        var dashboard = new DashboardService(db.UnitOfWork);

        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        await suivi.EmettreOrdreServiceAsync("M-001", DateOnly.FromDateTime(DateTime.Today));

        var echeances = await dashboard.ListerEcheancesAsync();
        var incomplets = await dashboard.ListerMarchesIncompletsAsync();

        Assert.Single(echeances);
        var marcheIncomplet = Assert.Single(incomplets);
        Assert.Contains("TEF absent", marcheIncomplet.Anomalies);
        Assert.Equal(2, marcheIncomplet.NombreAnomalies);
    }

    [Fact]
    public async Task Dashboard_regroupe_les_anomalies_par_marche()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-001");
        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        await suivi.EmettreOrdreServiceAsync("M-001", DateOnly.FromDateTime(DateTime.Today));
        var dashboard = new DashboardService(db.UnitOfWork);

        var incomplets = await dashboard.ListerMarchesIncompletsAsync();

        var marche = Assert.Single(incomplets);
        Assert.Equal("M-001", marche.RefMarche);
        Assert.Contains("TEF absent", marche.Anomalies);
        Assert.Contains("Réception provisoire absente", marche.Anomalies);
        Assert.Equal("TEF absent · Réception provisoire absente", marche.Anomalie);
    }

    [Fact]
    public async Task Dashboard_calcule_les_indicateurs_financiers_et_de_risque()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-LENT", delaiJours: 1);
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-NEW");
        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        await suivi.EmettreOrdreServiceAsync(
            "M-LENT", DateOnly.FromDateTime(DateTime.Today).AddDays(-10));
        var dashboard = new DashboardService(db.UnitOfWork);

        var kpi = await dashboard.ObtenirKpiAsync();

        Assert.Equal(2, kpi.TotalMarches);
        Assert.Equal(1_800_000m, kpi.MontantTotalHt);
        Assert.Equal(2_124_000m, kpi.MontantTotalTtc);
        Assert.Equal(1, kpi.MarchesEnRetard);
        Assert.Equal(900_000m, kpi.MontantEnRetard);
        Assert.Equal(1, kpi.MarchesNonDemarres);
        Assert.Equal(0m, kpi.TauxCloture);
    }

    [Fact]
    public async Task Dashboard_repartit_les_marches_par_statut()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-001");
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-002");
        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        await suivi.EmettreOrdreServiceAsync("M-001", DateOnly.FromDateTime(DateTime.Today));
        var dashboard = new DashboardService(db.UnitOfWork);

        var repartition = await dashboard.ListerRepartitionAsync();

        Assert.Equal(2, repartition.Count);
        Assert.Contains(repartition, ligne =>
            ligne.Statut == StatutExecution.EnCours && ligne.Nombre == 1 && ligne.Pourcentage == 50);
        Assert.Contains(repartition, ligne =>
            ligne.Statut == StatutExecution.NonDemarre && ligne.Nombre == 1 && ligne.Pourcentage == 50);
    }

    [Fact]
    public async Task Dashboard_respecte_la_fenetre_des_echeances()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-001", delaiJours: 45);
        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        await suivi.EmettreOrdreServiceAsync("M-001", DateOnly.FromDateTime(DateTime.Today));
        var dashboard = new DashboardService(db.UnitOfWork);

        var a30Jours = await dashboard.ListerEcheancesAsync(new DashboardFiltre(JoursEcheances: 30));
        var a60Jours = await dashboard.ListerEcheancesAsync(new DashboardFiltre(JoursEcheances: 60));

        Assert.Empty(a30Jours);
        Assert.Single(a60Jours);
    }

    [Fact]
    public async Task Dashboard_compte_les_echeances_des_trois_prochains_mois()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-001", delaiJours: 10);
        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        var aujourdHui = DateOnly.FromDateTime(DateTime.Today);
        await suivi.EmettreOrdreServiceAsync("M-001", aujourdHui);
        var dashboard = new DashboardService(db.UnitOfWork);

        var echeancesParMois = await dashboard.ListerEcheancesParMoisAsync();
        var dateLimite = aujourdHui.AddDays(10);
        var moisAttendu = new DateOnly(dateLimite.Year, dateLimite.Month, 1);

        Assert.Equal(3, echeancesParMois.Count);
        Assert.Equal(1, echeancesParMois.Sum(mois => mois.Nombre));
        Assert.Contains(echeancesParMois, mois => mois.Mois == moisAttendu && mois.Nombre == 1);
    }

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
