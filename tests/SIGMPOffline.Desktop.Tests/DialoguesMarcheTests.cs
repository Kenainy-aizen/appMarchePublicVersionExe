using SIGMPOffline.Desktop.ViewModels;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.Tests;

/// <summary>Tests du contrat des ViewModels de dialogue du module Marché &amp; Bordereau.</summary>
public class DialoguesMarcheTests
{
    private static IReadOnlyList<OptionDossier> Dossiers() =>
    [
        new OptionDossier("DOS-1", "DOS-1 — Premier dossier"),
        new OptionDossier("DOS-2", "DOS-2 — Deuxième dossier")
    ];

    private static IReadOnlyDictionary<string, IReadOnlyList<OptionCandidat>> Candidats() =>
        new Dictionary<string, IReadOnlyList<OptionCandidat>>
        {
            ["DOS-1"] =
            [
                new OptionCandidat(1, "ACME", EstRetenu: true),
                new OptionCandidat(2, "Beta")
            ],
            ["DOS-2"] = [new OptionCandidat(3, "Gamma")]
        };

    [Fact]
    public void Marche_refuse_une_reference_vide()
    {
        var vm = new MarcheDialogViewModel(Dossiers(), Candidats())
        {
            RefMarche = "  ",
            ObjetMarche = "Objet"
        };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.False(ferme);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Marche_refuse_un_objet_vide()
    {
        var vm = new MarcheDialogViewModel(Dossiers(), Candidats())
        {
            RefMarche = "M-1",
            ObjetMarche = ""
        };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.False(ferme);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Marche_VersEntite_mappe_le_dossier_et_le_titulaire()
    {
        var vm = new MarcheDialogViewModel(Dossiers(), Candidats())
        {
            RefMarche = "M-9",
            ObjetMarche = "Objet",
            MontantHT = 1000m,
            MontantTTC = 1180m,
            DelaiExecutionJours = 30,
            DossierSelectionne = Dossiers()[1],
            TitulaireSelectionne = new OptionCandidat(3, "Gamma")
        };

        var entite = vm.VersEntite();

        Assert.Equal("M-9", entite.RefMarche);
        Assert.Equal("DOS-2", entite.RefDossier);
        Assert.Equal(3, entite.IdCandidatTitulaire);
        Assert.Equal(30, entite.DelaiExecutionJours);
    }

    [Fact]
    public void Marche_le_titulaire_aucun_donne_une_cle_nulle()
    {
        var vm = new MarcheDialogViewModel(Dossiers(), Candidats())
        {
            RefMarche = "M-1",
            ObjetMarche = "Objet",
            TitulaireSelectionne = new OptionCandidat(0, "(aucun)")
        };

        Assert.Null(vm.VersEntite().IdCandidatTitulaire);
    }

    [Fact]
    public void Marche_preselectionne_le_candidat_retenu_du_premier_dossier()
    {
        var vm = new MarcheDialogViewModel(Dossiers(), Candidats());

        Assert.NotNull(vm.TitulaireSelectionne);
        Assert.Equal(1, vm.TitulaireSelectionne!.IdCandidat);
    }

    [Fact]
    public void Marche_rafraichit_les_candidats_au_changement_de_dossier()
    {
        var vm = new MarcheDialogViewModel(Dossiers(), Candidats())
        {
            DossierSelectionne = Dossiers()[1]
        };

        // 1 entrée "(aucun)" + 1 candidat du dossier DOS-2 (Gamma).
        Assert.Equal(2, vm.Candidats.Count);
        Assert.Contains(vm.Candidats, c => c.IdCandidat == 3);
        // Gamma n'est pas retenu : le titulaire par défaut reste "(aucun)".
        Assert.Equal(0, vm.TitulaireSelectionne!.IdCandidat);
    }

    [Fact]
    public void Marche_la_reference_n_est_modifiable_qu_a_la_creation()
    {
        Assert.True(new MarcheDialogViewModel(Dossiers(), Candidats()).RefMarcheModifiable);

        var existant = new Marche { RefMarche = "M-1", RefDossier = "DOS-1" };
        Assert.False(new MarcheDialogViewModel(Dossiers(), Candidats(), existant).RefMarcheModifiable);
    }

    [Fact]
    public void Element_refuse_un_code_vide()
    {
        var vm = new ElementMarcheDialogViewModel { CodeElement = "", Libelle = "X" };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.False(ferme);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Element_refuse_un_libelle_vide()
    {
        var vm = new ElementMarcheDialogViewModel { CodeElement = "ART-1", Libelle = "  " };
        var ferme = false;
        vm.CloseRequested += (_, _) => ferme = true;

        vm.ValiderCommand.Execute(null);

        Assert.False(ferme);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public void Element_VersEntite_mappe_les_champs()
    {
        var vm = new ElementMarcheDialogViewModel
        {
            CodeElement = "ART-1",
            Libelle = "Ciment",
            PrixUnitaire = 5000m,
            Quantite = 10m,
            Unite = "sac"
        };

        var entite = vm.VersEntite();

        Assert.Equal("ART-1", entite.CodeElement);
        Assert.Equal("Ciment", entite.Libelle);
        Assert.Equal(5000m, entite.PrixUnitaire);
        Assert.Equal(10m, entite.Quantite);
        Assert.Equal(50_000m, entite.MontantLigne);
    }

    [Fact]
    public void Element_le_code_n_est_modifiable_qu_a_la_creation()
    {
        Assert.True(new ElementMarcheDialogViewModel().CodeElementModifiable);

        var existant = new ElementMarche { CodeElement = "ART-1" };
        Assert.False(new ElementMarcheDialogViewModel(existant).CodeElementModifiable);
    }
}
