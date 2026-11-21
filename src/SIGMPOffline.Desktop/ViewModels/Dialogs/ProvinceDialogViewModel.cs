using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Services.Auth;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création d'une nouvelle province et de son mot de passe.</summary>
public partial class ProvinceDialogViewModel : DialogViewModelBase
{
    [ObservableProperty] private string _nomProvince = string.Empty;
    [ObservableProperty] private string _motDePasse = string.Empty;
    [ObservableProperty] private string _confirmation = string.Empty;
    [ObservableProperty] private string? _erreur;

    /// <summary>Nombre minimal de caractères exigé par le service d'authentification.</summary>
    public int LongueurMinMotDePasse => AuthService.LongueurMinMotDePasse;

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
        OnPropertyChanged(nameof(NomProvinceInvalide));
        OnPropertyChanged(nameof(MotDePasseInvalide));
        OnPropertyChanged(nameof(ConfirmationInvalide));
        OnPropertyChanged(nameof(MotDePasseSuffisant));
        OnPropertyChanged(nameof(MotsDePasseConcordent));
    }

    /// <inheritdoc />
    public override string Titre => "Nouvelle province";

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (string.IsNullOrWhiteSpace(NomProvince))
        {
            Erreur = "Le nom de la province est obligatoire.";
            return false;
        }

        if (MotDePasse.Length < AuthService.LongueurMinMotDePasse)
        {
            Erreur = $"Le mot de passe doit contenir au moins {AuthService.LongueurMinMotDePasse} caractères.";
            return false;
        }

        if (MotDePasse != Confirmation)
        {
            Erreur = "Les deux mots de passe ne correspondent pas.";
            return false;
        }

        Erreur = null;
        return true;
    }
}
