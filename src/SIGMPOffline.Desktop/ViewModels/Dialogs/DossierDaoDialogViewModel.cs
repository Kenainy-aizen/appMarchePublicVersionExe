using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création / modification d'un dossier d'appel d'offres (DAO).</summary>
public partial class DossierDaoDialogViewModel : DialogViewModelBase
{
    private readonly bool _creation;

    /// <summary>Crée le formulaire. <paramref name="existant"/> vaut <c>null</c> pour une création.</summary>
    public DossierDaoDialogViewModel(
        IReadOnlyList<OptionLignePpm> lignes,
        DossierDao? existant = null)
    {
        Lignes = lignes ?? throw new ArgumentNullException(nameof(lignes));
        _creation = existant is null;

        _refDossier = existant?.RefDossier ?? string.Empty;
        _objetDossier = existant?.ObjetDossier ?? string.Empty;
        _coutCahierCharges = existant?.CoutCahierCharges ?? 0m;
        _montantCaution = existant?.MontantCaution ?? 0m;

        var numLigne = existant?.NumPPMDetail;
        _ligneSelectionnee = Lignes.FirstOrDefault(l => l.NumPPMDetail == numLigne)
            ?? Lignes.FirstOrDefault();
    }

    /// <inheritdoc />
    public override string Titre => _creation ? "Nouveau dossier DAO" : "Modifier le dossier DAO";

    /// <summary>La référence n'est modifiable qu'à la création (elle sert de clé).</summary>
    public bool RefDossierModifiable => _creation;

    /// <summary>Lignes de PPM proposées.</summary>
    public IReadOnlyList<OptionLignePpm> Lignes { get; }

    [ObservableProperty] private string _refDossier;
    [ObservableProperty] private string _objetDossier;
    [ObservableProperty] private decimal _coutCahierCharges;
    [ObservableProperty] private decimal _montantCaution;
    [ObservableProperty] private OptionLignePpm? _ligneSelectionnee;
    [ObservableProperty] private string? _erreur;

    /// <summary>Construit l'entité à partir de la saisie.</summary>
    public DossierDao VersEntite() => new()
    {
        RefDossier = RefDossier.Trim(),
        NumPPMDetail = LigneSelectionnee?.NumPPMDetail ?? string.Empty,
        ObjetDossier = ObjetDossier.Trim(),
        CoutCahierCharges = CoutCahierCharges,
        MontantCaution = MontantCaution
    };

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (string.IsNullOrWhiteSpace(RefDossier))
        {
            Erreur = "La référence du dossier est obligatoire.";
            return false;
        }

        if (LigneSelectionnee is null)
        {
            Erreur = "Le dossier doit être rattaché à une ligne du PPM.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ObjetDossier))
        {
            Erreur = "L'objet du dossier est obligatoire.";
            return false;
        }

        if (CoutCahierCharges < 0 || MontantCaution < 0)
        {
            Erreur = "Les montants ne peuvent pas être négatifs.";
            return false;
        }

        Erreur = null;
        return true;
    }
}
