using System.Globalization;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that converts a boolean to an angle for rotation animations.
/// </summary>
public class BoolToAngleConverter : IValueConverter
{
    public double TrueAngle { get; set; } = 90;
    public double FalseAngle { get; set; } = 0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? TrueAngle : FalseAngle;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
