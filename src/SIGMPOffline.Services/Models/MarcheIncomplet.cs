namespace SIGMPOffline.Services.Models;

/// <summary>Information métier manquante sur un marché.</summary>
public sealed record MarcheIncomplet(
    string RefMarche,
    string ObjetMarche,
    string Anomalie);
