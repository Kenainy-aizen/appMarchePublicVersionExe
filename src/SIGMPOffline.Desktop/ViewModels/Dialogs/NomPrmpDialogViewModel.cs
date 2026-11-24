using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Services.Auth;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de modification du nom du responsable PRMP.</summary>
public partial class NomPrmpDialogViewModel : DialogViewModelBase
{
    /// <summary>Crée le formulaire, pré-rempli avec le nom actuel.</summary>
    public NomPrmpDialogViewModel(string? nomProprietaire = null)
        => _nomProprietaire = nomProprietaire ?? string.Empty;

    /// <inheritdoc />
    public override string Titre => "Nom de la PRMP";

    [ObservableProperty] private string _nomProprietaire;
    [ObservableProperty] private string? _erreur;

    /// <summary>Longueur maximale admise par le service d'authentification.</summary>
    public int LongueurMaxNom => AuthService.LongueurMaxNomProprietaire;

    /// <summary>Le nom saisi doit-il être signalé comme invalide ?</summary>
    public bool NomInvalide => Erreur is not null && string.IsNullOrWhiteSpace(NomProprietaire);

    /// <summary>Retourne le nom saisi, sans espaces superflus.</summary>
    public string VersNomProprietaire() => NomProprietaire.Trim();

    partial void OnErreurChanged(string? value) => OnPropertyChanged(nameof(NomInvalide));

    partial void OnNomProprietaireChanged(string value)
    {
        if (Erreur is not null)
        {
            Erreur = null;
        }
    }

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        var nom = NomProprietaire.Trim();

        if (nom.Length == 0)
        {
            Erreur = "Le nom du responsable PRMP est obligatoire.";
            return false;
        }

        if (nom.Length > LongueurMaxNom)
        {
            Erreur = $"Le nom du responsable PRMP ne peut pas dépasser {LongueurMaxNom} caractères.";
            return false;
        }

        Erreur = null;
        return true;
    }
}
