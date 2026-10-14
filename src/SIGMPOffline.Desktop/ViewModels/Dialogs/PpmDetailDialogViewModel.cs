using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création / modification d'une ligne de détail d'un PPM.</summary>
public partial class PpmDetailDialogViewModel : DialogViewModelBase
{
    private readonly bool _creation;

    /// <summary>Crée le formulaire. <paramref name="existant"/> vaut <c>null</c> pour une création.</summary>
    public PpmDetailDialogViewModel(PpmDetail? existant = null)
    {
        _creation = existant is null;

        _numPPMDetail = existant?.NumPPMDetail ?? string.Empty;
        _objetProjet = existant?.ObjetProjet ?? string.Empty;
        _montantEstimatif = existant?.MontantEstimatif ?? 0m;
        _sourceFinancement = existant?.SourceFinancement;
        _natureSelectionnee = OptionsEnums.Nature(existant?.NatureMarche ?? NatureMarche.Travaux);
        _modeSelectionne = OptionsEnums.Mode(existant?.ModePassation ?? ModePassation.AppelOffresOuvert);
    }

    /// <inheritdoc />
    public override string Titre => _creation ? "Nouvelle ligne de PPM" : "Modifier la ligne de PPM";

    /// <summary>Le numéro de ligne n'est modifiable qu'à la création (il sert de clé).</summary>
    public bool NumDetailModifiable => _creation;

    /// <summary>Natures de marché proposées.</summary>
    public IReadOnlyList<OptionEnum<NatureMarche>> Natures { get; } = OptionsEnums.Natures;

    /// <summary>Modes de passation proposés.</summary>
    public IReadOnlyList<OptionEnum<ModePassation>> Modes { get; } = OptionsEnums.ModesPassation;

    [ObservableProperty] private string _numPPMDetail;
    [ObservableProperty] private string _objetProjet;
    [ObservableProperty] private decimal _montantEstimatif;
    [ObservableProperty] private string? _sourceFinancement;
    [ObservableProperty] private OptionEnum<NatureMarche> _natureSelectionnee;
    [ObservableProperty] private OptionEnum<ModePassation> _modeSelectionne;
    [ObservableProperty] private string? _erreur;

    /// <summary>Construit l'entité à partir de la saisie.</summary>
    public PpmDetail VersEntite() => new()
    {
        NumPPMDetail = NumPPMDetail.Trim(),
        ObjetProjet = ObjetProjet.Trim(),
        MontantEstimatif = MontantEstimatif,
        SourceFinancement = string.IsNullOrWhiteSpace(SourceFinancement) ? null : SourceFinancement.Trim(),
        NatureMarche = NatureSelectionnee.Valeur,
        ModePassation = ModeSelectionne.Valeur
    };

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (string.IsNullOrWhiteSpace(NumPPMDetail))
        {
            Erreur = "Le numéro de la ligne est obligatoire.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ObjetProjet))
        {
            Erreur = "L'objet du projet est obligatoire.";
            return false;
        }

        if (MontantEstimatif < 0)
        {
            Erreur = "Le montant estimatif ne peut pas être négatif.";
            return false;
        }

        Erreur = null;
        return true;
    }
}
