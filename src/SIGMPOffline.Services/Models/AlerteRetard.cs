namespace SIGMPOffline.Services.Models;

/// <summary>Alerte de dépassement de délai pour un marché.</summary>
/// <param name="RefMarche">Référence du marché.</param>
/// <param name="ObjetMarche">Objet du marché.</param>
/// <param name="DateLimite">Date limite d'exécution dépassée.</param>
/// <param name="JoursDeRetard">Nombre de jours de retard.</param>
public sealed record AlerteRetard(
    string RefMarche,
    string ObjetMarche,
    DateOnly DateLimite,
    int JoursDeRetard);
