using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// Assistant de première configuration : enregistre le responsable PRMP
/// (propriétaire) et crée la première province avec son mot de passe.
/// </summary>
public partial class SetupViewModel : ViewModelBase
{
    private readonly IAuthService _auth;

    /// <summary>Signalé lorsque la configuration initiale a réussi.</summary>
    public event EventHandler? Termine;

    public SetupViewModel(IAuthService auth)
        => _auth = auth ?? throw new ArgumentNullException(nameof(auth));

    [ObservableProperty] private string _nomProprietaire = string.Empty;
    [ObservableProperty] private string _nomProvince = string.Empty;
    [ObservableProperty] private string _motDePasse = string.Empty;
    [ObservableProperty] private string _confirmation = string.Empty;
    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private bool _estOccupe;

    /// <summary>Valide la saisie et crée la configuration + la première province.</summary>
    [RelayCommand]
    private async Task Terminer()
    {
        if (EstOccupe)
        {
            return;
        }

        Erreur = null;

        if (string.IsNullOrWhiteSpace(NomProprietaire))
        {
            Erreur = "Indiquez le nom du responsable PRMP.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NomProvince))
        {
            Erreur = "Indiquez le nom de la province.";
            return;
        }

        if (MotDePasse != Confirmation)
        {
            Erreur = "Les deux mots de passe ne correspondent pas.";
            return;
        }

        try
        {
            EstOccupe = true;

            await _auth.InitialiserAsync(NomProprietaire, NomProvince, MotDePasse);

            MotDePasse = string.Empty;
            Confirmation = string.Empty;

            Termine?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>Ferme immédiatement l'application.</summary>
    [RelayCommand]
    private static void Quitter()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
