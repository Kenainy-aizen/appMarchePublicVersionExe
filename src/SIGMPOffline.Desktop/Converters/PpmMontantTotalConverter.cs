using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.Converters;

/// <summary>Convertit une collection de lignes PPM en montant estimatif total formaté.</summary>
public sealed class PpmMontantTotalConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable lignes)
        {
            return "0 Ar";
        }

        decimal total = 0m;
        foreach (var ligne in lignes)
        {
            if (ligne is PpmDetail detail)
            {
                total += detail.MontantEstimatif;
            }
        }

        return $"{total:N0} Ar";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
