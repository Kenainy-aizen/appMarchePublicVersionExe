using System.Globalization;

namespace SIGMPOffline.Services.Models;

/// <summary>Nombre d'échéances d'exécution pour un mois donné.</summary>
/// <param name="Mois">Premier jour du mois concerné.</param>
/// <param name="Nombre">Nombre de marchés dont la date limite tombe dans ce mois.</param>
public sealed record EcheanceMois(DateOnly Mois, int Nombre)
{
    private static readonly CultureInfo CultureFrancaise = CultureInfo.GetCultureInfo("fr-FR");

    /// <summary>Libellé court du mois, par exemple « déc. 2026 ».</summary>
    public string Libelle => Mois.ToString("MMM yyyy", CultureFrancaise);
}
