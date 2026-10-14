using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Domain.Common;

/// <summary>
/// Calculs métier liés aux délais d'exécution d'un marché.
/// Code pur (sans dépendance), donc directement testable.
/// </summary>
public static class MarcheDelais
{
    /// <summary>
    /// Calcule la date limite d'exécution d'un marché :
    /// <c>DateOS + DelaiExecutionJours + Σ(prolongations des avenants)</c>.
    /// </summary>
    /// <param name="dateOs">Date de notification de l'Ordre de Service (null si non émis).</param>
    /// <param name="delaiExecutionJours">Délai contractuel en jours.</param>
    /// <param name="avenants">Avenants du marché (peut contenir des prolongations négatives).</param>
    /// <returns>La date limite, ou <c>null</c> si l'OS n'a pas encore été émis.</returns>
    public static DateOnly? CalculerDateLimiteExecution(
        DateOnly? dateOs,
        int delaiExecutionJours,
        IEnumerable<Avenant>? avenants = null)
    {
        if (dateOs is null)
        {
            return null;
        }

        var prolongation = avenants?.Sum(a => a.ProlongationDelaiJours) ?? 0;

        return dateOs.Value.AddDays(delaiExecutionJours + prolongation);
    }

    /// <summary>
    /// Indique si un marché est en retard : date limite dépassée et marché non clôturé.
    /// </summary>
    public static bool EstEnRetard(DateOnly? dateLimite, bool estCloture, DateOnly aujourdhui)
        => dateLimite is not null && !estCloture && aujourdhui > dateLimite.Value;

    /// <summary>Nombre de jours restants (négatif si dépassé), ou <c>null</c> si pas de date limite.</summary>
    public static int? JoursRestants(DateOnly? dateLimite, DateOnly aujourdhui)
        => dateLimite is null ? null : dateLimite.Value.DayNumber - aujourdhui.DayNumber;
}
