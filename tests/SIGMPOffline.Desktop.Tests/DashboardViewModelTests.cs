using SIGMPOffline.Desktop.ViewModels;
using SIGMPOffline.Domain.Enums;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Models;

namespace SIGMPOffline.Desktop.Tests;

/// <summary>Tests du ViewModel du tableau de bord.</summary>
public class DashboardViewModelTests
{
    [Fact]
    public async Task Charger_remplit_les_indicateurs_et_limite_les_apercus()
    {
        var vm = new DashboardViewModel(new FakeDashboard());

        await vm.ChargerAsync();

        Assert.Equal(10, vm.TotalMarches);
        Assert.Equal(4, vm.MarchesEnCours);
        Assert.Equal(3, vm.MarchesClotures);
        Assert.Equal(2, vm.MarchesEnRetard);
        Assert.Equal(3, vm.MarchesNonDemarres);
        Assert.Equal("30 %", vm.TauxClotureAffiche);

        Assert.Equal(6, vm.Alertes.Count);
        Assert.Equal(5, vm.AlertesAffichees.Count());
        Assert.True(vm.AlertesLongues);
        Assert.Equal("Voir tout", vm.LibelleAlertes);
        Assert.Contains("(6)", vm.TitreAlertes);

        Assert.StartsWith("Mis à jour", vm.DerniereActualisation);
    }

    [Fact]
    public async Task Les_boutons_voir_tout_affichent_toutes_les_lignes()
    {
        var vm = new DashboardViewModel(new FakeDashboard());
        await vm.ChargerAsync();

        vm.BasculerAlertesCommand.Execute(null);
        vm.BasculerEcheancesCommand.Execute(null);
        vm.BasculerIncompletsCommand.Execute(null);

        Assert.Equal(6, vm.AlertesAffichees.Count());
        Assert.Equal(6, vm.EcheancesAffichees.Count());
        Assert.Equal(6, vm.IncompletsAffiches.Count());
        Assert.Equal("Voir moins", vm.LibelleAlertes);
        Assert.Equal("Voir moins", vm.LibelleEcheances);
        Assert.Equal("Voir moins", vm.LibelleIncomplets);
    }

    [Fact]
    public async Task Les_filtres_alimentent_le_service_avec_la_periode_choisie()
    {
        var service = new FakeDashboard();
        var vm = new DashboardViewModel(service);
        await vm.ChargerAsync();

        vm.PeriodeEcheances = "60 jours";
        await vm.ChargerAsync();

        Assert.Equal(60, vm.NombreJoursEcheances);
        Assert.Contains("60 jours", vm.TitreEcheances);
        Assert.Equal(60, service.DernierFiltre?.JoursEcheances);
    }

    [Fact]
    public void Ouvrir_marche_demande_la_navigation_vers_le_suivi()
    {
        var vm = new DashboardViewModel(new FakeDashboard());
        string? marcheDemandee = null;
        vm.MarcheDemandee += (_, refMarche) => marcheDemandee = refMarche;

        vm.OuvrirMarcheCommand.Execute("M-001");

        Assert.Equal("M-001", marcheDemandee);
    }

    private sealed class FakeDashboard : IDashboardService
    {
        public DashboardFiltre? DernierFiltre { get; private set; }

        public Task<DashboardKpi> ObtenirKpiAsync(
            DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
        {
            DernierFiltre = filtre;
            return Task.FromResult(new DashboardKpi(
                TotalMarches: 10,
                MarchesEnCours: 4,
                MarchesClotures: 3,
                MarchesEnRetard: 2,
                MarchesNonDemarres: 3));
        }

        public Task<IReadOnlyList<AlerteRetard>> ListerAlertesAsync(
            DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AlerteRetard>>(
                Enumerable.Range(1, 6)
                    .Select(index => new AlerteRetard(
                        $"M-{index:000}", $"Objet {index}",
                        DateOnly.FromDateTime(DateTime.Today).AddDays(-index), index))
                    .ToList());

        public Task<IReadOnlyList<AlerteEcheance>> ListerEcheancesAsync(
            DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AlerteEcheance>>(
                Enumerable.Range(1, 6)
                    .Select(index => new AlerteEcheance(
                        $"E-{index:000}", $"Objet {index}",
                        DateOnly.FromDateTime(DateTime.Today).AddDays(index), index))
                    .ToList());

        public Task<IReadOnlyList<MarcheIncomplet>> ListerMarchesIncompletsAsync(
            DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MarcheIncomplet>>(
                Enumerable.Range(1, 6)
                    .Select(index => new MarcheIncomplet(
                        $"I-{index:000}", $"Objet {index}", ["TEF absent", "OS absent"]))
                    .ToList());

        public Task<IReadOnlyList<RepartitionStatut>> ListerRepartitionAsync(
            DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RepartitionStatut>>(
            [
                new(StatutExecution.EnCours, 6, 60),
                new(StatutExecution.Cloture, 4, 40)
            ]);

        public Task<IReadOnlyList<EcheanceMois>> ListerEcheancesParMoisAsync(
            DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
        {
            var premierMois = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
            return Task.FromResult<IReadOnlyList<EcheanceMois>>(
            [
                new(premierMois, 1),
                new(premierMois.AddMonths(1), 2),
                new(premierMois.AddMonths(2), 3)
            ]);
        }

        public Task<IReadOnlyList<int>> ListerExercicesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>([2026]);
    }
}
