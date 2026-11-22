using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Models;
using SIGMPOffline.Domain.Enums;

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
    public ObservableCollection<AlerteEcheance> Echeances { get; } = new();
    public ObservableCollection<MarcheIncomplet> MarchesIncomplets { get; } = new();

    [ObservableProperty] private int _totalMarches;
    [ObservableProperty] private int _marchesEnCours;
    [ObservableProperty] private int _marchesClotures;
    [ObservableProperty] private int _marchesEnRetard;
    [ObservableProperty] private decimal _montantTotalHt;
    [ObservableProperty] private bool _estOccupe;
    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private string _exerciceFiltre = "Tous";
    [ObservableProperty] private string _statutFiltre = "Tous";

    public ObservableCollection<string> ExercicesDisponibles { get; } = new() { "Tous" };
    public IReadOnlyList<string> StatutsDisponibles { get; } = new[]
        { "Tous", "Non démarré", "En cours", "Réception provisoire", "Réception définitive", "Clôturé", "Résilié", "Annulé" };

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

            var filtre = ConstruireFiltre();
            var kpi = await _dashboard.ObtenirKpiAsync(filtre);
            TotalMarches = kpi.TotalMarches;
            MarchesEnCours = kpi.MarchesEnCours;
            MarchesClotures = kpi.MarchesClotures;
            MarchesEnRetard = kpi.MarchesEnRetard;
            MontantTotalHt = kpi.MontantTotalHt;
            OnPropertyChanged(nameof(MontantTotalFormate));

            Alertes.Clear();
            foreach (var alerte in await _dashboard.ListerAlertesAsync(filtre))
            {
                Alertes.Add(alerte);
            }

            Echeances.Clear();
            foreach (var echeance in await _dashboard.ListerEcheancesAsync(filtre))
                Echeances.Add(echeance);

            MarchesIncomplets.Clear();
            foreach (var marche in await _dashboard.ListerMarchesIncompletsAsync(filtre))
                MarchesIncomplets.Add(marche);

            var exercices = await _dashboard.ListerExercicesAsync();
            ExercicesDisponibles.Clear();
            ExercicesDisponibles.Add("Tous");
            foreach (var exercice in exercices)
                ExercicesDisponibles.Add(exercice.ToString());
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

    [RelayCommand]
    private async Task ReinitialiserFiltres()
    {
        ExerciceFiltre = "Tous";
        StatutFiltre = "Tous";
        await ChargerAsync();
    }

    partial void OnExerciceFiltreChanged(string value) => _ = ChargerAsync();
    partial void OnStatutFiltreChanged(string value) => _ = ChargerAsync();

    private DashboardFiltre? ConstruireFiltre()
    {
        int? exercice = int.TryParse(ExerciceFiltre, out var annee) ? annee : null;
        var statut = StatutsDisponibles.Contains(StatutFiltre)
            ? OptionsEnums.Statuts.FirstOrDefault(option => option.Libelle == StatutFiltre)?.Valeur
            : null;
        return exercice is null && statut is null ? null : new DashboardFiltre(exercice, statut);
    }
}
