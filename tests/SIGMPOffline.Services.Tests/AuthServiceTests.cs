using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Services.Auth;
using SIGMPOffline.Services.Ppm;
using SIGMPOffline.Services.Tests.Infrastructure;

namespace SIGMPOffline.Services.Tests;

/// <summary>Authentification par compte et contrôle des permissions par province.</summary>
public class AuthServiceTests
{
    private static AuthService Creer(TestDatabase db) => new(db.ScopeRunner, db.ProvinceAccessor);

    private static Task InitialiserAsync(AuthService auth)
        => auth.InitialiserAsync("PRMP Jean", "Province A", "admin-secret");

    [Fact]
    public async Task InitialiserAsync_cree_le_compte_admin_avec_le_nom_prmp_comme_identifiant()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);

        Assert.True(await auth.EstPremierLancementAsync());
        await InitialiserAsync(auth);

        Assert.False(await auth.EstPremierLancementAsync());
        Assert.True(auth.EstAuthentifie);
        Assert.True(auth.EstAdministrateur);
        Assert.True(auth.PeutModifier);
        Assert.Equal("PRMP Jean", auth.UtilisateurActif!.NomUtilisateur);
        Assert.Equal("PRMP Jean", auth.Configuration!.NomProprietairePRMP);
        Assert.Equal("Province A", auth.ProvinceActive!.NomProvince);
        Assert.Equal(auth.ProvinceActive.IdProvince, db.ProvinceAccessor.IdProvince);
    }

    [Fact]
    public async Task L_admin_peut_selectionner_toutes_les_provinces_sans_mot_de_passe_province()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await InitialiserAsync(auth);
        var provinceB = await auth.CreerProvinceAsync("Province B");

        var connectee = await auth.ChangerProvinceAsync(provinceB.IdProvince);

        Assert.Equal(provinceB.IdProvince, connectee.IdProvince);
        Assert.True(auth.EstAdministrateur);
        Assert.True(auth.PeutModifier);
        Assert.Equal(new[] { "Province A", "Province B" },
            (await auth.ListerProvincesAutoriseesAsync()).Select(x => x.NomProvince).ToArray());
    }

    [Fact]
    public async Task Un_compte_ne_peut_ouvrir_que_les_provinces_et_les_droits_attribues()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await InitialiserAsync(auth);
        var provinceA = auth.ProvinceActive!;
        var provinceB = await auth.CreerProvinceAsync("Province B");
        var provinceC = await auth.CreerProvinceAsync("Province C");
        var compte = await auth.CreerUtilisateurAsync(
            "Agent DAO",
            "agent-secret",
            new Dictionary<int, bool>
            {
                [provinceA.IdProvince] = false,
                [provinceB.IdProvince] = true
            });

        await auth.DeconnecterAsync();
        await Assert.ThrowsAsync<DomainException>(
            () => auth.ConnecterAsync("Agent DAO", "agent-secret", 999));
        await Assert.ThrowsAsync<DomainException>(
            () => auth.ConnecterAsync("Agent DAO", "agent-secret", provinceC.IdProvince));

        await auth.ConnecterAsync("agent dao", "agent-secret", provinceA.IdProvince);
        Assert.False(auth.EstAdministrateur);
        Assert.False(auth.PeutModifier);
        Assert.Equal(new[] { provinceA.IdProvince, provinceB.IdProvince },
            (await auth.ListerProvincesAutoriseesAsync()).Select(x => x.IdProvince));

        await auth.ChangerProvinceAsync(provinceB.IdProvince);
        Assert.True(auth.PeutModifier);
        Assert.Equal(provinceB.IdProvince, auth.ProvinceActive!.IdProvince);

        await auth.DeconnecterAsync();
        await Assert.ThrowsAsync<DomainException>(
            () => auth.ConnecterAsync("Agent DAO", "mauvais", provinceB.IdProvince));
        Assert.False(auth.EstAuthentifie);
        Assert.NotEqual(0, compte.IdUtilisateur);
    }

    [Fact]
    public async Task CreerUtilisateurAsync_exige_au_moins_une_province_existante()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await InitialiserAsync(auth);

        await Assert.ThrowsAsync<DomainException>(() => auth.CreerUtilisateurAsync(
            "Sans province", "secret", new Dictionary<int, bool>()));
        await Assert.ThrowsAsync<DomainException>(() => auth.CreerUtilisateurAsync(
            "Province inconnue", "secret", new Dictionary<int, bool> { [999] = false }));
    }

    [Fact]
    public async Task Seul_l_admin_peut_creer_modifier_et_supprimer_des_comptes()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await InitialiserAsync(auth);
        var idProvince = auth.ProvinceActive!.IdProvince;
        var compte = await auth.CreerUtilisateurAsync(
            "Agent", "secret1", new Dictionary<int, bool> { [idProvince] = false });

        await auth.DeconnecterAsync();
        await auth.ConnecterAsync("Agent", "secret1", idProvince);
        await Assert.ThrowsAsync<DomainException>(() => auth.ListerUtilisateursAsync());
        await Assert.ThrowsAsync<DomainException>(() => auth.CreerUtilisateurAsync(
            "Interdit", "secret2", new Dictionary<int, bool> { [idProvince] = true }));

        await auth.DeconnecterAsync();
        await auth.ConnecterAsync("PRMP Jean", "admin-secret", idProvince);
        await auth.ModifierUtilisateurAsync(
            compte.IdUtilisateur,
            "Agent modifié",
            "secret2",
            new Dictionary<int, bool> { [idProvince] = true });

        await auth.DeconnecterAsync();
        await Assert.ThrowsAsync<DomainException>(
            () => auth.ConnecterAsync("Agent", "secret1", idProvince));
        await auth.ConnecterAsync("Agent modifié", "secret2", idProvince);
        Assert.True(auth.PeutModifier);

        await auth.DeconnecterAsync();
        await auth.ConnecterAsync("PRMP Jean", "admin-secret", idProvince);
        await auth.SupprimerUtilisateurAsync(compte.IdUtilisateur);
        Assert.DoesNotContain(await auth.ListerUtilisateursAsync(), x => x.IdUtilisateur == compte.IdUtilisateur);
    }

    [Fact]
    public async Task Un_compte_ne_peut_pas_se_donner_des_droits_ou_supprimer_l_admin()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await InitialiserAsync(auth);
        var adminId = auth.UtilisateurActif!.IdUtilisateur;
        var provinceId = auth.ProvinceActive!.IdProvince;
        var user = await auth.CreerUtilisateurAsync(
            "Agent", "secret", new Dictionary<int, bool> { [provinceId] = false });

        await Assert.ThrowsAsync<DomainException>(() => auth.SupprimerUtilisateurAsync(adminId));

        await auth.DeconnecterAsync();
        await auth.ConnecterAsync("Agent", "secret", provinceId);
        await Assert.ThrowsAsync<DomainException>(() => auth.ModifierUtilisateurAsync(
            user.IdUtilisateur,
            "Agent",
            null,
            new Dictionary<int, bool> { [provinceId] = true }));
    }

    [Fact]
    public async Task Les_donnees_de_donnees_restent_isolees_entre_provinces_pour_un_compte_editeur()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await InitialiserAsync(auth);
        var provinceA = auth.ProvinceActive!;
        var ppmService = new PpmService(db.UnitOfWork);
        Assert.NotEmpty(await ppmService.ListerAsync());

        var provinceB = await auth.CreerProvinceAsync("Province B");
        await auth.CreerUtilisateurAsync(
            "Agent B",
            "agent-secret",
            new Dictionary<int, bool> { [provinceB.IdProvince] = true });

        await auth.DeconnecterAsync();
        await auth.ConnecterAsync("Agent B", "agent-secret", provinceB.IdProvince);
        Assert.Empty(await ppmService.ListerAsync());

        await auth.DeconnecterAsync();
        await auth.ConnecterAsync("PRMP Jean", "admin-secret", provinceA.IdProvince);
        Assert.NotEmpty(await ppmService.ListerAsync());
    }

    [Fact]
    public async Task ModifierNomProprietaireAsync_met_a_jour_aussi_identifiant_admin()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await InitialiserAsync(auth);
        var provinceId = auth.ProvinceActive!.IdProvince;

        await auth.ModifierNomProprietaireAsync("PRMP Marie");
        Assert.Equal("PRMP Marie", auth.UtilisateurActif!.NomUtilisateur);

        await auth.DeconnecterAsync();
        await Assert.ThrowsAsync<DomainException>(
            () => auth.ConnecterAsync("PRMP Jean", "admin-secret", provinceId));
        await auth.ConnecterAsync("PRMP Marie", "admin-secret", provinceId);
        Assert.True(auth.EstAdministrateur);
    }

    [Fact]
    public async Task ChangerMotDePasseAsync_verifie_l_ancien_mot_de_passe_du_compte()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await InitialiserAsync(auth);
        var idProvince = auth.ProvinceActive!.IdProvince;

        await Assert.ThrowsAsync<DomainException>(
            () => auth.ChangerMotDePasseAsync("faux", "nouveau-secret"));

        await auth.ChangerMotDePasseAsync("admin-secret", "nouveau-secret");
        await auth.DeconnecterAsync();
        await Assert.ThrowsAsync<DomainException>(
            () => auth.ConnecterAsync("PRMP Jean", "admin-secret", idProvince));
        await auth.ConnecterAsync("PRMP Jean", "nouveau-secret", idProvince);
        Assert.True(auth.EstAdministrateur);
    }

    [Fact]
    public async Task DeconnecterAsync_ferme_la_session_et_efface_la_province_active()
    {
        await using var db = await TestDatabase.CreateAsync();
        var auth = Creer(db);
        await InitialiserAsync(auth);

        await auth.DeconnecterAsync();

        Assert.False(auth.EstAuthentifie);
        Assert.False(auth.EstAdministrateur);
        Assert.False(auth.PeutModifier);
        Assert.Null(auth.UtilisateurActif);
        Assert.Null(auth.ProvinceActive);
        Assert.Null(db.ProvinceAccessor.IdProvince);
    }
}
