using System.Globalization;
using Avalonia.Data.Converters;

namespace SIGMPOffline.Desktop.Converters;

/// <summary>Convertit une valeur d'énumération en son libellé français (voir <see cref="ViewModels.Libelles"/>).</summary>
public sealed class EnumLibelleConverter : IValueConverter
{
    /// <summary>Instance partagée, utilisée comme ressource XAML.</summary>
    public static readonly EnumLibelleConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Enum enumeration ? ViewModels.Libelles.Pour(enumeration) : value;

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
