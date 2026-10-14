using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Domain.Enums;
using SIGMPOffline.Services.Ppm;
using SIGMPOffline.Services.Tests.Infrastructure;

namespace SIGMPOffline.Services.Tests;

/// <summary>Tests du module PPM.</summary>
public class PpmServiceTests
{
    private static PpmEntete NouvelEntete(string num = "PPM-2026") => new()
    {
        NumPPM = num,
        AnneeExercice = 2026,
        AutoriteContractante = "PRMP Test",
        DateElaboration = new DateOnly(2026, 1, 1)
    };

    [Fact]
    public async Task CreerAsync_persiste_l_entete()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new PpmService(db.UnitOfWork);

        await service.CreerAsync(NouvelEntete());

        var liste = await service.ListerAsync(2026);
        Assert.Single(liste);
        Assert.NotNull(liste[0].DateDerniereMAJ);
    }

    [Fact]
    public async Task CreerAsync_refuse_un_numero_deja_existant()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new PpmService(db.UnitOfWork);

        await service.CreerAsync(NouvelEntete());

        await Assert.ThrowsAsync<DomainException>(() => service.CreerAsync(NouvelEntete()));
    }

    [Fact]
    public async Task ObtenirAsync_leve_si_absent()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new PpmService(db.UnitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.ObtenirAsync("INEXISTANT"));
    }

    [Fact]
    public async Task AjouterDetailAsync_rattache_la_ligne_au_PPM()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new PpmService(db.UnitOfWork);
        await service.CreerAsync(NouvelEntete());

        await service.AjouterDetailAsync("PPM-2026", new PpmDetail
        {
            NumPPMDetail = "D-001",
            NatureMarche = NatureMarche.Fournitures,
            ObjetProjet = "Achat véhicules",
            MontantEstimatif = 250_000m,
            ModePassation = ModePassation.DemandeCotation
        });

        var entete = await service.ObtenirAsync("PPM-2026");
        Assert.Single(entete.Details);
        Assert.Equal(250_000m, entete.Details.First().MontantEstimatif);
    }

    [Fact]
    public async Task ReviserAsync_met_a_jour_le_numero_de_revision()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new PpmService(db.UnitOfWork);
        await service.CreerAsync(NouvelEntete());

        await service.ReviserAsync("PPM-2026", "MAJ-02");

        var entete = await service.ObtenirAsync("PPM-2026");
        Assert.Equal("MAJ-02", entete.NumDerniereMAJ);
    }

    [Fact]
    public async Task ModifierAsync_met_a_jour_les_champs()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new PpmService(db.UnitOfWork);
        await service.CreerAsync(NouvelEntete());

        var entete = await service.ObtenirAsync("PPM-2026");
        entete.AnneeExercice = 2027;
        entete.AutoriteContractante = "PRMP Modifiée";

        await service.ModifierAsync(entete);

        var relu = await service.ObtenirAsync("PPM-2026");
        Assert.Equal(2027, relu.AnneeExercice);
        Assert.Equal("PRMP Modifiée", relu.AutoriteContractante);
    }

    [Fact]
    public async Task ModifierAsync_leve_si_absent()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new PpmService(db.UnitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.ModifierAsync(NouvelEntete("INEXISTANT")));
    }

    [Fact]
    public async Task ModifierAsync_apres_ObtenirAsync_ne_provoque_pas_de_conflit_de_tracking()
    {
        // Reproduit le scénario de l'IHM : l'entité affichée est suivie par EF
        // (ObtenirAsync), puis la modification part d'une instance non suivie (liste).
        await using var db = await TestDatabase.CreateAsync();
        var service = new PpmService(db.UnitOfWork);
        await service.CreerAsync(NouvelEntete());

        _ = await service.ObtenirAsync("PPM-2026");

        var liste = await service.ListerAsync(2026);
        var copieNonSuivie = liste[0];
        copieNonSuivie.AutoriteContractante = "Nouvelle autorité";

        await service.ModifierAsync(copieNonSuivie);

        var relu = await service.ObtenirAsync("PPM-2026");
        Assert.Equal("Nouvelle autorité", relu.AutoriteContractante);
    }

    [Fact]
    public async Task ModifierDetailAsync_met_a_jour_la_ligne()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new PpmService(db.UnitOfWork);
        await service.CreerAsync(NouvelEntete());
        await service.AjouterDetailAsync("PPM-2026", new PpmDetail
        {
            NumPPMDetail = "D-001",
            ObjetProjet = "Ancien objet",
            MontantEstimatif = 100m,
            NatureMarche = NatureMarche.Services,
            ModePassation = ModePassation.ConsultationRestreinte
        });

        await service.ModifierDetailAsync("PPM-2026", new PpmDetail
        {
            NumPPMDetail = "D-001",
            ObjetProjet = "Nouvel objet",
            MontantEstimatif = 500m,
            NatureMarche = NatureMarche.Travaux,
            ModePassation = ModePassation.AppelOffresOuvert,
            SourceFinancement = "Bailleur"
        });

        var entete = await service.ObtenirAsync("PPM-2026");
        var ligne = Assert.Single(entete.Details);
        Assert.Equal("Nouvel objet", ligne.ObjetProjet);
        Assert.Equal(500m, ligne.MontantEstimatif);
        Assert.Equal("Bailleur", ligne.SourceFinancement);
    }

    [Fact]
    public async Task ModifierDetailAsync_leve_si_ligne_absente()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new PpmService(db.UnitOfWork);
        await service.CreerAsync(NouvelEntete());

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.ModifierDetailAsync("PPM-2026", new PpmDetail { NumPPMDetail = "ABSENT" }));
    }

    [Fact]
    public async Task SupprimerDetailAsync_retire_la_ligne()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new PpmService(db.UnitOfWork);
        await service.CreerAsync(NouvelEntete());
        await service.AjouterDetailAsync("PPM-2026", new PpmDetail
        {
            NumPPMDetail = "D-001",
            ObjetProjet = "À supprimer",
            MontantEstimatif = 100m
        });

        await service.SupprimerDetailAsync("PPM-2026", "D-001");

        var entete = await service.ObtenirAsync("PPM-2026");
        Assert.Empty(entete.Details);
    }

    [Fact]
    public async Task SupprimerDetailAsync_leve_si_ligne_absente()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new PpmService(db.UnitOfWork);
        await service.CreerAsync(NouvelEntete());

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.SupprimerDetailAsync("PPM-2026", "ABSENT"));
    }
}
