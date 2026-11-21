using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Services.Auth;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de changement du mot de passe d'une province.</summary>
public partial class MotDePasseDialogViewModel : DialogViewModelBase
{
    /// <summary>Crée le formulaire pour la province indiquée.</summary>
    public MotDePasseDialogViewModel(string nomProvince)
    {
        NomProvince = nomProvince;
    }

    /// <summary>Province dont on change le mot de passe.</summary>
    public string NomProvince { get; }

    [ObservableProperty] private string _ancienMotDePasse = string.Empty;
    [ObservableProperty] private string _nouveauMotDePasse = string.Empty;
    [ObservableProperty] private string _confirmation = string.Empty;
    [ObservableProperty] private string? _erreur;

    /// <summary>Nombre minimal de caractères exigé par le service d'authentification.</summary>
    public int LongueurMinMotDePasse => AuthService.LongueurMinMotDePasse;

    /// <summary>L'ancien mot de passe doit-il être signalé comme invalide ?</summary>
    public bool AncienMotDePasseInvalide => Erreur is not null && string.IsNullOrEmpty(AncienMotDePasse);

    /// <summary>Le nouveau mot de passe doit-il être signalé comme invalide ?</summary>
    public bool NouveauMotDePasseInvalide =>
        Erreur is not null && NouveauMotDePasse.Length < AuthService.LongueurMinMotDePasse;

    /// <summary>La confirmation doit-elle être signalée comme invalide ?</summary>
    public bool ConfirmationInvalide => Erreur is not null && Confirmation != NouveauMotDePasse;

    /// <summary>La longueur minimale du nouveau mot de passe est-elle atteinte ?</summary>
    public bool NouveauMotDePasseSuffisant =>
        NouveauMotDePasse.Length >= AuthService.LongueurMinMotDePasse;

    /// <summary>Les deux mots de passe saisis concordent-ils ?</summary>
    public bool MotsDePasseConcordent =>
        Confirmation.Length > 0 && Confirmation == NouveauMotDePasse;

    partial void OnErreurChanged(string? value) => NotifierEtatsChamps();

    partial void OnAncienMotDePasseChanged(string value) => EffacerErreurPuisNotifier();

    partial void OnNouveauMotDePasseChanged(string value) => EffacerErreurPuisNotifier();

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
        OnPropertyChanged(nameof(AncienMotDePasseInvalide));
        OnPropertyChanged(nameof(NouveauMotDePasseInvalide));
        OnPropertyChanged(nameof(ConfirmationInvalide));
        OnPropertyChanged(nameof(NouveauMotDePasseSuffisant));
        OnPropertyChanged(nameof(MotsDePasseConcordent));
    }

    /// <inheritdoc />
    public override string Titre => "Changer le mot de passe";

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (string.IsNullOrEmpty(AncienMotDePasse))
        {
            Erreur = "Saisissez l'ancien mot de passe.";
            return false;
        }

        if (NouveauMotDePasse.Length < AuthService.LongueurMinMotDePasse)
        {
            Erreur = $"Le nouveau mot de passe doit contenir au moins {AuthService.LongueurMinMotDePasse} caractères.";
            return false;
        }

        if (NouveauMotDePasse != Confirmation)
        {
            Erreur = "Les deux mots de passe ne correspondent pas.";
            return false;
        }

        Erreur = null;
        return true;
    }
}
