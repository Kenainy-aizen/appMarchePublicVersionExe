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
