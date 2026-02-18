using System.Globalization;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Converts a boolean (isPercent) to "$" or "%" display text.
/// </summary>
public class PercentDollarConverter : IValueConverter
{
    public static readonly PercentDollarConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "%" : "$";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is "%" or "%";
    }
}
