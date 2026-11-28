using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Auth;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Création ou modification d'un compte et de ses droits province par province.</summary>
public partial class UtilisateurDialogViewModel : DialogViewModelBase
{
    public UtilisateurDialogViewModel(IReadOnlyList<Province> provinces, Utilisateur? utilisateur = null)
    {
        UtilisateurExistant = utilisateur;
        NomUtilisateur = utilisateur?.NomUtilisateur ?? string.Empty;
        var droitsExistants = utilisateur?.PermissionsProvinces.ToDictionary(x => x.IdProvince)
            ?? new Dictionary<int, PermissionProvince>();

        Droits = new ObservableCollection<DroitProvinceOptionViewModel>(provinces.Select(province =>
        {
            var droitExistant = droitsExistants.GetValueOrDefault(province.IdProvince);
            return new DroitProvinceOptionViewModel(
                province,
                droitExistant is not null,
                droitExistant?.PeutModifier ?? false);
        }));
    }

    public Utilisateur? UtilisateurExistant { get; }
    public bool EstCreation => UtilisateurExistant is null;
    public string TitreAction => EstCreation ? "Créer le compte" : "Enregistrer";
    public string LibelleMotDePasse => EstCreation ? "Mot de passe initial" : "Nouveau mot de passe (facultatif)";
    public string AideMotDePasse => EstCreation
        ? $"Au moins {LongueurMinMotDePasse} caractères."
        : "Laissez vide pour conserver le mot de passe actuel.";
    public bool AfficherConfirmationMotDePasse => EstCreation || MotDePasse.Length > 0;
    public int LongueurMinMotDePasse => AuthService.LongueurMinMotDePasse;
    public ObservableCollection<DroitProvinceOptionViewModel> Droits { get; }
    public IReadOnlyDictionary<int, bool> DroitsParProvince => Droits
        .Where(x => x.EstAutorise)
        .ToDictionary(x => x.IdProvince, x => x.PeutModifier);

    [ObservableProperty] private string _nomUtilisateur = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AfficherConfirmationMotDePasse))]
    private string _motDePasse = string.Empty;
    [ObservableProperty] private string _confirmation = string.Empty;
    [ObservableProperty] private string? _erreur;

    public bool NomUtilisateurInvalide => Erreur is not null && string.IsNullOrWhiteSpace(NomUtilisateur);
    public bool MotDePasseInvalide => Erreur is not null
        && (EstCreation || MotDePasse.Length > 0)
        && MotDePasse.Length < LongueurMinMotDePasse;
    public bool ConfirmationInvalide => Erreur is not null && MotDePasse.Length > 0 && Confirmation != MotDePasse;

    partial void OnErreurChanged(string? value) => NotifierEtatsChamps();
    partial void OnNomUtilisateurChanged(string value) => EffacerErreurPuisNotifier();
    partial void OnMotDePasseChanged(string value) => EffacerErreurPuisNotifier();
    partial void OnConfirmationChanged(string value) => EffacerErreurPuisNotifier();

    public override string Titre => EstCreation ? "Nouveau compte utilisateur" : "Modifier le compte utilisateur";

    protected override bool ValiderSaisie()
    {
        NomUtilisateur = (NomUtilisateur ?? string.Empty).Trim();
        if (NomUtilisateur.Length == 0)
        {
            Erreur = "Le nom d'utilisateur est obligatoire.";
            return false;
        }

        if (NomUtilisateur.Length > AuthService.LongueurMaxNomUtilisateur)
        {
            Erreur = $"Le nom d'utilisateur ne peut pas dépasser {AuthService.LongueurMaxNomUtilisateur} caractères.";
            return false;
        }

        if (EstCreation && MotDePasse.Length < LongueurMinMotDePasse)
        {
            Erreur = $"Le mot de passe doit contenir au moins {LongueurMinMotDePasse} caractères.";
            return false;
        }

        if (MotDePasse.Length > 0 && MotDePasse.Length < LongueurMinMotDePasse)
        {
            Erreur = $"Le mot de passe doit contenir au moins {LongueurMinMotDePasse} caractères.";
            return false;
        }

        if (MotDePasse.Length > 0 && Confirmation != MotDePasse)
        {
            Erreur = "Les deux mots de passe ne correspondent pas.";
            return false;
        }

        if (Droits.All(x => !x.EstAutorise))
        {
            Erreur = "Attribuez au moins une province à ce compte.";
            return false;
        }

        Erreur = null;
        return true;
    }

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
        OnPropertyChanged(nameof(NomUtilisateurInvalide));
        OnPropertyChanged(nameof(MotDePasseInvalide));
        OnPropertyChanged(nameof(ConfirmationInvalide));
    }
}
