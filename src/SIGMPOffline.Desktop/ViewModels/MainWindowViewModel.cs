using CommunityToolkit.Mvvm.ComponentModel;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// ViewModel du shell principal : menu latéral de navigation et page courante.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private NavigationItem _selectedItem;
    [ObservableProperty] private ViewModelBase _currentPage;

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
            new("Tableau de bord", "📊", dashboard),
            new("Plan de passation (PPM)", "🗂️", ppm),
            new("DAO & Attribution", "📁", dao),
            new("Marchés", "📄", marches),
            new("Suivi d'exécution", "⏱️", suivi),
            new("Sauvegarde", "💾", backup)
        };

        _selectedItem = NavigationItems[0];
        _currentPage = _selectedItem.ViewModel;
    }

    /// <summary>Charge la page initiale (appelé au démarrage).</summary>
    public Task InitialiserAsync() => ActiverAsync(CurrentPage);

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
