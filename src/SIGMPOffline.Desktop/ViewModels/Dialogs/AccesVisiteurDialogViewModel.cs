using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Services.Auth;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Création, remplacement ou révocation du mot de passe visiteur d'une province.</summary>
public partial class AccesVisiteurDialogViewModel : DialogViewModelBase
{
    public AccesVisiteurDialogViewModel(string nomProvince, bool accesConfigure)
    {
        NomProvince = nomProvince;
        AccesConfigure = accesConfigure;
    }

    public string NomProvince { get; }
    public bool AccesConfigure { get; }

    [ObservableProperty] private string _nouveauMotDePasse = string.Empty;
    [ObservableProperty] private string _confirmation = string.Empty;
    [ObservableProperty] private bool _desactiverAcces;
    [ObservableProperty] private string? _erreur;

    public int LongueurMinMotDePasse => AuthService.LongueurMinMotDePasse;
    public bool MotDePasseInvalide => Erreur is not null && NouveauMotDePasse.Length < LongueurMinMotDePasse;
    public bool ConfirmationInvalide => Erreur is not null && Confirmation != NouveauMotDePasse;
    public bool MotsDePasseConcordent => Confirmation.Length > 0 && Confirmation == NouveauMotDePasse;

    partial void OnErreurChanged(string? value) => NotifierEtatsChamps();
    partial void OnNouveauMotDePasseChanged(string value) => EffacerErreurPuisNotifier();
    partial void OnConfirmationChanged(string value) => EffacerErreurPuisNotifier();
    partial void OnDesactiverAccesChanged(bool value) => EffacerErreurPuisNotifier();

    private void EffacerErreurPuisNotifier()
    {
        Erreur = null;
        NotifierEtatsChamps();
    }

    private void NotifierEtatsChamps()
    {
        OnPropertyChanged(nameof(MotDePasseInvalide));
        OnPropertyChanged(nameof(ConfirmationInvalide));
        OnPropertyChanged(nameof(MotsDePasseConcordent));
    }

    public override string Titre => "Accès visiteur";

    protected override bool ValiderSaisie()
    {
        if (DesactiverAcces)
        {
            Erreur = null;
            return true;
        }

        if (NouveauMotDePasse.Length < LongueurMinMotDePasse)
        {
            Erreur = $"Le mot de passe doit contenir au moins {LongueurMinMotDePasse} caractères.";
            return false;
        }

        if (Confirmation != NouveauMotDePasse)
        {
            Erreur = "Les deux mots de passe ne correspondent pas.";
            return false;
        }

        Erreur = null;
        return true;
    }
}
