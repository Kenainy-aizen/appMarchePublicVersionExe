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
    public async Task<DashboardKpi> ObtenirKpiAsync(CancellationToken cancellationToken = default)
    {
        var marches = await _marches.ListWithExecutionAsync(cancellationToken);
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
    public async Task<IReadOnlyList<AlerteRetard>> ListerAlertesAsync(CancellationToken cancellationToken = default)
    {
        var marches = await _marches.ListWithExecutionAsync(cancellationToken);
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
}
