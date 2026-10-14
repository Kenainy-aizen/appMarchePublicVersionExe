using CommunityToolkit.Mvvm.Input;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>
/// Base des ViewModels de fenêtres modales. Expose deux commandes
/// (<see cref="ValiderCommand"/> / <see cref="AnnulerCommand"/>) et un évènement
/// de fermeture que le <c>DialogService</c> traduit en fermeture de fenêtre.
/// </summary>
public abstract partial class DialogViewModelBase : ViewModelBase
{
    /// <summary>Titre affiché par la fenêtre.</summary>
    public abstract string Titre { get; }

    /// <summary>Demande la fermeture de la fenêtre avec le résultat indiqué.</summary>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>Ferme la fenêtre avec le résultat demandé.</summary>
    protected void Close(bool resultat) => CloseRequested?.Invoke(this, resultat);

    /// <summary>Valide la saisie puis ferme si elle est correcte.</summary>
    [RelayCommand]
    private void Valider()
    {
        if (ValiderSaisie())
        {
            Close(true);
        }
    }

    /// <summary>Ferme la fenêtre sans rien enregistrer.</summary>
    [RelayCommand]
    private void Annuler() => Close(false);

    /// <summary>Valide la saisie. Retourner <c>false</c> empêche la fermeture.</summary>
    protected virtual bool ValiderSaisie() => true;
}
