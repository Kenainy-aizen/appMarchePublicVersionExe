using CommunityToolkit.Mvvm.ComponentModel;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de saisie du Titre d'Engagement Financier (TEF).</summary>
public partial class TefDialogViewModel : DialogViewModelBase
{
    /// <summary>Crée le formulaire, éventuellement pré-rempli.</summary>
    public TefDialogViewModel(string? refTef = null)
        => _refTef = refTef ?? string.Empty;

    /// <inheritdoc />
    public override string Titre => "Titre d'Engagement Financier (TEF)";

    [ObservableProperty] private string _refTef;
    [ObservableProperty] private string? _erreur;

    /// <summary>Retourne la référence saisie.</summary>
    public string VersRefTef() => RefTef.Trim();

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (string.IsNullOrWhiteSpace(RefTef))
        {
            Erreur = "La référence du TEF est obligatoire.";
            return false;
        }

        Erreur = null;
        return true;
    }
}
