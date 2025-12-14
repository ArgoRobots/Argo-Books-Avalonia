using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that returns true if the bound value equals the CompareValue.
/// </summary>
public class StringEqualsConverter : IValueConverter
{
    /// <summary>
    /// The value to compare against.
    /// </summary>
    public string? CompareValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return string.Equals(stringValue, CompareValue, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
