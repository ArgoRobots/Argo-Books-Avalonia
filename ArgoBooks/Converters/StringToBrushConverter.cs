using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that converts a color hex string to a SolidColorBrush.
/// Optional parameter for opacity (0-1).
/// </summary>
public class StringToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string colorHex || string.IsNullOrEmpty(colorHex))
            return new SolidColorBrush(Colors.Gray);

        try
        {
            var color = Color.Parse(colorHex);

            // Apply opacity if parameter is provided
            if (parameter is string opacityStr && double.TryParse(opacityStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var opacity))
            {
                color = Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B);
            }

            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(Colors.Gray);
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
