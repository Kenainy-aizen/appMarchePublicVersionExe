using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Enums;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Models;

namespace SIGMPOffline.Services.Dashboard;

/// <inheritdoc />
public class DashboardService : IDashboardService
{
    private readonly IMarcheRepository _marches;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        _marches = unitOfWork.Marches;
    }

    /// <inheritdoc />
    public async Task<DashboardKpi> ObtenirKpiAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        var marches = AppliquerFiltre(await _marches.ListWithExecutionAsync(cancellationToken), filtre);
        var aujourdhui = DateOnly.FromDateTime(DateTime.Today);

        var enRetard = marches
            .Where(m => EstEnRetard(m, aujourdhui))
            .ToList();

        return new DashboardKpi(
            TotalMarches: marches.Count,
            MarchesEnCours: marches.Count(m => m.StatutExecution == StatutExecution.EnCours),
            MarchesClotures: marches.Count(m => m.StatutExecution == StatutExecution.Cloture),
            MarchesEnRetard: enRetard.Count,
            MontantTotalHt: marches.Sum(m => m.MontantHT),
            MontantTotalTtc: marches.Sum(m => m.MontantTTC),
            MontantEnRetard: enRetard.Sum(m => m.MontantHT),
            MarchesNonDemarres: marches.Count(m => m.StatutExecution == StatutExecution.NonDemarre));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlerteRetard>> ListerAlertesAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        var marches = AppliquerFiltre(await _marches.ListWithExecutionAsync(cancellationToken), filtre);
        var aujourdhui = DateOnly.FromDateTime(DateTime.Today);

        return marches
            .Select(m => new
            {
                Marche = m,
                DateLimite = MarcheDelais.CalculerDateLimiteExecution(
                    m.Suivi?.DateOS, m.DelaiExecutionJours, m.Avenants)
            })
            .Where(x => MarcheDelais.EstEnRetard(x.DateLimite, x.Marche.Suivi?.EstCloture ?? false, aujourdhui))
            .Select(x => new AlerteRetard(
                x.Marche.RefMarche,
                x.Marche.ObjetMarche,
                x.DateLimite!.Value,
                aujourdhui.DayNumber - x.DateLimite!.Value.DayNumber))
            .OrderByDescending(a => a.JoursDeRetard)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlerteEcheance>> ListerEcheancesAsync(
        DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        var marches = AppliquerFiltre(await _marches.ListWithExecutionAsync(cancellationToken), filtre);
        var aujourdHui = DateOnly.FromDateTime(DateTime.Today);
        var limite = aujourdHui.AddDays(filtre?.JoursEcheances ?? 30);

        return marches
            .Select(marche => new
            {
                Marche = marche,
                DateLimite = MarcheDelais.CalculerDateLimiteExecution(
                    marche.Suivi?.DateOS, marche.DelaiExecutionJours, marche.Avenants)
            })
            .Where(x => x.DateLimite is not null
                && x.DateLimite.Value >= aujourdHui
                && x.DateLimite.Value <= limite
                && x.Marche.Suivi?.EstCloture != true)
            .Select(x => new AlerteEcheance(
                x.Marche.RefMarche, x.Marche.ObjetMarche, x.DateLimite!.Value,
                x.DateLimite.Value.DayNumber - aujourdHui.DayNumber))
            .OrderBy(x => x.JoursRestants)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MarcheIncomplet>> ListerMarchesIncompletsAsync(
        DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        var marches = AppliquerFiltre(await _marches.ListWithExecutionAsync(cancellationToken), filtre);
        var resultats = new List<MarcheIncomplet>();

        foreach (var marche in marches)
        {
            var suivi = marche.Suivi;
            var anomalies = new List<string>();

            if (suivi?.DateOS is null)
                anomalies.Add("OS absent");
            if (string.IsNullOrWhiteSpace(suivi?.RefTEF))
                anomalies.Add("TEF absent");
            if (suivi?.DateReceptionProvisoire is null && marche.StatutExecution != StatutExecution.NonDemarre)
                anomalies.Add("Réception provisoire absente");
            if (suivi?.DateReceptionDefinitive is null && suivi?.DateReceptionProvisoire is not null)
                anomalies.Add("Réception définitive absente");

            if (anomalies.Count > 0)
                resultats.Add(new MarcheIncomplet(marche.RefMarche, marche.ObjetMarche, anomalies));
        }

        return resultats
            .OrderByDescending(marche => marche.NombreAnomalies)
            .ThenBy(marche => marche.RefMarche)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RepartitionStatut>> ListerRepartitionAsync(
        DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        var marches = AppliquerFiltre(await _marches.ListWithExecutionAsync(cancellationToken), filtre);
        if (marches.Count == 0)
        {
            return [];
        }

        return marches
            .GroupBy(marche => marche.StatutExecution)
            .OrderBy(groupe => groupe.Key)
            .Select(groupe => new RepartitionStatut(
                groupe.Key,
                groupe.Count(),
                Math.Round(groupe.Count() * 100d / marches.Count, 1)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EcheanceMois>> ListerEcheancesParMoisAsync(
        DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        var marches = AppliquerFiltre(await _marches.ListWithExecutionAsync(cancellationToken), filtre);
        var aujourdHui = DateOnly.FromDateTime(DateTime.Today);
        var premierMois = new DateOnly(aujourdHui.Year, aujourdHui.Month, 1);
        var fin = premierMois.AddMonths(3).AddDays(-1);

        var datesLimites = marches
            .Where(marche => marche.Suivi?.EstCloture != true)
            .Select(marche => MarcheDelais.CalculerDateLimiteExecution(
                marche.Suivi?.DateOS, marche.DelaiExecutionJours, marche.Avenants))
            .Where(date => date is not null && date.Value >= aujourdHui && date.Value <= fin)
            .Select(date => date!.Value)
            .ToList();

        return Enumerable.Range(0, 3)
            .Select(decalage =>
            {
                var mois = premierMois.AddMonths(decalage);
                var moisSuivant = mois.AddMonths(1);
                var nombre = datesLimites.Count(date => date >= mois && date < moisSuivant);
                return new EcheanceMois(mois, nombre);
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> ListerExercicesAsync(CancellationToken cancellationToken = default)
        => (await _marches.ListWithExecutionAsync(cancellationToken))
            .Select(marche => marche.Dossier?.PpmDetail?.Entete?.AnneeExercice)
            .Where(annee => annee is not null)
            .Select(annee => annee!.Value)
            .Distinct()
            .OrderByDescending(annee => annee)
            .ToList();

    private static bool EstEnRetard(Domain.Entities.Marche marche, DateOnly aujourdhui)
        => MarcheDelais.EstEnRetard(
            MarcheDelais.CalculerDateLimiteExecution(
                marche.Suivi?.DateOS, marche.DelaiExecutionJours, marche.Avenants),
            marche.Suivi?.EstCloture ?? false,
            aujourdhui);

    private static IReadOnlyList<Domain.Entities.Marche> AppliquerFiltre(
        IReadOnlyList<Domain.Entities.Marche> marches, DashboardFiltre? filtre)
        => marches
            .Where(marche => filtre?.Statut is null || marche.StatutExecution == filtre.Statut)
            .Where(marche => filtre?.AnneeExercice is null
                || marche.Dossier?.PpmDetail?.Entete?.AnneeExercice == filtre.AnneeExercice)
            .ToList();
}
