using SIGMPOffline.Desktop.ViewModels;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.Tests;

/// <summary>Tests du contrat des ViewModels de dialogue du module DAO.</summary>
public class DialoguesDaoTests
{
    private static IReadOnlyList<OptionLignePpm> Lignes() =>
    [
        new OptionLignePpm("D-1", "D-1 — Première ligne"),
        new OptionLignePpm("D-2", "D-2 — Deuxième ligne")
    ];

    [Fact]
    public void DossierDao_refuse_une_reference_vide()
    {
        var vm = new DossierDaoDialogViewModel(Lignes()) { RefDossier = "  ", ObjetDossier = "Objet" };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.False(ferme);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void DossierDao_refuse_un_objet_vide()
    {
        var vm = new DossierDaoDialogViewModel(Lignes()) { RefDossier = "DOS-1", ObjetDossier = "" };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.False(ferme);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void DossierDao_VersEntite_mappe_la_ligne_selectionnee()
    {
        var vm = new DossierDaoDialogViewModel(Lignes())
        {
            RefDossier = "DOS-9",
            ObjetDossier = "Objet",
            LigneSelectionnee = Lignes()[1],
            CoutCahierCharges = 1000m,
            MontantCaution = 2000m
        };

        var entite = vm.VersEntite();

        Assert.Equal("DOS-9", entite.RefDossier);
        Assert.Equal("D-2", entite.NumPPMDetail);
        Assert.Equal(1000m, entite.CoutCahierCharges);
        Assert.Equal(2000m, entite.MontantCaution);
    }

    [Fact]
    public void DossierDao_la_reference_n_est_modifiable_qu_a_la_creation()
    {
        Assert.True(new DossierDaoDialogViewModel(Lignes()).RefDossierModifiable);

        var existant = new DossierDao { RefDossier = "DOS-1", NumPPMDetail = "D-1" };
        Assert.False(new DossierDaoDialogViewModel(Lignes(), existant).RefDossierModifiable);
    }

    [Fact]
    public void Candidat_refuse_un_nom_vide()
    {
        var vm = new CandidatDialogViewModel { NomEntreprise = "" };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.False(ferme);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Candidat_VersEntite_conserve_l_identifiant()
    {
        var existant = new Candidat { IdCandidat = 42, NomEntreprise = "ACME", MontantOffre = 10m };
        var vm = new CandidatDialogViewModel(existant) { NomEntreprise = "ACME SARL" };

        var entite = vm.VersEntite();

        Assert.Equal(42, entite.IdCandidat);
        Assert.Equal("ACME SARL", entite.NomEntreprise);
    }

    [Fact]
    public void Acheteur_refuse_une_raison_sociale_vide()
    {
        var vm = new AcheteurDaoDialogViewModel { RaisonSociale = "" };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.False(ferme);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Acheteur_VersEntite_normalise_l_adresse_vide()
    {
        var vm = new AcheteurDaoDialogViewModel
        {
            RaisonSociale = "Fournisseur",
            Adresse = "   "
        };

        var entite = vm.VersEntite();

        Assert.Null(entite.Adresse);
    }

    [Fact]
    public void Acheteur_initialise_la_date_d_achat_par_defaut()
    {
        // Une entité avec DateAchat par défaut ne doit pas lever (dépassement de plage).
        var existant = new AcheteurDao { IdAcheteur = 1, RaisonSociale = "X" };
        var vm = new AcheteurDaoDialogViewModel(existant);

        Assert.NotNull(vm.DateAchat);
    }

    [Fact]
    public void Acheteur_le_num_stat_n_est_modifiable_qu_a_la_creation()
    {
        Assert.True(new AcheteurDaoDialogViewModel().NumStatModifiable);
        Assert.False(new AcheteurDaoDialogViewModel(new AcheteurDao { IdAcheteur = 1 }).NumStatModifiable);
    }
}
