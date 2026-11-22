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

        var enRetard = marches.Count(m =>
            MarcheDelais.EstEnRetard(
                MarcheDelais.CalculerDateLimiteExecution(m.Suivi?.DateOS, m.DelaiExecutionJours, m.Avenants),
                m.Suivi?.EstCloture ?? false,
                aujourdhui));

        return new DashboardKpi(
            TotalMarches: marches.Count,
            MarchesEnCours: marches.Count(m => m.StatutExecution == StatutExecution.EnCours),
            MarchesClotures: marches.Count(m => m.StatutExecution == StatutExecution.Cloture),
            MarchesEnRetard: enRetard,
            MontantTotalHt: marches.Sum(m => m.MontantHT));
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
        var limite = aujourdHui.AddDays(30);

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
            if (suivi?.DateOS is null)
                resultats.Add(new(marche.RefMarche, marche.ObjetMarche, "Ordre de service absent"));
            if (string.IsNullOrWhiteSpace(suivi?.RefTEF))
                resultats.Add(new(marche.RefMarche, marche.ObjetMarche, "TEF absent"));
            if (suivi?.DateReceptionProvisoire is null && marche.StatutExecution != StatutExecution.NonDemarre)
                resultats.Add(new(marche.RefMarche, marche.ObjetMarche, "Réception provisoire absente"));
            if (suivi?.DateReceptionDefinitive is null && suivi?.DateReceptionProvisoire is not null)
                resultats.Add(new(marche.RefMarche, marche.ObjetMarche, "Réception définitive absente"));
        }

        return resultats;
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

    private static IReadOnlyList<Domain.Entities.Marche> AppliquerFiltre(
        IReadOnlyList<Domain.Entities.Marche> marches, DashboardFiltre? filtre)
        => marches
            .Where(marche => filtre?.Statut is null || marche.StatutExecution == filtre.Statut)
            .Where(marche => filtre?.AnneeExercice is null
                || marche.Dossier?.PpmDetail?.Entete?.AnneeExercice == filtre.AnneeExercice)
            .ToList();
}
