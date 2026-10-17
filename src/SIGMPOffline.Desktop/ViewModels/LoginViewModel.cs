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
    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private bool _estOccupe;

    /// <summary>Nom du propriétaire de l'application (affiché en pied d'écran).</summary>
    public string NomProprietaire => _auth.Configuration?.NomProprietairePRMP ?? string.Empty;

    /// <summary>Recharge la liste des provinces et réinitialise la saisie.</summary>
    public async Task RafraichirAsync()
    {
        Erreur = null;
        MotDePasse = string.Empty;

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

            await _auth.ConnecterAsync(ProvinceSelectionnee.IdProvince, MotDePasse);

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
