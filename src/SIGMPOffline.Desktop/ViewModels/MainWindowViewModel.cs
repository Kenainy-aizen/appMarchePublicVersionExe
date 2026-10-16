using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// ViewModel du shell principal : menu latéral de navigation et page courante.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private NavigationItem _selectedItem;
    [ObservableProperty] private ViewModelBase _currentPage;

    /// <summary>Indique si le thème sombre est actif (pilote l'icône de bascule).</summary>
    [ObservableProperty] private bool _estThemeSombre;

    /// <summary>Entrées du menu latéral (un module par page).</summary>
    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public MainWindowViewModel(
        DashboardViewModel dashboard,
        PpmViewModel ppm,
        DossierDaoViewModel dao,
        MarcheViewModel marches,
        SuiviViewModel suivi,
        BackupViewModel backup)
    {
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

    /// <summary>Charge la page initiale (appelé au démarrage).</summary>
    public Task InitialiserAsync() => ActiverAsync(CurrentPage);

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

    partial void OnSelectedItemChanged(NavigationItem value)
    {
        if (value is null)
        {
            return;
        }

        CurrentPage = value.ViewModel;
        _ = ActiverAsync(value.ViewModel);
    }

    private static async Task ActiverAsync(ViewModelBase viewModel)
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
