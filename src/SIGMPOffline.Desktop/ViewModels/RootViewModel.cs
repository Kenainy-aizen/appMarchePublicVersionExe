using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// ViewModel racine de la fenêtre principale. Il aiguille l'application entre
/// les trois écrans : <b>initialisation</b> (premier lancement), <b>connexion</b>
/// et <b>shell</b> (application ouverte sur une province).
/// </summary>
public partial class RootViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly SetupViewModel _setup;
    private readonly LoginViewModel _login;
    private readonly ShellViewModel _shell;

    /// <summary>Écran actuellement affiché.</summary>
    [ObservableProperty] private ViewModelBase? _ecranCourant;

    public RootViewModel(
        IAuthService auth,
        SetupViewModel setup,
        LoginViewModel login,
        ShellViewModel shell)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _setup = setup ?? throw new ArgumentNullException(nameof(setup));
        _login = login ?? throw new ArgumentNullException(nameof(login));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));

        _setup.Termine += SurInitialisationTerminee;
        _login.Connecte += SurConnexion;
        _shell.DeconnexionDemandee += SurDeconnexion;
    }

    /// <summary>Détermine l'écran de départ : assistant d'installation ou connexion.</summary>
    public async Task DemarrerAsync()
    {
        if (await _auth.EstPremierLancementAsync())
        {
            EcranCourant = _setup;
            return;
        }

        await AfficherConnexionAsync();
    }

    private async void SurInitialisationTerminee(object? sender, EventArgs e)
        => await OuvrirShellAsync();

    private async void SurConnexion(object? sender, EventArgs e)
        => await OuvrirShellAsync();

    private async void SurDeconnexion(object? sender, EventArgs e)
        => await AfficherConnexionAsync();

    /// <summary>Ouvre le shell sur la province active et charge la première page.</summary>
    private async Task OuvrirShellAsync()
    {
        await _shell.ActiverAsync();
        EcranCourant = _shell;
    }

    /// <summary>Affiche l'écran de connexion (liste des provinces à jour).</summary>
    private async Task AfficherConnexionAsync()
    {
        await _login.RafraichirAsync();
        EcranCourant = _login;
    }
}
