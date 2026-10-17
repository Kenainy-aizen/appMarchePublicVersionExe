using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.Tests;

/// <summary>Tests du contrat des ViewModels liés à l'authentification et aux provinces.</summary>
public class DialoguesProvinceTests
{
    [Fact]
    public void Province_refuse_un_nom_vide()
    {
        var vm = new ProvinceDialogViewModel { NomProvince = "  ", MotDePasse = "secret", Confirmation = "secret" };
        Assert.False(Valider(vm));
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Province_refuse_un_mot_de_passe_trop_court()
    {
        var vm = new ProvinceDialogViewModel { NomProvince = "Province B", MotDePasse = "ab", Confirmation = "ab" };
        Assert.False(Valider(vm));
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Province_refuse_une_confirmation_differente()
    {
        var vm = new ProvinceDialogViewModel { NomProvince = "Province B", MotDePasse = "secret", Confirmation = "secre" };
        Assert.False(Valider(vm));
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Province_accepte_une_saisie_valide()
    {
        var vm = new ProvinceDialogViewModel { NomProvince = "Province B", MotDePasse = "secret", Confirmation = "secret" };
        Assert.True(Valider(vm));
        Assert.Null(vm.Erreur);
    }

    [Fact]
    public void MotDePasse_exige_l_ancien_mot_de_passe()
    {
        var vm = new MotDePasseDialogViewModel("Province A")
        {
            AncienMotDePasse = string.Empty,
            NouveauMotDePasse = "nouveau",
            Confirmation = "nouveau"
        };

        Assert.False(Valider(vm));
        Assert.NotNull(vm.Erreur);
        Assert.Equal("Province A", vm.NomProvince);
    }

    [Fact]
    public void MotDePasse_accepte_une_saisie_valide()
    {
        var vm = new MotDePasseDialogViewModel("Province A")
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
    public void ChangerProvince_exige_un_mot_de_passe()
    {
        var provinces = new List<Province> { new() { IdProvince = 1, NomProvince = "Province A" } };
        var vm = new ChangerProvinceDialogViewModel(provinces, null) { MotDePasse = string.Empty };

        Assert.False(Valider(vm));
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void ChangerProvince_accepte_une_saisie_valide()
    {
        var provinces = new List<Province> { new() { IdProvince = 1, NomProvince = "Province A" } };
        var vm = new ChangerProvinceDialogViewModel(provinces, null) { MotDePasse = "secret" };

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

    private static bool Valider(DialogViewModelBase vm)
    {
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;
        vm.ValiderCommand.Execute(null);
        return ferme;
    }
}
