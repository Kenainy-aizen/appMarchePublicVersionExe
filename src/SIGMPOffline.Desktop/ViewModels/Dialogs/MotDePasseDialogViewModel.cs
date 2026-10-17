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
