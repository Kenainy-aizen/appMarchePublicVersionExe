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
    private readonly ModeLectureService _modeLecture;
    private readonly DashboardViewModel _dashboard;
    private readonly PpmViewModel _ppm;
    private readonly DossierDaoViewModel _dao;
    private readonly MarcheViewModel _marches;
    private readonly SuiviViewModel _suivi;
    private readonly BackupViewModel _backup;
    private readonly GestionComptesViewModel _gestionComptes;
    private Task _chargementPage = Task.CompletedTask;

    [ObservableProperty] private NavigationItem _selectedItem;
    [ObservableProperty] private ViewModelBase _currentPage;

    /// <summary>Indique si le thème sombre est actif (pilote l'icône de bascule).</summary>
    [ObservableProperty] private bool _estThemeSombre;

    /// <summary>Indique si le mode lecture seule est actif.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutGererComptes))]
    private bool _estLectureSeule;

    /// <summary>Indique si le compte courant est l'administrateur PRMP.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutGererComptes))]
    private bool _estAdministrateur;

    /// <summary>Nom du responsable PRMP / propriétaire (affiché dans l'en-tête).</summary>
    [ObservableProperty] private string _nomProprietaire = string.Empty;

    /// <summary>Nom de la province actuellement gérée.</summary>
    [ObservableProperty] private string _nomProvince = string.Empty;

    /// <summary>Identifiant de l'utilisateur connecté.</summary>
    [ObservableProperty] private string _nomUtilisateur = string.Empty;

    /// <summary>Demande le retour à l'écran de connexion (déconnexion).</summary>
    public event EventHandler? DeconnexionDemandee;

    /// <summary>Entrées du menu latéral (un module par page).</summary>
    public IReadOnlyList<NavigationItem> NavigationItems { get; private set; }

    /// <summary>Les comptes en lecture seule ne peuvent pas déverrouiller les écritures.</summary>
    public bool PeutBasculerLectureSeule => _auth.PeutModifier;

    public bool PeutGererComptes => EstAdministrateur && !EstLectureSeule;

    public ShellViewModel(
        IAuthService auth,
        IDialogService dialogues,
        ModeLectureService modeLecture,
        DashboardViewModel dashboard,
        PpmViewModel ppm,
        DossierDaoViewModel dao,
        MarcheViewModel marches,
        SuiviViewModel suivi,
        BackupViewModel backup,
        GestionComptesViewModel gestionComptes)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));
        _modeLecture = modeLecture ?? throw new ArgumentNullException(nameof(modeLecture));
        _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
        _ppm = ppm ?? throw new ArgumentNullException(nameof(ppm));
        _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        _marches = marches ?? throw new ArgumentNullException(nameof(marches));
        _suivi = suivi ?? throw new ArgumentNullException(nameof(suivi));
        _backup = backup ?? throw new ArgumentNullException(nameof(backup));
        _gestionComptes = gestionComptes ?? throw new ArgumentNullException(nameof(gestionComptes));

        NavigationItems = CreerNavigationItems(
            estAdministrateur: false, _dashboard, _ppm, _dao, _marches, _suivi, _backup, _gestionComptes);

        _selectedItem = NavigationItems[0];
        _currentPage = _selectedItem.ViewModel;

        _estThemeSombre = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        _estLectureSeule = _modeLecture.EstActif;
        _modeLecture.PropertyChanged += (_, evenement) =>
        {
            if (evenement.PropertyName == nameof(ModeLectureService.EstActif))
            {
                EstLectureSeule = _modeLecture.EstActif;
            }
        };

        dashboard.MarcheDemandee += async (_, refMarche) =>
        {
            try
            {
                await OuvrirSuiviAsync(refMarche);
            }
            catch (Exception ex)
            {
                await _dialogues.InformerAsync("Navigation impossible", ex.Message);
            }
        };
    }

    /// <summary>
    /// Active le shell pour la province ouverte : met à jour l'en-tête et
    /// (re)charge la page initiale.
    /// </summary>
    public async Task ActiverAsync()
    {
        NomProprietaire = _auth.Configuration?.NomProprietairePRMP ?? string.Empty;
        NomProvince = _auth.ProvinceActive?.NomProvince ?? string.Empty;
        NomUtilisateur = _auth.UtilisateurActif?.NomUtilisateur ?? string.Empty;
        EstAdministrateur = _auth.EstAdministrateur;
        EstLectureSeule = !_auth.PeutModifier;
        NavigationItems = CreerNavigationItems(
            EstAdministrateur, _dashboard, _ppm, _dao, _marches, _suivi, _backup, _gestionComptes);
        OnPropertyChanged(nameof(NavigationItems));
        OnPropertyChanged(nameof(PeutBasculerLectureSeule));
        OnPropertyChanged(nameof(PeutGererComptes));

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

    /// <summary>Crée une nouvelle province, réservée à l'administrateur PRMP.</summary>
    [RelayCommand]
    private async Task NouvelleProvince()
    {
        if (!EstAdministrateur || EstLectureSeule)
        {
            return;
        }

        var dialogue = new ProvinceDialogViewModel();
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        try
        {
            var province = await _auth.CreerProvinceAsync(dialogue.NomProvince);
            await _dialogues.InformerAsync(
                "Province créée",
                $"La province « {province.NomProvince} » a bien été créée.\n\n"
                + "Vous pouvez maintenant attribuer son accès aux comptes dans « Comptes & permissions ».");
        }
        catch (Exception ex)
        {
            await _dialogues.InformerAsync("Création impossible", ex.Message);
        }
    }

    /// <summary>Change le mot de passe du compte actuellement connecté.</summary>
    [RelayCommand]
    private async Task ChangerMotDePasse()
    {
        var utilisateur = _auth.UtilisateurActif;
        if (utilisateur is null)
        {
            return;
        }

        var dialogue = new MotDePasseDialogViewModel(utilisateur.NomUtilisateur);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        try
        {
            await _auth.ChangerMotDePasseAsync(dialogue.AncienMotDePasse, dialogue.NouveauMotDePasse);
            await _dialogues.InformerAsync("Mot de passe modifié", "Le nouveau mot de passe a été enregistré.");
        }
        catch (Exception ex)
        {
            await _dialogues.InformerAsync("Modification impossible", ex.Message);
        }
    }

    /// <summary>Modifie le nom du responsable PRMP affiché dans l'en-tête.</summary>
    [RelayCommand]
    private async Task ModifierNomPrmp()
    {
        if (!EstAdministrateur || EstLectureSeule)
        {
            return;
        }

        var dialogue = new NomPrmpDialogViewModel(NomProprietaire);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        try
        {
            NomProprietaire = await _auth.ModifierNomProprietaireAsync(dialogue.VersNomProprietaire());
            await _dialogues.InformerAsync(
                "Nom de la PRMP modifié", "Le nouveau nom a été enregistré.");
        }
        catch (Exception ex)
        {
            await _dialogues.InformerAsync("Modification impossible", ex.Message);
        }
    }

    /// <summary>Bascule vers une autre province autorisée au compte.</summary>
    [RelayCommand]
    private async Task ChangerProvince()
    {
        IReadOnlyList<Province> provinces;
        try
        {
            provinces = await _auth.ListerProvincesAutoriseesAsync();
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
                "Aucune province n'est attribuée à ce compte. Contactez l'administrateur PRMP.");
            return;
        }

        var dialogue = new ChangerProvinceDialogViewModel(provinces, _auth.ProvinceActive?.IdProvince);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        try
        {
            await _auth.ChangerProvinceAsync(dialogue.IdProvinceChoisi);
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

    partial void OnEstLectureSeuleChanged(bool value)
    {
        _modeLecture.EstActif = value;
    }

    private static IReadOnlyList<NavigationItem> CreerNavigationItems(
        bool estAdministrateur,
        DashboardViewModel dashboard,
        PpmViewModel ppm,
        DossierDaoViewModel dao,
        MarcheViewModel marches,
        SuiviViewModel suivi,
        BackupViewModel backup,
        GestionComptesViewModel gestionComptes)
    {
        var items = new List<NavigationItem>
        {
            new("Tableau de bord", Icones.TableauDeBord, dashboard),
            new("Plan de passation (PPM)", Icones.Ppm, ppm),
            new("DAO & Attribution", Icones.Dao, dao),
            new("Marchés", Icones.Marche, marches),
            new("Suivi d'exécution", Icones.Suivi, suivi)
        };

        if (estAdministrateur)
        {
            items.Add(new NavigationItem("Comptes & permissions", Icones.Utilisateur, gestionComptes));
            items.Add(new NavigationItem("Sauvegarde", Icones.Sauvegarde, backup));
        }

        return items;
    }

    partial void OnSelectedItemChanged(NavigationItem value)
    {
        if (value is null)
        {
            return;
        }

        CurrentPage = value.ViewModel;

        // Les chargements de page sont enchaînés pour garder un ordre déterministe
        // et éviter une rafale de requêtes lors d'une navigation rapide.
        var precedent = _chargementPage;
        _chargementPage = ChargerPageEnSerieAsync(precedent, value.ViewModel);
    }

    private static async Task ChargerPageEnSerieAsync(Task precedent, ViewModelBase viewModel)
    {
        try
        {
            await precedent;
        }
        catch
        {
            // L'erreur a déjà été exposée par la page concernée.
        }

        await ChargerPageAsync(viewModel);
    }

    private async Task OuvrirSuiviAsync(string refMarche)
    {
        var cible = NavigationItems.FirstOrDefault(item => item.ViewModel is SuiviViewModel);
        if (cible?.ViewModel is not SuiviViewModel suivi)
        {
            return;
        }

        if (!ReferenceEquals(SelectedItem, cible))
        {
            SelectedItem = cible;
            await _chargementPage;
        }
        else
        {
            await ChargerPageAsync(suivi);
        }

        await suivi.OuvrirMarcheParReferenceAsync(refMarche);
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
