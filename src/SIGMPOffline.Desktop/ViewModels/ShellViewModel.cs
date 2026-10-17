using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// ViewModel du shell principal, affiché une fois la province ouverte : barre
/// latérale de navigation, barre d'en-tête (propriétaire + province active) et
/// page courante.
/// </summary>
public partial class ShellViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly IDialogService _dialogues;

    [ObservableProperty] private NavigationItem _selectedItem;
    [ObservableProperty] private ViewModelBase _currentPage;

    /// <summary>Indique si le thème sombre est actif (pilote l'icône de bascule).</summary>
    [ObservableProperty] private bool _estThemeSombre;

    /// <summary>Nom du responsable PRMP / propriétaire (affiché dans l'en-tête).</summary>
    [ObservableProperty] private string _nomProprietaire = string.Empty;

    /// <summary>Nom de la province actuellement gérée.</summary>
    [ObservableProperty] private string _nomProvince = string.Empty;

    /// <summary>Demande le retour à l'écran de connexion (déconnexion).</summary>
    public event EventHandler? DeconnexionDemandee;

    /// <summary>Entrées du menu latéral (un module par page).</summary>
    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public ShellViewModel(
        IAuthService auth,
        IDialogService dialogues,
        DashboardViewModel dashboard,
        PpmViewModel ppm,
        DossierDaoViewModel dao,
        MarcheViewModel marches,
        SuiviViewModel suivi,
        BackupViewModel backup)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));

        NavigationItems = new List<NavigationItem>
        {
            new("Tableau de bord", Icones.TableauDeBord, dashboard),
            new("Plan de passation (PPM)", Icones.Ppm, ppm),
            new("DAO & Attribution", Icones.Dao, dao),
            new("Marchés", Icones.Marche, marches),
            new("Suivi d'exécution", Icones.Suivi, suivi),
            new("Sauvegarde", Icones.Sauvegarde, backup)
        };

        _selectedItem = NavigationItems[0];
        _currentPage = _selectedItem.ViewModel;

        _estThemeSombre = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
    }

    /// <summary>
    /// Active le shell pour la province ouverte : met à jour l'en-tête et
    /// (re)charge la page initiale.
    /// </summary>
    public async Task ActiverAsync()
    {
        NomProprietaire = _auth.Configuration?.NomProprietairePRMP ?? string.Empty;
        NomProvince = _auth.ProvinceActive?.NomProvince ?? string.Empty;

        var cible = NavigationItems[0];
        if (ReferenceEquals(SelectedItem, cible))
        {
            CurrentPage = cible.ViewModel;
            await ChargerPageAsync(CurrentPage);
        }
        else
        {
            // Le changement de sélection déclenche le chargement de la page.
            SelectedItem = cible;
        }
    }

    /// <summary>Bascule entre le thème clair et le thème sombre.</summary>
    [RelayCommand]
    private void BasculerTheme()
    {
        if (Application.Current is not { } app)
        {
            return;
        }

        var versSombre = app.ActualThemeVariant != ThemeVariant.Dark;
        app.RequestedThemeVariant = versSombre ? ThemeVariant.Dark : ThemeVariant.Light;
        EstThemeSombre = versSombre;
    }

    /// <summary>Crée une nouvelle province avec son propre mot de passe.</summary>
    [RelayCommand]
    private async Task NouvelleProvince()
    {
        var dialogue = new ProvinceDialogViewModel();
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        try
        {
            var province = await _auth.CreerProvinceAsync(dialogue.NomProvince, dialogue.MotDePasse);
            await _dialogues.InformerAsync(
                "Province créée",
                $"La province « {province.NomProvince} » a bien été créée.\n\n"
                + "Pour y accéder, utilisez « Changer de province » et saisissez son mot de passe.");
        }
        catch (Exception ex)
        {
            await _dialogues.InformerAsync("Création impossible", ex.Message);
        }
    }

    /// <summary>Change le mot de passe de la province courante.</summary>
    [RelayCommand]
    private async Task ChangerMotDePasse()
    {
        var province = _auth.ProvinceActive;
        if (province is null)
        {
            return;
        }

        var dialogue = new MotDePasseDialogViewModel(province.NomProvince);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        try
        {
            await _auth.ChangerMotDePasseAsync(
                province.IdProvince, dialogue.AncienMotDePasse, dialogue.NouveauMotDePasse);
            await _dialogues.InformerAsync("Mot de passe modifié", "Le nouveau mot de passe a été enregistré.");
        }
        catch (Exception ex)
        {
            await _dialogues.InformerAsync("Modification impossible", ex.Message);
        }
    }

    /// <summary>Bascule vers une autre province en saisissant son mot de passe.</summary>
    [RelayCommand]
    private async Task ChangerProvince()
    {
        IReadOnlyList<Province> provinces;
        try
        {
            provinces = await _auth.ListerProvincesAsync();
        }
        catch (Exception ex)
        {
            await _dialogues.InformerAsync("Erreur", ex.Message);
            return;
        }

        if (provinces.Count == 0)
        {
            await _dialogues.InformerAsync(
                "Aucune province",
                "Aucune province n'est disponible. Créez-en une avec « Nouvelle province ».");
            return;
        }

        var dialogue = new ChangerProvinceDialogViewModel(provinces, _auth.ProvinceActive?.IdProvince);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        try
        {
            await _auth.ConnecterAsync(dialogue.IdProvinceChoisi, dialogue.MotDePasse);
            await ActiverAsync();
            await _dialogues.InformerAsync(
                "Province changée", $"Vous êtes maintenant connecté à « {_auth.ProvinceActive?.NomProvince} ».");
        }
        catch (Exception ex)
        {
            await _dialogues.InformerAsync("Connexion impossible", ex.Message);
        }
    }

    /// <summary>Ferme la session en cours et revient à l'écran de connexion.</summary>
    [RelayCommand]
    private async Task SeDeconnecter()
    {
        if (!await _dialogues.ConfirmerAsync(
                "Se déconnecter", "Voulez-vous fermer la session en cours ?"))
        {
            return;
        }

        await _auth.DeconnecterAsync();
        DeconnexionDemandee?.Invoke(this, EventArgs.Empty);
    }

    partial void OnSelectedItemChanged(NavigationItem value)
    {
        if (value is null)
        {
            return;
        }

        CurrentPage = value.ViewModel;
        _ = ChargerPageAsync(value.ViewModel);
    }

    private static async Task ChargerPageAsync(ViewModelBase viewModel)
    {
        if (viewModel is IPageViewModel page)
        {
            try
            {
                await page.ChargerAsync();
            }
            catch
            {
                // L'erreur est exposée par la page elle-même (propriété Erreur).
            }
        }
    }
}
