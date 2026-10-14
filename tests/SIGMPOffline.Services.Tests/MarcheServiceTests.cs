using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Marches;
using SIGMPOffline.Services.Tests.Infrastructure;

namespace SIGMPOffline.Services.Tests;

/// <summary>Tests du module Marché &amp; Bordereau (CRUD en-tête et éléments).</summary>
public class MarcheServiceTests
{
    [Fact]
    public async Task ModifierAsync_met_a_jour_l_entete()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new MarcheService(db.UnitOfWork);

        var marche = await service.ObtenirAsync("M-001");
        marche.ObjetMarche = "Objet modifié";
        marche.MontantHT = 1_500_000m;
        marche.MontantTTC = 1_770_000m;
        marche.DelaiExecutionJours = 45;

        await service.ModifierAsync(marche);

        var relu = await service.ObtenirAsync("M-001");
        Assert.Equal("Objet modifié", relu.ObjetMarche);
        Assert.Equal(1_500_000m, relu.MontantHT);
        Assert.Equal(1_770_000m, relu.MontantTTC);
        Assert.Equal(45, relu.DelaiExecutionJours);
    }

    [Fact]
    public async Task ModifierAsync_refuse_un_objet_vide()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new MarcheService(db.UnitOfWork);

        var marche = await service.ObtenirAsync("M-001");
        marche.ObjetMarche = "   ";

        await Assert.ThrowsAsync<DomainException>(() => service.ModifierAsync(marche));
    }

    [Fact]
    public async Task ModifierAsync_leve_si_absent()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new MarcheService(db.UnitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.ModifierAsync(new Marche { RefMarche = "ABSENT", ObjetMarche = "Objet" }));
    }

    [Fact]
    public async Task SupprimerAsync_retire_le_marche_et_son_bordereau()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new MarcheService(db.UnitOfWork);
        await service.AjouterElementAsync("M-001", new ElementMarche
        {
            CodeElement = "ART-1",
            Libelle = "Article 1",
            PrixUnitaire = 1_000m,
            Quantite = 2m,
            Unite = "u"
        });

        await service.SupprimerAsync("M-001");

        Assert.Empty(await service.ListerAsync());
        Assert.Equal(0, await db.Context.Set<ElementMarche>().CountAsync());
    }

    [Fact]
    public async Task SupprimerAsync_leve_si_absent()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new MarcheService(db.UnitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.SupprimerAsync("ABSENT"));
    }

    [Fact]
    public async Task ModifierElementAsync_met_a_jour_la_ligne()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new MarcheService(db.UnitOfWork);
        await service.AjouterElementAsync("M-001", new ElementMarche
        {
            CodeElement = "ART-1",
            Libelle = "Article 1",
            PrixUnitaire = 1_000m,
            Quantite = 2m,
            Unite = "u"
        });

        await service.ModifierElementAsync("M-001", new ElementMarche
        {
            CodeElement = "ART-1",
            Libelle = "Article 1 (révisé)",
            PrixUnitaire = 1_200m,
            Quantite = 3m,
            Unite = "u"
        });

        var marche = await service.ObtenirAsync("M-001");
        var relu = Assert.Single(marche.Elements);
        Assert.Equal("Article 1 (révisé)", relu.Libelle);
        Assert.Equal(1_200m, relu.PrixUnitaire);
        Assert.Equal(3m, relu.Quantite);
    }

    [Fact]
    public async Task ModifierElementAsync_leve_si_absent()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new MarcheService(db.UnitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.ModifierElementAsync(
            "M-001",
            new ElementMarche { CodeElement = "ABSENT", Libelle = "X", Quantite = 1m }));
    }

    [Fact]
    public async Task SupprimerElementAsync_retire_la_ligne()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new MarcheService(db.UnitOfWork);
        await service.AjouterElementAsync("M-001", new ElementMarche
        {
            CodeElement = "ART-1",
            Libelle = "Article 1",
            PrixUnitaire = 1_000m,
            Quantite = 2m
        });

        await service.SupprimerElementAsync("M-001", "ART-1");

        var marche = await service.ObtenirAsync("M-001");
        Assert.Empty(marche.Elements);
    }

    [Fact]
    public async Task RecalculerMontantsAsync_agrege_le_bordereau()
    {
        await using var db = await TestDatabase.CreateAsync();
        await GraphSeeder.SeedMarcheAsync(db);
        var service = new MarcheService(db.UnitOfWork);
        await service.AjouterElementAsync("M-001", new ElementMarche
        {
            CodeElement = "ART-1",
            Libelle = "Article 1",
            PrixUnitaire = 1_000m,
            Quantite = 2m
        });
        await service.AjouterElementAsync("M-001", new ElementMarche
        {
            CodeElement = "ART-2",
            Libelle = "Article 2",
            PrixUnitaire = 500m,
            Quantite = 4m
        });

        await service.RecalculerMontantsAsync("M-001", tauxTVA: 0.18m);

        var marche = await service.ObtenirAsync("M-001");
        Assert.Equal(4_000m, marche.MontantHT);
        Assert.Equal(4_720m, marche.MontantTTC);
    }
}
