namespace SIGMPOffline.Services.Models;

/// <summary>Marché dont la date limite d'exécution approche.</summary>
public sealed record AlerteEcheance(
    string RefMarche,
    string ObjetMarche,
    DateOnly DateLimite,
    int JoursRestants);
