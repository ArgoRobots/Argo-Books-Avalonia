using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter for binding decimal properties to TextBox.Text.
/// Handles empty strings by returning 0 for the decimal property.
/// Shows empty string when decimal is 0.
/// </summary>
public class NullableDecimalConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance for use in XAML bindings.
    /// </summary>
    public static readonly NullableDecimalConverter Instance = new();

    /// <summary>
    /// Converts a decimal to string for display.
    /// Shows "0.00" format for zero values.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            return d.ToString("0.00", culture);
        }
        return "0.00";
    }

    /// <summary>
    /// Converts a string to decimal for binding.
    /// Returns 0 if string is null or empty.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return 0m;
            }
            if (decimal.TryParse(s, NumberStyles.Any, culture, out var result))
            {
                return result;
            }
        }
        return 0m;
    }
}
