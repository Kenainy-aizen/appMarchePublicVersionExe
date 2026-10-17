using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Models;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Tableau de bord : indicateurs clés et alertes de dépassement de délai.</summary>
public partial class DashboardViewModel : ViewModelBase, IPageViewModel
{
    private readonly IDashboardService _dashboard;

    public DashboardViewModel(IDashboardService dashboard)
        => _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));

    /// <inheritdoc />
    public string Titre => "Tableau de bord";

    /// <summary>Alertes de retard affichées.</summary>
    public ObservableCollection<AlerteRetard> Alertes { get; } = new();

    [ObservableProperty] private int _totalMarches;
    [ObservableProperty] private int _marchesEnCours;
    [ObservableProperty] private int _marchesClotures;
    [ObservableProperty] private int _marchesEnRetard;
    [ObservableProperty] private decimal _montantTotalHt;
    [ObservableProperty] private bool _estOccupe;
    [ObservableProperty] private string? _erreur;

    /// <summary>Montant total formaté.</summary>
    public string MontantTotalFormate =>
        MontantTotalHt.ToString("N0", CultureInfo.GetCultureInfo("fr-FR")) + " Ar";

    /// <inheritdoc />
    [RelayCommand]
    public async Task ChargerAsync()
    {
        try
        {
            EstOccupe = true;
            Erreur = null;

            var kpi = await _dashboard.ObtenirKpiAsync();
            TotalMarches = kpi.TotalMarches;
            MarchesEnCours = kpi.MarchesEnCours;
            MarchesClotures = kpi.MarchesClotures;
            MarchesEnRetard = kpi.MarchesEnRetard;
            MontantTotalHt = kpi.MontantTotalHt;
            OnPropertyChanged(nameof(MontantTotalFormate));

            Alertes.Clear();
            foreach (var alerte in await _dashboard.ListerAlertesAsync())
            {
                Alertes.Add(alerte);
            }
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
        }
        finally
        {
            EstOccupe = false;
        }
    }
}
