using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that returns true if the bound value equals the CompareValue.
/// Optionally returns TrueValue/FalseValue instead of bool.
/// </summary>
public class StringEqualsConverter : IValueConverter
{
    /// <summary>
    /// The value to compare against.
    /// </summary>
    public string? CompareValue { get; set; }

    /// <summary>
    /// Optional value to return when the comparison is true.
    /// </summary>
    public object? TrueValue { get; set; }

    /// <summary>
    /// Optional value to return when the comparison is false.
    /// </summary>
    public object? FalseValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isEqual = false;
        if (value is string stringValue)
        {
            isEqual = string.Equals(stringValue, CompareValue, StringComparison.OrdinalIgnoreCase);
        }

        // If TrueValue/FalseValue are set, return those instead of bool
        if (TrueValue != null || FalseValue != null)
        {
            var result = isEqual ? TrueValue : FalseValue;

            // Handle Thickness conversion for BorderThickness
            if (targetType == typeof(Thickness) && result is string thicknessStr)
            {
                if (double.TryParse(thicknessStr, out var thickness))
                {
                    return new Thickness(thickness);
                }
            }

            return result;
        }

        return isEqual;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
