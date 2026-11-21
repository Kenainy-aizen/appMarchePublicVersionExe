using SIGMPOffline.Desktop.ViewModels;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Desktop.Tests;

/// <summary>
/// Tests du contrat des ViewModels de dialogue (validation, fermeture, mapping).
/// Ces ViewModels ne dépendent pas d'Avalonia : ils sont testables sans IHM.
/// </summary>
public class DialoguesTests
{
    [Fact]
    public void MessageDialog_valider_declenche_la_fermeture_avec_succes()
    {
        var vm = new MessageDialogViewModel("T", "M", estQuestion: false);
        bool? resultat = null;
        vm.CloseRequested += (_, r) => resultat = r;

        vm.ValiderCommand.Execute(null);

        Assert.True(resultat);
    }

    [Fact]
    public void MessageDialog_annuler_declenche_la_fermeture_sans_succes()
    {
        var vm = new MessageDialogViewModel("T", "M", estQuestion: true);
        bool? resultat = null;
        vm.CloseRequested += (_, r) => resultat = r;

        vm.AnnulerCommand.Execute(null);

        Assert.False(resultat);
    }

    [Fact]
    public void MessageDialog_affiche_le_bouton_annuler_seulement_pour_une_question()
    {
        Assert.True(new MessageDialogViewModel("T", "M", estQuestion: true).AfficherAnnuler);
        Assert.False(new MessageDialogViewModel("T", "M", estQuestion: false).AfficherAnnuler);
    }

    [Fact]
    public void PpmEntete_valider_refuse_une_saisie_vide()
    {
        var vm = new PpmEnteteDialogViewModel { NumPPM = string.Empty, AutoriteContractante = string.Empty };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.False(ferme);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void PpmEntete_valider_accepte_une_saisie_valide()
    {
        var vm = new PpmEnteteDialogViewModel { NumPPM = "PPM-2026", AutoriteContractante = "PRMP" };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.True(ferme);
        Assert.Null(vm.Erreur);
    }

    [Fact]
    public void PpmEntete_le_numero_est_modifiable_uniquement_a_la_creation()
    {
        Assert.True(new PpmEnteteDialogViewModel().NumPpmModifiable);

        var existant = new Domain.Entities.PpmEntete { NumPPM = "PPM-2026" };
        Assert.False(new PpmEnteteDialogViewModel(existant).NumPpmModifiable);
    }

    [Fact]
    public void PpmEntete_la_date_de_mise_a_jour_est_saisissable_en_modification()
    {
        var existant = new Domain.Entities.PpmEntete
        {
            NumPPM = "PPM-2026",
            DateDerniereMAJ = new DateTime(2026, 3, 15),
            NumDerniereMAJ = "2"
        };

        var vm = new PpmEnteteDialogViewModel(existant)
        {
            DateDerniereMAJ = new DateTimeOffset(new DateTime(2026, 4, 20))
        };

        Assert.True(vm.AfficherDateMiseAJour);
        Assert.False(new PpmEnteteDialogViewModel().AfficherDateMiseAJour);

        // La date change : le numéro affiché passe de 2 à 3.
        Assert.Equal("3", vm.NumeroMiseAJourAffiche);

        var entite = vm.VersEntite();
        Assert.Equal(new DateTime(2026, 4, 20), entite.DateDerniereMAJ);
        Assert.Equal("3", entite.NumDerniereMAJ);

        // Retour à la date d'origine : le numéro revient à sa valeur initiale.
        vm.DateDerniereMAJ = new DateTimeOffset(new DateTime(2026, 3, 15));
        Assert.Equal("2", vm.NumeroMiseAJourAffiche);
    }

    [Fact]
    public void PpmEntete_VersEntite_mappe_les_champs()
    {
        var vm = new PpmEnteteDialogViewModel
        {
            NumPPM = "PPM-2030",
            AnneeExercice = 2030,
            AutoriteContractante = "PRMP X"
        };

        var entite = vm.VersEntite();

        Assert.Equal("PPM-2030", entite.NumPPM);
        Assert.Equal(2030, entite.AnneeExercice);
        Assert.Equal("PRMP X", entite.AutoriteContractante);
    }

    [Fact]
    public void PpmDetail_VersEntite_mappe_les_enums_et_montants()
    {
        var vm = new PpmDetailDialogViewModel
        {
            NumPPMDetail = "D-9",
            ObjetProjet = "Objet",
            MontantEstimatif = 1234m,
            SourceFinancement = "Bailleur",
            NatureSelectionnee = OptionsEnums.Nature(NatureMarche.Fournitures),
            ModeSelectionne = OptionsEnums.Mode(ModePassation.ConsultationPrix)
        };

        var entite = vm.VersEntite();

        Assert.Equal(NatureMarche.Fournitures, entite.NatureMarche);
        Assert.Equal(ModePassation.ConsultationPrix, entite.ModePassation);
        Assert.Equal(1234m, entite.MontantEstimatif);
        Assert.Equal("Bailleur", entite.SourceFinancement);
    }

    [Fact]
    public void PpmDetail_initialise_les_options_depuis_l_entite()
    {
        var existant = new Domain.Entities.PpmDetail
        {
            NumPPMDetail = "D-1",
            NatureMarche = NatureMarche.PrestationsIntellectuelles,
            ModePassation = ModePassation.AppelPrequalification
        };

        var vm = new PpmDetailDialogViewModel(existant);

        Assert.Equal(NatureMarche.PrestationsIntellectuelles, vm.NatureSelectionnee.Valeur);
        Assert.Equal(ModePassation.AppelPrequalification, vm.ModeSelectionne.Valeur);
    }
}
