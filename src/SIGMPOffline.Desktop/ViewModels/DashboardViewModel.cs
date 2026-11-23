using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Domain.Enums;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Models;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Tableau de bord : indicateurs clés et alertes de dépassement de délai.</summary>
public partial class DashboardViewModel : ViewModelBase, IPageViewModel
{
    private const int TailleApercu = 5;
    private static readonly CultureInfo CultureFrancaise = CultureInfo.GetCultureInfo("fr-FR");

    private readonly IDashboardService _dashboard;
    private readonly SemaphoreSlim _chargement = new(1, 1);
    private bool _suspendreChargement;

    public DashboardViewModel(IDashboardService dashboard)
        => _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));

    /// <summary>Demande l'ouverture d'un marché dans le module Suivi d'exécution.</summary>
    public event EventHandler<string>? MarcheDemandee;

    /// <inheritdoc />
    public string Titre => "Tableau de bord";

    /// <summary>Alertes de retard affichées.</summary>
    public ObservableCollection<AlerteRetard> Alertes { get; } = new();

    /// <summary>Échéances proches affichées.</summary>
    public ObservableCollection<AlerteEcheance> Echeances { get; } = new();

    /// <summary>Marchés avec des informations de suivi manquantes.</summary>
    public ObservableCollection<MarcheIncomplet> MarchesIncomplets { get; } = new();

    /// <summary>Répartition des marchés par statut.</summary>
    public ObservableCollection<RepartitionStatut> Repartition { get; } = new();

    /// <summary>Échéances des trois prochains mois.</summary>
    public ObservableCollection<EcheanceMois> EcheancesMois { get; } = new();

    [ObservableProperty] private int _totalMarches;
    [ObservableProperty] private int _marchesEnCours;
    [ObservableProperty] private int _marchesClotures;
    [ObservableProperty] private int _marchesEnRetard;
    [ObservableProperty] private int _marchesNonDemarres;
    [ObservableProperty] private decimal _tauxCloture;
    [ObservableProperty] private bool _estOccupe;
    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private string _exerciceFiltre = "Tous";
    [ObservableProperty] private string _statutFiltre = "Tous";
    [ObservableProperty] private string _periodeEcheances = "30 jours";
    [ObservableProperty] private string? _derniereActualisation;
    [ObservableProperty] private bool _afficherToutesAlertes;
    [ObservableProperty] private bool _afficherToutesEcheances;
    [ObservableProperty] private bool _afficherTousIncomplets;

    public ObservableCollection<string> ExercicesDisponibles { get; } = new() { "Tous" };

    public IReadOnlyList<string> StatutsDisponibles { get; } = new[]
        { "Tous", "Non démarré", "En cours", "Réception provisoire", "Réception définitive", "Clôturé", "Résilié", "Annulé" };

    public IReadOnlyList<string> PeriodesDisponibles { get; } = new[] { "7 jours", "30 jours", "60 jours", "90 jours" };

    /// <summary>Taux de clôture formaté.</summary>
    public string TauxClotureAffiche => $"{TauxCloture.ToString("0.#", CultureFrancaise)} %";

    /// <summary>Taux de clôture utilisable par une barre de progression.</summary>
    public double TauxClotureValeur => (double)TauxCloture;

    /// <summary>Nombre de jours de la fenêtre d'échéances.</summary>
    public int NombreJoursEcheances
        => int.TryParse(PeriodeEcheances.Split(' ')[0], out var jours) ? jours : 30;

    public string TitreAlertes => $"Marchés en retard ({Alertes.Count})";

    public string TitreEcheances => $"Échéances à {NombreJoursEcheances} jours ({Echeances.Count})";

    public string TitreIncomplets => $"Données de suivi à compléter ({MarchesIncomplets.Count})";

    public IEnumerable<AlerteRetard> AlertesAffichees
        => AfficherToutesAlertes ? Alertes : Alertes.Take(TailleApercu);

    public IEnumerable<AlerteEcheance> EcheancesAffichees
        => AfficherToutesEcheances ? Echeances : Echeances.Take(TailleApercu);

    public IEnumerable<MarcheIncomplet> IncompletsAffiches
        => AfficherTousIncomplets ? MarchesIncomplets : MarchesIncomplets.Take(TailleApercu);

    public bool AlertesLongues => Alertes.Count > TailleApercu;

    public bool EcheancesLongues => Echeances.Count > TailleApercu;

    public bool IncompletsLongs => MarchesIncomplets.Count > TailleApercu;

    public string LibelleAlertes => AfficherToutesAlertes ? "Voir moins" : "Voir tout";

    public string LibelleEcheances => AfficherToutesEcheances ? "Voir moins" : "Voir tout";

    public string LibelleIncomplets => AfficherTousIncomplets ? "Voir moins" : "Voir tout";

    /// <summary>Échelle maximale des barres du graphique mensuel.</summary>
    public int MaximumEcheancesMois => EcheancesMois.Count == 0
        ? 1
        : Math.Max(1, EcheancesMois.Max(mois => mois.Nombre));

    /// <inheritdoc />
    [RelayCommand]
    public async Task ChargerAsync()
    {
        await _chargement.WaitAsync();
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
            MarchesNonDemarres = kpi.MarchesNonDemarres;
            TauxCloture = kpi.TauxCloture;

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

            Repartition.Clear();
            foreach (var ligne in await _dashboard.ListerRepartitionAsync(filtre))
                Repartition.Add(ligne);

            EcheancesMois.Clear();
            foreach (var mois in await _dashboard.ListerEcheancesParMoisAsync(filtre))
                EcheancesMois.Add(mois);

            var exercices = await _dashboard.ListerExercicesAsync();
            var libellesExercices = new List<string> { "Tous" };
            libellesExercices.AddRange(exercices.Select(exercice => exercice.ToString()));
            if (!ExercicesDisponibles.SequenceEqual(libellesExercices))
            {
                _suspendreChargement = true;
                try
                {
                    ExercicesDisponibles.Clear();
                    foreach (var libelle in libellesExercices)
                        ExercicesDisponibles.Add(libelle);

                    if (!ExercicesDisponibles.Contains(ExerciceFiltre))
                        ExerciceFiltre = "Tous";
                }
                finally
                {
                    _suspendreChargement = false;
                }
            }

            DerniereActualisation = $"Mis à jour à {DateTime.Now:HH:mm}";
            NotifierSections();
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
        }
        finally
        {
            EstOccupe = false;
            _chargement.Release();
        }
    }

    [RelayCommand]
    private async Task ReinitialiserFiltres()
    {
        _suspendreChargement = true;
        try
        {
            ExerciceFiltre = "Tous";
            StatutFiltre = "Tous";
            PeriodeEcheances = "30 jours";
        }
        finally
        {
            _suspendreChargement = false;
        }

        await ChargerAsync();
    }

    [RelayCommand]
    private void OuvrirMarche(string? refMarche)
    {
        if (!string.IsNullOrWhiteSpace(refMarche))
        {
            MarcheDemandee?.Invoke(this, refMarche);
        }
    }

    [RelayCommand]
    private void BasculerAlertes() => AfficherToutesAlertes = !AfficherToutesAlertes;

    [RelayCommand]
    private void BasculerEcheances() => AfficherToutesEcheances = !AfficherToutesEcheances;

    [RelayCommand]
    private void BasculerIncomplets() => AfficherTousIncomplets = !AfficherTousIncomplets;

    partial void OnExerciceFiltreChanged(string value)
    {
        if (!_suspendreChargement)
        {
            _ = ChargerAsync();
        }
    }

    partial void OnStatutFiltreChanged(string value)
    {
        if (!_suspendreChargement)
        {
            _ = ChargerAsync();
        }
    }

    partial void OnPeriodeEcheancesChanged(string value)
    {
        OnPropertyChanged(nameof(NombreJoursEcheances));
        OnPropertyChanged(nameof(TitreEcheances));
        if (!_suspendreChargement)
        {
            _ = ChargerAsync();
        }
    }

    partial void OnAfficherToutesAlertesChanged(bool value)
    {
        OnPropertyChanged(nameof(AlertesAffichees));
        OnPropertyChanged(nameof(LibelleAlertes));
    }

    partial void OnAfficherToutesEcheancesChanged(bool value)
    {
        OnPropertyChanged(nameof(EcheancesAffichees));
        OnPropertyChanged(nameof(LibelleEcheances));
    }

    partial void OnAfficherTousIncompletsChanged(bool value)
    {
        OnPropertyChanged(nameof(IncompletsAffiches));
        OnPropertyChanged(nameof(LibelleIncomplets));
    }

    private DashboardFiltre ConstruireFiltre()
    {
        int? exercice = int.TryParse(ExerciceFiltre, out var annee) ? annee : null;
        var statut = StatutsDisponibles.Contains(StatutFiltre)
            ? OptionsEnums.Statuts.FirstOrDefault(option => option.Libelle == StatutFiltre)?.Valeur
            : null;
        return new DashboardFiltre(exercice, statut, NombreJoursEcheances);
    }

    private void NotifierSections()
    {
        OnPropertyChanged(nameof(TauxClotureAffiche));
        OnPropertyChanged(nameof(TauxClotureValeur));
        OnPropertyChanged(nameof(TitreAlertes));
        OnPropertyChanged(nameof(TitreEcheances));
        OnPropertyChanged(nameof(TitreIncomplets));
        OnPropertyChanged(nameof(AlertesAffichees));
        OnPropertyChanged(nameof(EcheancesAffichees));
        OnPropertyChanged(nameof(IncompletsAffiches));
        OnPropertyChanged(nameof(AlertesLongues));
        OnPropertyChanged(nameof(EcheancesLongues));
        OnPropertyChanged(nameof(IncompletsLongs));
        OnPropertyChanged(nameof(LibelleAlertes));
        OnPropertyChanged(nameof(LibelleEcheances));
        OnPropertyChanged(nameof(LibelleIncomplets));
        OnPropertyChanged(nameof(MaximumEcheancesMois));
    }
}
