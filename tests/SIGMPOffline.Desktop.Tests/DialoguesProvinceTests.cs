using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Auth;

namespace SIGMPOffline.Desktop.Tests;

/// <summary>Tests du contrat des ViewModels liés à l'authentification et aux provinces.</summary>
public class DialoguesProvinceTests
{
    [Fact]
    public void Province_refuse_un_nom_vide()
    {
        var vm = new ProvinceDialogViewModel { NomProvince = "  " };
        Assert.False(Valider(vm));
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Province_accepte_une_saisie_valide()
    {
        var vm = new ProvinceDialogViewModel { NomProvince = " Province B " };
        Assert.True(Valider(vm));
        Assert.Equal("Province B", vm.NomProvince);
        Assert.Null(vm.Erreur);
    }

    [Fact]
    public void MotDePasse_exige_l_ancien_mot_de_passe()
    {
        var vm = new MotDePasseDialogViewModel("Agent")
        {
            AncienMotDePasse = string.Empty,
            NouveauMotDePasse = "nouveau",
            Confirmation = "nouveau"
        };

        Assert.False(Valider(vm));
        Assert.NotNull(vm.Erreur);
        Assert.Equal("Agent", vm.NomUtilisateur);
    }

    [Fact]
    public void MotDePasse_accepte_une_saisie_valide()
    {
        var vm = new MotDePasseDialogViewModel("Agent")
        {
            AncienMotDePasse = "ancien",
            NouveauMotDePasse = "nouveau",
            Confirmation = "nouveau"
        };

        Assert.True(Valider(vm));
    }

    [Fact]
    public void ChangerProvince_presélectionne_la_province_courante()
    {
        var provinces = new List<Province>
        {
            new() { IdProvince = 1, NomProvince = "Province A" },
            new() { IdProvince = 2, NomProvince = "Province B" }
        };

        var vm = new ChangerProvinceDialogViewModel(provinces, 2);

        Assert.Equal(2, vm.ProvinceSelectionnee!.IdProvince);
        Assert.Equal(2, vm.IdProvinceChoisi);
    }

    [Fact]
    public void ChangerProvince_refuse_une_liste_vide()
    {
        var vm = new ChangerProvinceDialogViewModel(Array.Empty<Province>(), null);

        Assert.False(Valider(vm));
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void ChangerProvince_accepte_une_saisie_valide()
    {
        var provinces = new List<Province> { new() { IdProvince = 1, NomProvince = "Province A" } };
        var vm = new ChangerProvinceDialogViewModel(provinces, null);

        Assert.True(Valider(vm));
        Assert.Equal(1, vm.IdProvinceChoisi);
    }

    [Fact]
    public void ChangerProvince_supporte_une_liste_vide()
    {
        var vm = new ChangerProvinceDialogViewModel(Array.Empty<Province>(), null);

        Assert.Null(vm.ProvinceSelectionnee);
        Assert.Equal(0, vm.IdProvinceChoisi);
        Assert.False(Valider(vm));
    }

    [Fact]
    public void NomPrmp_est_prerempli_et_nettoie_les_espaces()
    {
        var vm = new NomPrmpDialogViewModel("PRMP Jean Dupont") { NomProprietaire = "  PRMP Awa  " };

        Assert.True(Valider(vm));
        Assert.Equal("PRMP Awa", vm.VersNomProprietaire());
    }

    [Fact]
    public void NomPrmp_refuse_un_nom_vide()
    {
        var vm = new NomPrmpDialogViewModel("PRMP") { NomProprietaire = "   " };

        Assert.False(Valider(vm));
        Assert.NotNull(vm.Erreur);
        Assert.True(vm.NomInvalide);
    }

    [Fact]
    public void NomPrmp_refuse_un_nom_trop_long()
    {
        var vm = new NomPrmpDialogViewModel("PRMP")
        {
            NomProprietaire = new string('a', AuthService.LongueurMaxNomProprietaire + 1)
        };

        Assert.False(Valider(vm));
        Assert.NotNull(vm.Erreur);
    }

    private static bool Valider(DialogViewModelBase vm)
    {
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;
        vm.ValiderCommand.Execute(null);
        return ferme;
    }
}
