using System.Globalization;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter for checking if two values are equal.
/// Returns true if both values are equal.
/// </summary>
public class PageEqualsConverter : IMultiValueConverter
{
    public static readonly PageEqualsConverter Instance = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return false;

        return Equals(values[0], values[1]);
    }
}
