using SIGMPOffline.Desktop.ViewModels.Dialogs;

namespace SIGMPOffline.Desktop.Services;

/// <summary>Affiche des fenêtres modales (formulaires, confirmations, messages).</summary>
public interface IDialogService
{
    /// <summary>Ouvre la fenêtre correspondant au ViewModel et retourne <c>true</c> si validé.</summary>
    Task<bool> AfficherAsync(DialogViewModelBase viewModel);

    /// <summary>Affiche une demande de confirmation (retourne <c>true</c> si l'utilisateur confirme).</summary>
    Task<bool> ConfirmerAsync(string titre, string message);

    /// <summary>Affiche un message d'information.</summary>
    Task InformerAsync(string titre, string message);
}
