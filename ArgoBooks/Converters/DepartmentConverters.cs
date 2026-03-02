using System.Globalization;
using ArgoBooks.Core;
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
            "blue" => new SolidColorBrush(Color.Parse(AppColors.PrimaryLight)),
            "green" => new SolidColorBrush(Color.Parse(AppColors.SuccessLight)),
            "yellow" => new SolidColorBrush(Color.Parse(AppColors.WarningLight)),
            "purple" => new SolidColorBrush(Color.Parse(AppColors.PurpleLight)),
            "red" => new SolidColorBrush(Color.Parse(AppColors.ErrorLight)),
            "cyan" => new SolidColorBrush(Color.Parse(AppColors.CyanLight)),
            "orange" => new SolidColorBrush(Color.Parse(AppColors.OrangeLight)),
            "pink" => new SolidColorBrush(Color.Parse(AppColors.PinkLight)),
            _ => new SolidColorBrush(Color.Parse(AppColors.PrimaryLight)) // default to blue
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
