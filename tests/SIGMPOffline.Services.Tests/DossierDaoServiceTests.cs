using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Domain.Enums;
using SIGMPOffline.Services.Dao;
using SIGMPOffline.Services.Ppm;
using SIGMPOffline.Services.Tests.Infrastructure;

namespace SIGMPOffline.Services.Tests;

/// <summary>Tests du module DAO &amp; Attribution (CRUD dossiers, candidats, acheteurs).</summary>
public class DossierDaoServiceTests
{
    /// <summary>Crée un PPM avec une ligne de détail et retourne le n° de la ligne.</summary>
    private static async Task<string> SeedLignePpmAsync(TestDatabase db, string suffixe = "1")
    {
        var ppm = new PpmService(db.UnitOfWork);
        var numPpm = $"PPM-{suffixe}";
        var numDetail = $"D-{suffixe}";

        await ppm.CreerAsync(new PpmEntete
        {
            NumPPM = numPpm,
            AnneeExercice = 2026,
            AutoriteContractante = "PRMP Test",
            DateElaboration = new DateOnly(2026, 1, 1)
        });

        await ppm.AjouterDetailAsync(numPpm, new PpmDetail
        {
            NumPPMDetail = numDetail,
            NatureMarche = NatureMarche.Travaux,
            ObjetProjet = "Objet de test",
            MontantEstimatif = 1_000_000m,
            ModePassation = ModePassation.AppelOffresOuvert
        });

        return numDetail;
    }

    private static DossierDao NouveauDossier(string numDetail, string reference = "DOS-001") => new()
    {
        RefDossier = reference,
        NumPPMDetail = numDetail,
        ObjetDossier = "DAO de test",
        CoutCahierCharges = 25_000m,
        MontantCaution = 50_000m
    };

    [Fact]
    public async Task CreerAsync_persiste_le_dossier()
    {
        await using var db = await TestDatabase.CreateAsync();
        var numDetail = await SeedLignePpmAsync(db);
        var service = new DossierDaoService(db.UnitOfWork);

        await service.CreerAsync(NouveauDossier(numDetail));

        var liste = await service.ListerAsync();
        var dossier = Assert.Single(liste);
        Assert.Equal("DOS-001", dossier.RefDossier);
        Assert.NotEqual(default, dossier.DateCreation);
    }

    [Fact]
    public async Task CreerAsync_refuse_une_reference_deja_existante()
    {
        await using var db = await TestDatabase.CreateAsync();
        var numDetail = await SeedLignePpmAsync(db);
        var service = new DossierDaoService(db.UnitOfWork);
        await service.CreerAsync(NouveauDossier(numDetail));

        await Assert.ThrowsAsync<DomainException>(
            () => service.CreerAsync(NouveauDossier(numDetail)));
    }

    [Fact]
    public async Task CreerAsync_leve_si_la_ligne_ppm_n_existe_pas()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new DossierDaoService(db.UnitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.CreerAsync(NouveauDossier("D-INEXISTANT")));
    }

    [Fact]
    public async Task ModifierAsync_met_a_jour_les_champs()
    {
        await using var db = await TestDatabase.CreateAsync();
        var numDetail = await SeedLignePpmAsync(db);
        var service = new DossierDaoService(db.UnitOfWork);
        await service.CreerAsync(NouveauDossier(numDetail));

        var dossier = await service.ObtenirAsync("DOS-001");
        dossier.ObjetDossier = "Objet modifié";
        dossier.CoutCahierCharges = 30_000m;

        await service.ModifierAsync(dossier);

        var relu = await service.ObtenirAsync("DOS-001");
        Assert.Equal("Objet modifié", relu.ObjetDossier);
        Assert.Equal(30_000m, relu.CoutCahierCharges);
    }

    [Fact]
    public async Task ModifierAsync_leve_si_absent()
    {
        await using var db = await TestDatabase.CreateAsync();
        var numDetail = await SeedLignePpmAsync(db);
        var service = new DossierDaoService(db.UnitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.ModifierAsync(NouveauDossier(numDetail, "ABSENT")));
    }

    [Fact]
    public async Task SupprimerAsync_retire_le_dossier()
    {
        await using var db = await TestDatabase.CreateAsync();
        var numDetail = await SeedLignePpmAsync(db);
        var service = new DossierDaoService(db.UnitOfWork);
        await service.CreerAsync(NouveauDossier(numDetail));

        await service.SupprimerAsync("DOS-001");

        Assert.Empty(await service.ListerAsync());
    }

    [Fact]
    public async Task SupprimerAsync_leve_si_absent()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new DossierDaoService(db.UnitOfWork);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.SupprimerAsync("ABSENT"));
    }

    [Fact]
    public async Task ModifierCandidatAsync_met_a_jour_l_offre()
    {
        await using var db = await TestDatabase.CreateAsync();
        var numDetail = await SeedLignePpmAsync(db);
        var service = new DossierDaoService(db.UnitOfWork);
        await service.CreerAsync(NouveauDossier(numDetail));

        var candidat = await service.AjouterCandidatAsync("DOS-001", new Candidat
        {
            NomEntreprise = "ACME",
            MontantOffre = 100m
        });

        await service.ModifierCandidatAsync("DOS-001", new Candidat
        {
            IdCandidat = candidat.IdCandidat,
            NomEntreprise = "ACME SARL",
            MontantOffre = 250m
        });

        var dossier = await service.ObtenirAsync("DOS-001");
        var relu = Assert.Single(dossier.Candidats);
        Assert.Equal("ACME SARL", relu.NomEntreprise);
        Assert.Equal(250m, relu.MontantOffre);
    }

    [Fact]
    public async Task SupprimerCandidatAsync_retire_le_candidat()
    {
        await using var db = await TestDatabase.CreateAsync();
        var numDetail = await SeedLignePpmAsync(db);
        var service = new DossierDaoService(db.UnitOfWork);
        await service.CreerAsync(NouveauDossier(numDetail));
        var candidat = await service.AjouterCandidatAsync("DOS-001", new Candidat { NomEntreprise = "ACME" });

        await service.SupprimerCandidatAsync("DOS-001", candidat.IdCandidat);

        var dossier = await service.ObtenirAsync("DOS-001");
        Assert.Empty(dossier.Candidats);
    }

    [Fact]
    public async Task DesignerRetenuAsync_ne_conserve_qu_un_seul_retenu()
    {
        await using var db = await TestDatabase.CreateAsync();
        var numDetail = await SeedLignePpmAsync(db);
        var service = new DossierDaoService(db.UnitOfWork);
        await service.CreerAsync(NouveauDossier(numDetail));

        var a = await service.AjouterCandidatAsync("DOS-001", new Candidat { NomEntreprise = "A" });
        var b = await service.AjouterCandidatAsync("DOS-001", new Candidat { NomEntreprise = "B" });

        await service.DesignerRetenuAsync("DOS-001", a.IdCandidat);
        await service.DesignerRetenuAsync("DOS-001", b.IdCandidat);

        var dossier = await service.ObtenirAsync("DOS-001");
        Assert.Single(dossier.Candidats, c => c.EstRetenu);
        Assert.True(dossier.Candidats.Single(c => c.IdCandidat == b.IdCandidat).EstRetenu);
    }

    [Fact]
    public async Task ModifierAcheteurAsync_met_a_jour_la_raison_sociale()
    {
        await using var db = await TestDatabase.CreateAsync();
        var numDetail = await SeedLignePpmAsync(db);
        var service = new DossierDaoService(db.UnitOfWork);
        await service.CreerAsync(NouveauDossier(numDetail));

        var acheteur = await service.AjouterAcheteurAsync("DOS-001", new AcheteurDao
        {
            RaisonSociale = "Fournisseur X",
            NumStat = "S1"
        });

        await service.ModifierAcheteurAsync("DOS-001", new AcheteurDao
        {
            IdAcheteur = acheteur.IdAcheteur,
            RaisonSociale = "Fournisseur Y",
            NumStat = "S2",
            DateAchat = acheteur.DateAchat
        });

        var dossier = await service.ObtenirAsync("DOS-001");
        var relu = Assert.Single(dossier.Acheteurs);
        Assert.Equal("Fournisseur Y", relu.RaisonSociale);
        Assert.Equal("S2", relu.NumStat);
    }

    [Fact]
    public async Task SupprimerAcheteurAsync_retire_l_acheteur()
    {
        await using var db = await TestDatabase.CreateAsync();
        var numDetail = await SeedLignePpmAsync(db);
        var service = new DossierDaoService(db.UnitOfWork);
        await service.CreerAsync(NouveauDossier(numDetail));
        var acheteur = await service.AjouterAcheteurAsync("DOS-001", new AcheteurDao { RaisonSociale = "X" });

        await service.SupprimerAcheteurAsync("DOS-001", acheteur.IdAcheteur);

        var dossier = await service.ObtenirAsync("DOS-001");
        Assert.Empty(dossier.Acheteurs);
    }
}
