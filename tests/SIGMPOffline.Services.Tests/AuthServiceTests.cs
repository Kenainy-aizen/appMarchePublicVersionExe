using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data.Database;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Services.Auth;
using SIGMPOffline.Services.Ppm;
using SIGMPOffline.Services.Tests.Infrastructure;

namespace SIGMPOffline.Services.Tests;

/// <summary>
/// Tests de l'authentification et de la gestion multi-provinces :
/// premier lancement, connexion, isolation des données et mots de passe.
/// </summary>
public class AuthServiceTests
{
    private static AuthService Creer(TestDatabase db)
        => new(db.UnitOfWork, db.ProvinceAccessor, db.Context);

    [Fact]
    public async Task EstPremierLancementAsync_vrai_sur_base_vierge()
    {
        await using var db = await TestDatabase.CreateAsync();

        Assert.True(await Creer(db).EstPremierLancementAsync());
    }

    [Fact]
    public async Task InitialiserAsync_cree_la_configuration_et_ouvre_la_province()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);

        await auth.InitialiserAsync("PRMP Jean Dupont", "Province d'Antananarivo", "secret");

        Assert.False(await auth.EstPremierLancementAsync());
        Assert.True(auth.EstAuthentifie);
        Assert.Equal("PRMP Jean Dupont", auth.Configuration!.NomProprietairePRMP);
        Assert.Equal("Province d'Antananarivo", auth.ProvinceActive!.NomProvince);
        Assert.Equal(auth.ProvinceActive.IdProvince, db.ProvinceAccessor.IdProvince);
    }

    [Fact]
    public async Task InitialiserAsync_seme_des_donnees_rattachees_a_la_province()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);

        await auth.InitialiserAsync("PRMP", "Province A", "secret");

        var ppm = await new PpmService(db.UnitOfWork).ListerAsync();
        Assert.NotEmpty(ppm);
        Assert.All(ppm, p => Assert.Equal(auth.ProvinceActive!.IdProvince, p.IdProvince));
    }

    [Fact]
    public async Task ConnecterAsync_refuse_un_mauvais_mot_de_passe()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await auth.InitialiserAsync("PRMP", "Province A", "secret");
        var idProvince = auth.ProvinceActive!.IdProvince;

        await auth.DeconnecterAsync();

        await Assert.ThrowsAsync<DomainException>(() => auth.ConnecterAsync(idProvince, "mauvais"));
        Assert.False(auth.EstAuthentifie);
    }

    [Fact]
    public async Task InitialiserAsync_adopte_les_donnees_existantes_sans_province()
    {
        await using var db = await TestDatabase.CreateAsync();

        // Données « héritées » créées avant le multi-provinces (aucune province active).
        await DbSeeder.SeedAsync(db.Context);
        Assert.NotEmpty(await db.Context.PpmEntetes.ToListAsync());
        Assert.All(
            await db.Context.PpmEntetes.ToListAsync(),
            p => Assert.Null(p.IdProvince));

        var auth = Creer(db);
        await auth.InitialiserAsync("PRMP", "Province A", "secret");

        var ppm = await new PpmService(db.UnitOfWork).ListerAsync();
        Assert.NotEmpty(ppm);
        Assert.All(ppm, p => Assert.Equal(auth.ProvinceActive!.IdProvince, p.IdProvince));

        // Pas de doublon : le jeu de démonstration n'a pas été dupliqué.
        Assert.Single(await db.Context.PpmEntetes.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task CreerProvinceAsync_refuse_un_nom_deja_utilise()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await auth.InitialiserAsync("PRMP", "Province A", "secret");

        await Assert.ThrowsAsync<DomainException>(() => auth.CreerProvinceAsync("Province A", "autre"));
    }

    [Fact]
    public async Task Les_donnees_sont_isolees_entre_provinces()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        var ppmService = new PpmService(db.UnitOfWork);

        // Province A : contient les données de démonstration.
        await auth.InitialiserAsync("PRMP", "Province A", "secret-A");
        Assert.NotEmpty(await ppmService.ListerAsync());

        // Province B : créée à vide.
        var provinceB = await auth.CreerProvinceAsync("Province B", "secret-B");
        await auth.ConnecterAsync(provinceB.IdProvince, "secret-B");
        Assert.Empty(await ppmService.ListerAsync());

        // Retour sur la province A : ses données réapparaissent.
        await auth.ConnecterAsync(
            (await auth.ListerProvincesAsync()).Single(p => p.NomProvince == "Province A").IdProvince,
            "secret-A");
        Assert.NotEmpty(await ppmService.ListerAsync());
    }

    [Fact]
    public async Task ChangerMotDePasseAsync_exige_l_ancien_et_applique_le_nouveau()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await auth.InitialiserAsync("PRMP", "Province A", "ancien");
        var idProvince = auth.ProvinceActive!.IdProvince;

        await Assert.ThrowsAsync<DomainException>(
            () => auth.ChangerMotDePasseAsync(idProvince, "faux", "nouveau"));

        await auth.ChangerMotDePasseAsync(idProvince, "ancien", "nouveau");
        await auth.DeconnecterAsync();

        await Assert.ThrowsAsync<DomainException>(() => auth.ConnecterAsync(idProvince, "ancien"));

        var province = await auth.ConnecterAsync(idProvince, "nouveau");
        Assert.True(auth.EstAuthentifie);
        Assert.Equal(idProvince, province.IdProvince);
    }

    [Fact]
    public async Task DeconnecterAsync_ferme_la_session()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await auth.InitialiserAsync("PRMP", "Province A", "secret");

        await auth.DeconnecterAsync();

        Assert.False(auth.EstAuthentifie);
        Assert.Null(auth.ProvinceActive);
        Assert.Null(db.ProvinceAccessor.IdProvince);
    }
}
