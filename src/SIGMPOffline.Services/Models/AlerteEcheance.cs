namespace SIGMPOffline.Services.Models;

/// <summary>Marché dont la date limite d'exécution approche.</summary>
public sealed record AlerteEcheance(
    string RefMarche,
    string ObjetMarche,
    DateOnly DateLimite,
    int JoursRestants)
{
    /// <summary>Libellé du délai restant, prêt à afficher.</summary>
    public string DelaiAffiche => JoursRestants switch
    {
        0 => "aujourd'hui",
        1 => "demain",
        _ => $"dans {JoursRestants} j"
    };
}
