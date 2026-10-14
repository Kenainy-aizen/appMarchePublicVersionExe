using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SIGMPOffline.Data.Converters;

/// <summary>
/// Convertit un <see cref="decimal"/> en entier mis à l'échelle (ex. centimes)
/// lors de la persistance SQLite.
///
/// Pourquoi : SQLite ne possède pas de type décimal natif. EF Core le mappe par défaut
/// en TEXT, ce qui casse le tri et les agrégats (<c>ORDER BY</c>, <c>SUM</c>) en SQL.
/// En stockant un entier mis à l'échelle, comparaisons et agrégats restent numériques,
/// tout en conservant l'exactitude décimale (<c>decimal</c> côté C#).
/// </summary>
public sealed class ScaledDecimalConverter : ValueConverter<decimal, long>
{
    /// <param name="scale">Facteur d'échelle (100 = centimes, 1000 = milli-unités).</param>
    public ScaledDecimalConverter(long scale)
        : base(
            // decimal (C#) -> long (SQLite)
            value => (long)Math.Round(value * scale, MidpointRounding.AwayFromZero),
            // long (SQLite) -> decimal (C#)
            stored => stored / (decimal)scale)
    {
    }
}

/// <summary>Convertisseurs décimal partagés par les configurations d'entités.</summary>
public static class DecimalConverters
{
    /// <summary>Montants monétaires — 2 décimales (centimes).</summary>
    public static readonly ScaledDecimalConverter Money = new(100);

    /// <summary>Quantités — 3 décimales (milli-unités), pour les quantités fractionnaires.</summary>
    public static readonly ScaledDecimalConverter Quantity = new(1000);
}
