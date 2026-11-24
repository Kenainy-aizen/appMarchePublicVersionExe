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
        Assert.Equal(1, marcheIncomplet.NombreAnomalies);
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
        Assert.DoesNotContain("Réception provisoire absente", marche.Anomalies);
        Assert.Equal("TEF absent", marche.Anomalie);
    }

    [Fact]
    public async Task Dashboard_calcule_les_indicateurs_de_suivi_et_de_risque()
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
        Assert.Equal(1, kpi.MarchesEnRetard);
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
    public async Task Kpi_compte_les_marches_et_leur_statut()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-001");
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-002");
        var dashboard = new DashboardService(db.UnitOfWork);

        var kpi = await dashboard.ObtenirKpiAsync();

        Assert.Equal(2, kpi.TotalMarches);
        Assert.Equal(2, kpi.MarchesNonDemarres);
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

    [Fact]
    public async Task Un_marche_resilie_ou_annule_n_est_jamais_en_retard()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-RES", delaiJours: 1);
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-ANN", delaiJours: 1);

        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        var aujourdhui = DateOnly.FromDateTime(DateTime.Today);
        await suivi.EmettreOrdreServiceAsync("M-RES", aujourdhui.AddDays(-10));
        await suivi.EmettreOrdreServiceAsync("M-ANN", aujourdhui.AddDays(-10));
        DefinirStatut(db, "M-RES", StatutExecution.Resilie);
        DefinirStatut(db, "M-ANN", StatutExecution.Annule);

        var dashboard = new DashboardService(db.UnitOfWork);
        var alertes = await dashboard.ListerAlertesAsync();
        var kpi = await dashboard.ObtenirKpiAsync();

        Assert.Empty(alertes);
        Assert.Equal(0, kpi.MarchesEnRetard);
    }

    [Fact]
    public async Task Un_marche_termine_n_apparait_pas_dans_les_echeances()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-RES", delaiJours: 10);
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-ANN", delaiJours: 10);
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-OK", delaiJours: 10);

        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        var aujourdhui = DateOnly.FromDateTime(DateTime.Today);
        await suivi.EmettreOrdreServiceAsync("M-RES", aujourdhui);
        await suivi.EmettreOrdreServiceAsync("M-ANN", aujourdhui);
        await suivi.EmettreOrdreServiceAsync("M-OK", aujourdhui);
        DefinirStatut(db, "M-RES", StatutExecution.Resilie);
        DefinirStatut(db, "M-ANN", StatutExecution.Annule);

        var dashboard = new DashboardService(db.UnitOfWork);
        var echeances = await dashboard.ListerEcheancesAsync(new DashboardFiltre(JoursEcheances: 30));
        var parMois = await dashboard.ListerEcheancesParMoisAsync();

        var echeance = Assert.Single(echeances);
        Assert.Equal("M-OK", echeance.RefMarche);
        Assert.Equal(1, parMois.Sum(mois => mois.Nombre));
    }

    [Fact]
    public async Task Un_marche_non_demarre_ou_termine_n_est_pas_signale_incomplet()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-NEW");
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-RES");
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-ANN");

        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        var aujourdhui = DateOnly.FromDateTime(DateTime.Today);
        await suivi.EmettreOrdreServiceAsync("M-RES", aujourdhui);
        await suivi.EmettreOrdreServiceAsync("M-ANN", aujourdhui);
        DefinirStatut(db, "M-RES", StatutExecution.Resilie);
        DefinirStatut(db, "M-ANN", StatutExecution.Annule);

        var dashboard = new DashboardService(db.UnitOfWork);
        var incomplets = await dashboard.ListerMarchesIncompletsAsync();

        Assert.Empty(incomplets);
    }

    [Fact]
    public async Task Les_receptions_ne_sont_exigees_qu_aux_statuts_avances()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-ENC");
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-RD");
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-CLO");

        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        var aujourdhui = DateOnly.FromDateTime(DateTime.Today);
        foreach (var reference in new[] { "M-ENC", "M-RD", "M-CLO" })
        {
            await suivi.EmettreOrdreServiceAsync(reference, aujourdhui);
            await suivi.EnregistrerTefAsync(reference, $"TEF-{reference}");
        }

        DefinirStatut(db, "M-RD", StatutExecution.ReceptionDefinitive);
        DefinirStatut(db, "M-CLO", StatutExecution.Cloture);

        var dashboard = new DashboardService(db.UnitOfWork);
        var incomplets = await dashboard.ListerMarchesIncompletsAsync();

        Assert.DoesNotContain(incomplets, marche => marche.RefMarche == "M-ENC");

        var receptionDefinitive = Assert.Single(incomplets, marche => marche.RefMarche == "M-RD");
        Assert.Equal(["Réception provisoire absente"], receptionDefinitive.Anomalies);

        var cloture = Assert.Single(incomplets, marche => marche.RefMarche == "M-CLO");
        Assert.Contains("Réception provisoire absente", cloture.Anomalies);
        Assert.Contains("Réception définitive absente", cloture.Anomalies);
    }

    [Fact]
    public async Task Kpi_compte_les_marches_en_reception()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-RP");
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-RD");
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-ENC");

        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        var aujourdhui = DateOnly.FromDateTime(DateTime.Today);
        foreach (var reference in new[] { "M-RP", "M-RD", "M-ENC" })
        {
            await suivi.EmettreOrdreServiceAsync(reference, aujourdhui);
        }

        DefinirStatut(db, "M-RP", StatutExecution.ReceptionProvisoire);
        DefinirStatut(db, "M-RD", StatutExecution.ReceptionDefinitive);

        var dashboard = new DashboardService(db.UnitOfWork);
        var kpi = await dashboard.ObtenirKpiAsync();

        Assert.Equal(2, kpi.MarchesEnReception);
        Assert.Equal(1, kpi.MarchesEnCours);
    }

    [Fact]
    public async Task Le_tableau_de_bord_instantane_regroupe_toutes_les_sections()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, refMarche: "M-001", delaiJours: 10);

        var suivi = new SIGMPOffline.Services.Suivi.SuiviExecutionService(db.UnitOfWork);
        await suivi.EmettreOrdreServiceAsync("M-001", DateOnly.FromDateTime(DateTime.Today));

        var dashboard = new DashboardService(db.UnitOfWork);
        var instantane = await dashboard.ObtenirTableauDeBordAsync();

        Assert.Equal(1, instantane.Kpi.TotalMarches);
        Assert.Single(instantane.Echeances);
        Assert.Single(instantane.Repartition);
        Assert.Equal(3, instantane.EcheancesParMois.Count);
        Assert.Contains(2026, instantane.Exercices);
        Assert.Single(instantane.Incomplets);
    }

    /// <summary>Force le statut d'exécution d'un marché déjà semé.</summary>
    private static void DefinirStatut(TestDatabase db, string refMarche, StatutExecution statut)
    {
        var marche = db.Context.Marches.Single(marche => marche.RefMarche == refMarche);
        marche.StatutExecution = statut;
        db.Context.SaveChanges();
    }
}
