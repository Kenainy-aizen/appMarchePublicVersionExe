using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.Tests;

public class GestionComptesTests
{
    [Fact]
    public void Creation_compte_permet_consultation_seule_par_province()
    {
        var provinceA = new Province { IdProvince = 1, NomProvince = "Province A" };
        var provinceB = new Province { IdProvince = 2, NomProvince = "Province B" };
        var vm = new UtilisateurDialogViewModel(new[] { provinceA, provinceB })
        {
            NomUtilisateur = "Agent DAO",
            MotDePasse = "agent-secret",
            Confirmation = "agent-secret"
        };
        vm.Droits[0].EstAutorise = true;
        vm.Droits[0].PeutModifier = false;

        Assert.True(Valider(vm));
        Assert.Equal(new Dictionary<int, bool> { [1] = false }, vm.DroitsParProvince);
    }

    [Fact]
    public void Creation_compte_exige_au_moins_une_province()
    {
        var province = new Province { IdProvince = 1, NomProvince = "Province A" };
        var vm = new UtilisateurDialogViewModel(new[] { province })
        {
            NomUtilisateur = "Agent DAO",
            MotDePasse = "agent-secret",
            Confirmation = "agent-secret"
        };

        Assert.False(Valider(vm));
        Assert.Contains("province", vm.Erreur!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Modification_compte_permet_de_conserver_le_mot_de_passe()
    {
        var province = new Province { IdProvince = 1, NomProvince = "Province A" };
        var utilisateur = new Utilisateur
        {
            IdUtilisateur = 12,
            NomUtilisateur = "Agent DAO",
            PermissionsProvinces = new List<PermissionProvince>
            {
                new() { IdUtilisateur = 12, IdProvince = 1, PeutModifier = true, Province = province }
            }
        };
        var vm = new UtilisateurDialogViewModel(new[] { province }, utilisateur);

        Assert.True(Valider(vm));
        Assert.Equal("Agent DAO", vm.NomUtilisateur);
        Assert.Empty(vm.MotDePasse);
        Assert.Equal(new Dictionary<int, bool> { [1] = true }, vm.DroitsParProvince);
    }

    private static bool Valider(DialogViewModelBase vm)
    {
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;
        vm.ValiderCommand.Execute(null);
        return ferme;
    }
}
