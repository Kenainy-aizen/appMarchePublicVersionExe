using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Auth;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// Assistant de première configuration : enregistre le responsable PRMP
/// comme administrateur et crée la première province.
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

    /// <summary>Nombre minimal de caractères exigé par le service d'authentification.</summary>
    public int LongueurMinMotDePasse => AuthService.LongueurMinMotDePasse;

    /// <summary>Le nom du responsable doit-il être signalé comme invalide ?</summary>
    public bool NomProprietaireInvalide => Erreur is not null && string.IsNullOrWhiteSpace(NomProprietaire);

    /// <summary>Le nom de la province doit-il être signalé comme invalide ?</summary>
    public bool NomProvinceInvalide => Erreur is not null && string.IsNullOrWhiteSpace(NomProvince);

    /// <summary>Le mot de passe doit-il être signalé comme invalide ?</summary>
    public bool MotDePasseInvalide =>
        Erreur is not null && MotDePasse.Length < AuthService.LongueurMinMotDePasse;

    /// <summary>La confirmation doit-elle être signalée comme invalide ?</summary>
    public bool ConfirmationInvalide => Erreur is not null && Confirmation != MotDePasse;

    /// <summary>La longueur minimale du mot de passe est-elle atteinte ?</summary>
    public bool MotDePasseSuffisant => MotDePasse.Length >= AuthService.LongueurMinMotDePasse;

    /// <summary>Les deux mots de passe saisis concordent-ils ?</summary>
    public bool MotsDePasseConcordent => Confirmation.Length > 0 && Confirmation == MotDePasse;

    partial void OnErreurChanged(string? value) => NotifierEtatsChamps();

    partial void OnNomProprietaireChanged(string value) => EffacerErreurPuisNotifier();

    partial void OnNomProvinceChanged(string value) => EffacerErreurPuisNotifier();

    partial void OnMotDePasseChanged(string value) => EffacerErreurPuisNotifier();

    partial void OnConfirmationChanged(string value) => EffacerErreurPuisNotifier();

    private void EffacerErreurPuisNotifier()
    {
        if (Erreur is not null)
        {
            Erreur = null;
        }

        NotifierEtatsChamps();
    }

    private void NotifierEtatsChamps()
    {
        OnPropertyChanged(nameof(NomProprietaireInvalide));
        OnPropertyChanged(nameof(NomProvinceInvalide));
        OnPropertyChanged(nameof(MotDePasseInvalide));
        OnPropertyChanged(nameof(ConfirmationInvalide));
        OnPropertyChanged(nameof(MotDePasseSuffisant));
        OnPropertyChanged(nameof(MotsDePasseConcordent));
    }

    /// <summary>Valide la saisie et crée le compte admin PRMP + la première province.</summary>
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
