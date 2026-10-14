using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.Tests;

/// <summary>Tests du contrat des ViewModels de dialogue du module Suivi d'Exécution.</summary>
public class DialoguesSuiviTests
{
    [Fact]
    public void Date_refuse_une_date_absente()
    {
        var vm = new DateDialogViewModel("Titre", "Libellé") { Date = null };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.False(ferme);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Date_reprend_la_date_initiale()
    {
        var vm = new DateDialogViewModel("Émettre l'OS", "Date :", new DateOnly(2026, 3, 15));

        Assert.Equal(new DateOnly(2026, 3, 15), vm.VersDate());
    }

    [Fact]
    public void Date_VersDate_mappe_la_selection()
    {
        var vm = new DateDialogViewModel("Titre", "Libellé")
        {
            Date = new DateTimeOffset(new DateTime(2026, 7, 4))
        };

        Assert.Equal(new DateOnly(2026, 7, 4), vm.VersDate());
    }

    [Fact]
    public void Tef_refuse_une_reference_vide()
    {
        var vm = new TefDialogViewModel { RefTef = "  " };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.False(ferme);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Tef_VersRefTef_normalise_les_espaces()
    {
        var vm = new TefDialogViewModel("  TEF-1  ");

        Assert.Equal("TEF-1", vm.VersRefTef());
    }

    [Fact]
    public void Avenant_refuse_un_numero_vide()
    {
        var vm = new AvenantDialogViewModel { NumAvenant = "" };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.False(ferme);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Avenant_VersEntite_mappe_les_champs()
    {
        var vm = new AvenantDialogViewModel
        {
            NumAvenant = "AV-9",
            Motif = "Travaux supplémentaires",
            VariationMontant = 25_000m,
            ProlongationJours = 12,
            DateAvenant = new DateTimeOffset(new DateTime(2026, 4, 2))
        };

        var entite = vm.VersEntite();

        Assert.Equal("AV-9", entite.NumAvenant);
        Assert.Equal("Travaux supplémentaires", entite.Motif);
        Assert.Equal(25_000m, entite.VariationMontant);
        Assert.Equal(12, entite.ProlongationDelaiJours);
        Assert.Equal(new DateOnly(2026, 4, 2), entite.DateAvenant);
    }

    [Fact]
    public void Avenant_VersEntite_normalise_un_motif_vide()
    {
        var vm = new AvenantDialogViewModel { NumAvenant = "AV-1", Motif = "   " };

        Assert.Null(vm.VersEntite().Motif);
    }

    [Fact]
    public void Avenant_conserve_l_identifiant_en_modification()
    {
        var existant = new Avenant
        {
            IdAvenant = 7,
            NumAvenant = "AV-1",
            DateAvenant = new DateOnly(2026, 5, 1)
        };
        var vm = new AvenantDialogViewModel(existant);

        Assert.Equal(7, vm.VersEntite().IdAvenant);
        Assert.Equal("Modifier l'avenant", vm.Titre);
        Assert.Equal(new DateOnly(2026, 5, 1), vm.VersEntite().DateAvenant);
    }

    [Fact]
    public void Avenant_creation_utilise_le_titre_de_creation()
    {
        Assert.Equal("Nouvel avenant", new AvenantDialogViewModel().Titre);
    }
}
