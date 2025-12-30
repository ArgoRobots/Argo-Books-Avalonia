using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArgoBooks.Converters;

/// <summary>
/// Converts a department color name (blue, green, etc.) to a background brush.
/// </summary>
public class DepartmentColorToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var colorName = value as string ?? "blue";

        return colorName.ToLowerInvariant() switch
        {
            "blue" => new SolidColorBrush(Color.Parse("#dbeafe")),
            "green" => new SolidColorBrush(Color.Parse("#dcfce7")),
            "yellow" => new SolidColorBrush(Color.Parse("#fef3c7")),
            "purple" => new SolidColorBrush(Color.Parse("#f3e8ff")),
            "red" => new SolidColorBrush(Color.Parse("#fee2e2")),
            "cyan" => new SolidColorBrush(Color.Parse("#cffafe")),
            "orange" => new SolidColorBrush(Color.Parse("#ffedd5")),
            "pink" => new SolidColorBrush(Color.Parse("#fce7f3")),
            _ => new SolidColorBrush(Color.Parse("#dbeafe")) // default to blue
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
