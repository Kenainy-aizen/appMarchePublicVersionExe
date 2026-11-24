using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;
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
    public async Task<DashboardInstantane> ObtenirTableauDeBordAsync(
        DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        // Une seule lecture de la base : le DbContext partagé n'est pas thread-safe,
        // tous les calculs restent donc séquentiels sur la même liste.
        var tousLesMarches = await _marches.ListWithExecutionAsync(cancellationToken);
        var marches = AppliquerFiltre(tousLesMarches, filtre);
        var aujourdhui = DateOnly.FromDateTime(DateTime.Today);
        var joursEcheances = filtre?.JoursEcheances ?? 30;

        return new DashboardInstantane(
            CalculerKpi(marches, aujourdhui),
            CalculerAlertes(marches, aujourdhui),
            CalculerEcheances(marches, aujourdhui, joursEcheances),
            CalculerIncomplets(marches),
            CalculerRepartition(marches),
            CalculerEcheancesParMois(marches, aujourdhui),
            CalculerExercices(tousLesMarches));
    }

    /// <inheritdoc />
    public async Task<DashboardKpi> ObtenirKpiAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        var marches = AppliquerFiltre(await _marches.ListWithExecutionAsync(cancellationToken), filtre);
        return CalculerKpi(marches, DateOnly.FromDateTime(DateTime.Today));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlerteRetard>> ListerAlertesAsync(DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        var marches = AppliquerFiltre(await _marches.ListWithExecutionAsync(cancellationToken), filtre);
        return CalculerAlertes(marches, DateOnly.FromDateTime(DateTime.Today));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlerteEcheance>> ListerEcheancesAsync(
        DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        var marches = AppliquerFiltre(await _marches.ListWithExecutionAsync(cancellationToken), filtre);
        var aujourdHui = DateOnly.FromDateTime(DateTime.Today);
        return CalculerEcheances(marches, aujourdHui, filtre?.JoursEcheances ?? 30);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MarcheIncomplet>> ListerMarchesIncompletsAsync(
        DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        var marches = AppliquerFiltre(await _marches.ListWithExecutionAsync(cancellationToken), filtre);
        return CalculerIncomplets(marches);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RepartitionStatut>> ListerRepartitionAsync(
        DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        var marches = AppliquerFiltre(await _marches.ListWithExecutionAsync(cancellationToken), filtre);
        return CalculerRepartition(marches);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EcheanceMois>> ListerEcheancesParMoisAsync(
        DashboardFiltre? filtre = null, CancellationToken cancellationToken = default)
    {
        var marches = AppliquerFiltre(await _marches.ListWithExecutionAsync(cancellationToken), filtre);
        return CalculerEcheancesParMois(marches, DateOnly.FromDateTime(DateTime.Today));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> ListerExercicesAsync(CancellationToken cancellationToken = default)
        => CalculerExercices(await _marches.ListWithExecutionAsync(cancellationToken));

    private static DashboardKpi CalculerKpi(IReadOnlyList<Marche> marches, DateOnly aujourdhui)
    {
        var enRetard = marches.Count(m => EstEnRetard(m, aujourdhui));

        return new DashboardKpi(
            TotalMarches: marches.Count,
            MarchesEnCours: marches.Count(m => m.StatutExecution == StatutExecution.EnCours),
            MarchesClotures: marches.Count(m => m.StatutExecution == StatutExecution.Cloture),
            MarchesEnRetard: enRetard,
            MarchesNonDemarres: marches.Count(m => m.StatutExecution == StatutExecution.NonDemarre),
            MarchesEnReception: marches.Count(m => m.StatutExecution is
                StatutExecution.ReceptionProvisoire or StatutExecution.ReceptionDefinitive));
    }

    private static IReadOnlyList<AlerteRetard> CalculerAlertes(
        IReadOnlyList<Marche> marches, DateOnly aujourdhui)
        => marches
            .Select(m => new
            {
                Marche = m,
                DateLimite = MarcheDelais.CalculerDateLimiteExecution(
                    m.Suivi?.DateOS, m.DelaiExecutionJours, m.Avenants)
            })
            .Where(x => MarcheDelais.EstEnRetard(
                x.DateLimite, MarcheDelais.EstTermine(x.Marche.StatutExecution), aujourdhui))
            .Select(x => new AlerteRetard(
                x.Marche.RefMarche,
                x.Marche.ObjetMarche,
                x.DateLimite!.Value,
                aujourdhui.DayNumber - x.DateLimite!.Value.DayNumber))
            .OrderByDescending(a => a.JoursDeRetard)
            .ToList();

    private static IReadOnlyList<AlerteEcheance> CalculerEcheances(
        IReadOnlyList<Marche> marches, DateOnly aujourdHui, int joursEcheances)
    {
        var limite = aujourdHui.AddDays(joursEcheances);

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
                && !MarcheDelais.EstTermine(x.Marche.StatutExecution))
            .Select(x => new AlerteEcheance(
                x.Marche.RefMarche, x.Marche.ObjetMarche, x.DateLimite!.Value,
                x.DateLimite.Value.DayNumber - aujourdHui.DayNumber))
            .OrderBy(x => x.JoursRestants)
            .ToList();
    }

    private static IReadOnlyList<MarcheIncomplet> CalculerIncomplets(IReadOnlyList<Marche> marches)
    {
        var resultats = new List<MarcheIncomplet>();

        foreach (var marche in marches)
        {
            // Un marché non démarré, résilié ou annulé n'a pas de suivi à compléter.
            if (marche.StatutExecution is
                StatutExecution.NonDemarre or StatutExecution.Resilie or StatutExecution.Annule)
            {
                continue;
            }

            var suivi = marche.Suivi;
            var anomalies = new List<string>();

            if (suivi?.DateOS is null)
            {
                anomalies.Add("OS absent");
            }

            if (string.IsNullOrWhiteSpace(suivi?.RefTEF))
            {
                anomalies.Add("TEF absent");
            }

            if (marche.StatutExecution is StatutExecution.ReceptionDefinitive or StatutExecution.Cloture
                && suivi?.DateReceptionProvisoire is null)
            {
                anomalies.Add("Réception provisoire absente");
            }

            if (marche.StatutExecution is StatutExecution.Cloture
                && suivi?.DateReceptionDefinitive is null)
            {
                anomalies.Add("Réception définitive absente");
            }

            if (anomalies.Count > 0)
            {
                resultats.Add(new MarcheIncomplet(marche.RefMarche, marche.ObjetMarche, anomalies));
            }
        }

        return resultats
            .OrderByDescending(marche => marche.NombreAnomalies)
            .ThenBy(marche => marche.RefMarche)
            .ToList();
    }

    private static IReadOnlyList<RepartitionStatut> CalculerRepartition(IReadOnlyList<Marche> marches)
    {
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

    private static IReadOnlyList<EcheanceMois> CalculerEcheancesParMois(
        IReadOnlyList<Marche> marches, DateOnly aujourdHui)
    {
        var premierMois = new DateOnly(aujourdHui.Year, aujourdHui.Month, 1);
        var fin = premierMois.AddMonths(3).AddDays(-1);

        var datesLimites = marches
            .Where(marche => !MarcheDelais.EstTermine(marche.StatutExecution))
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

    private static IReadOnlyList<int> CalculerExercices(IReadOnlyList<Marche> marches)
        => marches
            .Select(marche => marche.Dossier?.PpmDetail?.Entete?.AnneeExercice)
            .Where(annee => annee is not null)
            .Select(annee => annee!.Value)
            .Distinct()
            .OrderByDescending(annee => annee)
            .ToList();

    private static bool EstEnRetard(Marche marche, DateOnly aujourdhui)
        => MarcheDelais.EstEnRetard(
            MarcheDelais.CalculerDateLimiteExecution(
                marche.Suivi?.DateOS, marche.DelaiExecutionJours, marche.Avenants),
            MarcheDelais.EstTermine(marche.StatutExecution),
            aujourdhui);

    private static IReadOnlyList<Marche> AppliquerFiltre(
        IReadOnlyList<Marche> marches, DashboardFiltre? filtre)
        => marches
            .Where(marche => filtre?.Statut is null || marche.StatutExecution == filtre.Statut)
            .Where(marche => filtre?.AnneeExercice is null
                || marche.Dossier?.PpmDetail?.Entete?.AnneeExercice == filtre.AnneeExercice)
            .ToList();
}
