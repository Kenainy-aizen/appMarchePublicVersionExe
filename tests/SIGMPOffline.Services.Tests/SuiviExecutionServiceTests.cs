using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Domain.Enums;
using SIGMPOffline.Services.Suivi;
using SIGMPOffline.Services.Tests.Infrastructure;

namespace SIGMPOffline.Services.Tests;

/// <summary>Tests du module Suivi d'Exécution (cœur métier).</summary>
public class SuiviExecutionServiceTests
{
    [Fact]
    public async Task EmettreOrdreService_renseigne_la_date_et_passe_en_cours()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new SuiviExecutionService(db.UnitOfWork);

        var os = new DateOnly(2026, 2, 1);
        await service.EmettreOrdreServiceAsync("M-001", os);

        var situation = await service.ObtenirSituationAsync("M-001");
        Assert.Equal(os, situation.DateOS);
        Assert.Equal(StatutExecution.EnCours, situation.Statut);
        Assert.Equal(new DateOnly(2026, 3, 3), situation.DateLimiteExecution); // 01/02 + 30 j
    }

    [Fact]
    public async Task Un_avenant_prolonge_la_date_limite()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new SuiviExecutionService(db.UnitOfWork);

        await service.EmettreOrdreServiceAsync("M-001", new DateOnly(2026, 2, 1));
        await service.AjouterAvenantAsync("M-001", new Avenant
        {
            NumAvenant = "AV-01",
            Motif = "Travaux supplémentaires",
            ProlongationDelaiJours = 15,
            VariationMontant = 50_000m
        });

        var situation = await service.ObtenirSituationAsync("M-001");
        Assert.Equal(new DateOnly(2026, 3, 18), situation.DateLimiteExecution); // +15 j
    }

    [Fact]
    public async Task Un_marche_non_cloture_avec_delai_depasse_est_en_retard()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db, delaiJours: 1);
        var service = new SuiviExecutionService(db.UnitOfWork);

        // OS émis il y a 10 jours pour un délai d'1 jour => en retard.
        var os = DateOnly.FromDateTime(DateTime.Today).AddDays(-10);
        await service.EmettreOrdreServiceAsync("M-001", os);

        var situation = await service.ObtenirSituationAsync("M-001");
        Assert.True(situation.EstEnRetard);
        Assert.True(situation.JoursRestants < 0);
    }

    [Fact]
    public async Task Cloturer_marque_le_marche_comme_cloture()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new SuiviExecutionService(db.UnitOfWork);
        await service.EmettreOrdreServiceAsync("M-001", new DateOnly(2026, 2, 1));

        await service.CloturerAsync("M-001", new DateOnly(2026, 6, 1));

        var situation = await service.ObtenirSituationAsync("M-001");
        Assert.True(situation.EstCloture);
        Assert.Equal(StatutExecution.Cloture, situation.Statut);
        Assert.False(situation.EstEnRetard);
    }

    [Fact]
    public async Task Ajouter_avenant_refuse_un_marche_cloture()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new SuiviExecutionService(db.UnitOfWork);
        await service.CloturerAsync("M-001", new DateOnly(2026, 6, 1));

        await Assert.ThrowsAsync<DomainException>(() =>
            service.AjouterAvenantAsync("M-001", new Avenant { NumAvenant = "AV-02" }));
    }

    [Fact]
    public async Task EnregistrerTef_puis_reception_definitive_suivent_le_statut()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new SuiviExecutionService(db.UnitOfWork);

        await service.EnregistrerTefAsync("M-001", "TEF-2026-001");
        await service.EnregistrerReceptionProvisoireAsync("M-001", new DateOnly(2026, 5, 1));
        await service.EnregistrerReceptionDefinitiveAsync("M-001", new DateOnly(2026, 6, 1));

        var situation = await service.ObtenirSituationAsync("M-001");
        Assert.Equal(StatutExecution.ReceptionDefinitive, situation.Statut);
    }

    [Fact]
    public async Task ObtenirSituationAsync_expose_le_tef()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new SuiviExecutionService(db.UnitOfWork);

        await service.EnregistrerTefAsync("M-001", "TEF-2026-042");

        var situation = await service.ObtenirSituationAsync("M-001");
        Assert.Equal("TEF-2026-042", situation.RefTef);
    }

    [Fact]
    public async Task ModifierAvenantAsync_met_a_jour_les_champs()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new SuiviExecutionService(db.UnitOfWork);
        var avenant = await service.AjouterAvenantAsync("M-001", new Avenant
        {
            NumAvenant = "AV-01",
            Motif = "Initial",
            VariationMontant = 10_000m,
            ProlongationDelaiJours = 5
        });

        avenant.Motif = "Révisé";
        avenant.VariationMontant = 20_000m;
        avenant.ProlongationDelaiJours = 10;
        await service.ModifierAvenantAsync("M-001", avenant);

        var marche = await db.UnitOfWork.Marches.GetWithExecutionAsync("M-001");
        Assert.NotNull(marche);
        var relu = Assert.Single(marche!.Avenants);
        Assert.Equal("Révisé", relu.Motif);
        Assert.Equal(20_000m, relu.VariationMontant);
        Assert.Equal(10, relu.ProlongationDelaiJours);
    }

    [Fact]
    public async Task ModifierAvenantAsync_leve_si_absent()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new SuiviExecutionService(db.UnitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.ModifierAvenantAsync(
            "M-001",
            new Avenant { IdAvenant = 999, NumAvenant = "AV-X" }));
    }

    [Fact]
    public async Task SupprimerAvenantAsync_retire_l_avenant()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new SuiviExecutionService(db.UnitOfWork);
        var avenant = await service.AjouterAvenantAsync("M-001", new Avenant { NumAvenant = "AV-01" });

        await service.SupprimerAvenantAsync("M-001", avenant.IdAvenant);

        var marche = await db.UnitOfWork.Marches.GetWithExecutionAsync("M-001");
        Assert.NotNull(marche);
        Assert.Empty(marche!.Avenants);
    }

    [Fact]
    public async Task Modifier_ou_supprimer_un_avenant_est_refuse_sur_un_marche_cloture()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new SuiviExecutionService(db.UnitOfWork);
        var avenant = await service.AjouterAvenantAsync("M-001", new Avenant { NumAvenant = "AV-01" });
        await service.CloturerAsync("M-001", new DateOnly(2026, 6, 1));

        await Assert.ThrowsAsync<DomainException>(() => service.ModifierAvenantAsync("M-001", avenant));
        await Assert.ThrowsAsync<DomainException>(() => service.SupprimerAvenantAsync("M-001", avenant.IdAvenant));
    }
}
