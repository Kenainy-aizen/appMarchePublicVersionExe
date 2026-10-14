using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création / modification d'un élément du bordereau d'un marché.</summary>
public partial class ElementMarcheDialogViewModel : DialogViewModelBase
{
    private readonly bool _creation;

    /// <summary>Crée le formulaire. <paramref name="existant"/> vaut <c>null</c> pour une création.</summary>
    public ElementMarcheDialogViewModel(ElementMarche? existant = null)
    {
        _creation = existant is null;

        _codeElement = existant?.CodeElement ?? string.Empty;
        _libelle = existant?.Libelle ?? string.Empty;
        _prixUnitaire = existant?.PrixUnitaire ?? 0m;
        _quantite = existant?.Quantite ?? 1m;
        _unite = existant?.Unite ?? string.Empty;
    }

    /// <inheritdoc />
    public override string Titre => _creation ? "Nouvel élément du bordereau" : "Modifier l'élément";

    /// <summary>Le code n'est modifiable qu'à la création (il sert de clé).</summary>
    public bool CodeElementModifiable => _creation;

    [ObservableProperty] private string _codeElement;
    [ObservableProperty] private string _libelle;
    [ObservableProperty] private decimal _prixUnitaire;
    [ObservableProperty] private decimal _quantite;
    [ObservableProperty] private string _unite;
    [ObservableProperty] private string? _erreur;

    /// <summary>Construit l'entité à partir de la saisie.</summary>
    public ElementMarche VersEntite() => new()
    {
        CodeElement = CodeElement.Trim(),
        Libelle = Libelle.Trim(),
        PrixUnitaire = PrixUnitaire,
        Quantite = Quantite,
        Unite = Unite.Trim()
    };

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (string.IsNullOrWhiteSpace(CodeElement))
        {
            Erreur = "Le code de l'élément est obligatoire.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Libelle))
        {
            Erreur = "Le libellé de l'élément est obligatoire.";
            return false;
        }

        if (PrixUnitaire < 0 || Quantite < 0)
        {
            Erreur = "Le prix unitaire et la quantité ne peuvent pas être négatifs.";
            return false;
        }

        Erreur = null;
        return true;
    }
}
