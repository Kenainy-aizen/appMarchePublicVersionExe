namespace SIGMPOffline.Services.Models;

/// <summary>Informations métier manquantes sur un marché, regroupées par marché.</summary>
/// <param name="RefMarche">Référence du marché.</param>
/// <param name="ObjetMarche">Objet du marché.</param>
/// <param name="Anomalies">Liste des informations manquantes.</param>
public sealed record MarcheIncomplet(
    string RefMarche,
    string ObjetMarche,
    IReadOnlyList<string> Anomalies)
{
    /// <summary>Libellés courts utilisés dans les badges du tableau de bord.</summary>
    public IReadOnlyList<string> AnomaliesCourtes => Anomalies
        .Select(anomalie => anomalie switch
        {
            "Réception provisoire absente" => "RP absente",
            "Réception définitive absente" => "RD absente",
            _ => anomalie
        })
        .ToList();

    /// <summary>Nombre d'informations manquantes.</summary>
    public int NombreAnomalies => Anomalies.Count;

    /// <summary>Résumé lisible de toutes les anomalies.</summary>
    public string Anomalie => string.Join(" · ", Anomalies);
}
