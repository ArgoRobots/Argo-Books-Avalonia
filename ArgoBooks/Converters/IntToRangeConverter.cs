using System.Globalization;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that converts an integer to a list of integers from 0 to value-1.
/// Useful for creating dots or progress indicators in ItemsControls.
/// </summary>
public class IntToRangeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count && count > 0)
        {
            return Enumerable.Range(0, count).ToList();
        }
        return new List<int>();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
