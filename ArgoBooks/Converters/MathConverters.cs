using System.Globalization;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Math converters for calculations in XAML bindings.
/// </summary>
public static class MathConverters
{
    /// <summary>
    /// Calculates a percentage of a total width.
    /// Values[0] = percentage (0-100), Values[1] = total width
    /// </summary>
    public static readonly IMultiValueConverter Percentage = new PercentageMultiConverter();
}

/// <summary>
/// Multi-value converter that calculates a percentage of a total width.
/// </summary>
public class PercentageMultiConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return 0.0;

        double percentage = 0;
        double totalWidth = 0;

        if (values[0] is double p)
            percentage = p;
        else if (values[0] is int pi)
            percentage = pi;

        if (values[1] is double w)
            totalWidth = w;
        else if (values[1] is int wi)
            totalWidth = wi;

        if (totalWidth <= 0 || percentage <= 0)
            return 0.0;

        return Math.Max(0, Math.Min(totalWidth, totalWidth * percentage / 100.0));
    }
}
