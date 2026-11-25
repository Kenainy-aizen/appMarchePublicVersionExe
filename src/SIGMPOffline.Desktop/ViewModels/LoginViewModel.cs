using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// Écran de connexion : sélection de la province à gérer puis saisie de son
/// mot de passe.
/// </summary>
public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _auth;

    /// <summary>Signalé lorsque la connexion a réussi.</summary>
    public event EventHandler? Connecte;

    public LoginViewModel(IAuthService auth)
        => _auth = auth ?? throw new ArgumentNullException(nameof(auth));

    /// <summary>Provinces proposées à la connexion.</summary>
    [ObservableProperty] private IReadOnlyList<Province> _provinces = Array.Empty<Province>();

    /// <summary>Province sélectionnée.</summary>
    [ObservableProperty] private Province? _provinceSelectionnee;

    [ObservableProperty] private string _motDePasse = string.Empty;
    [ObservableProperty] private bool _estConnexionVisiteur;
    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private bool _estOccupe;

    /// <summary>Libellé du champ secret selon le type de compte choisi.</summary>
    public string LibelleMotDePasse => EstConnexionVisiteur ? "Mot de passe visiteur" : "Mot de passe province";

    /// <summary>Indication du champ secret selon le type de compte choisi.</summary>
    public string PlaceholderMotDePasse => EstConnexionVisiteur
        ? "Saisissez le mot de passe visiteur"
        : "Saisissez le mot de passe de la province";

    /// <summary>Libellé de l'action principale selon le type de compte.</summary>
    public string LibelleConnexion => EstConnexionVisiteur ? "Accéder en visiteur" : "Se connecter";

    partial void OnEstConnexionVisiteurChanged(bool value)
    {
        OnPropertyChanged(nameof(LibelleMotDePasse));
        OnPropertyChanged(nameof(PlaceholderMotDePasse));
        OnPropertyChanged(nameof(LibelleConnexion));
        MotDePasse = string.Empty;
        Erreur = null;
    }

    /// <summary>Le champ mot de passe doit-il être signalé comme invalide ?</summary>
    public bool MotDePasseInvalide => Erreur is not null;

    partial void OnErreurChanged(string? value) => OnPropertyChanged(nameof(MotDePasseInvalide));

    partial void OnMotDePasseChanged(string value)
    {
        // Dès que l'utilisateur corrige sa saisie, l'ancien message n'est plus pertinent.
        if (Erreur is not null)
        {
            Erreur = null;
        }
    }

    /// <summary>Nom du propriétaire de l'application (affiché en pied d'écran).</summary>
    public string NomProprietaire => _auth.Configuration?.NomProprietairePRMP ?? string.Empty;

    /// <summary>Recharge la liste des provinces et réinitialise la saisie.</summary>
    public async Task RafraichirAsync()
    {
        Erreur = null;
        MotDePasse = string.Empty;
        EstConnexionVisiteur = false;

        try
        {
            Provinces = await _auth.ListerProvincesAsync();
            ProvinceSelectionnee = Provinces.Count > 0 ? Provinces[0] : null;
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
        }

        OnPropertyChanged(nameof(NomProprietaire));
    }

    /// <summary>Vérifie le mot de passe de la province choisie et ouvre la session.</summary>
    [RelayCommand]
    private async Task SeConnecter()
    {
        if (EstOccupe)
        {
            return;
        }

        Erreur = null;

        if (ProvinceSelectionnee is null)
        {
            Erreur = "Sélectionnez une province.";
            return;
        }

        if (string.IsNullOrEmpty(MotDePasse))
        {
            Erreur = "Saisissez le mot de passe de la province.";
            return;
        }

        try
        {
            EstOccupe = true;

            if (EstConnexionVisiteur)
            {
                await _auth.ConnecterVisiteurAsync(ProvinceSelectionnee.IdProvince, MotDePasse);
            }
            else
            {
                await _auth.ConnecterAsync(ProvinceSelectionnee.IdProvince, MotDePasse);
            }

            MotDePasse = string.Empty;
            Connecte?.Invoke(this, EventArgs.Empty);
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
