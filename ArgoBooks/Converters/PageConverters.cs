using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

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

/// <summary>
/// Converter that returns primary background color if page equals current page, else transparent.
/// </summary>
public class PageActiveBackgroundConverter : IMultiValueConverter
{
    public static readonly PageActiveBackgroundConverter Instance = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return Brushes.Transparent;

        var isActive = Equals(values[0], values[1]);
        if (isActive)
        {
            if (Application.Current?.Resources != null &&
                Application.Current.Resources.TryGetResource("PrimaryBrush", Application.Current.ActualThemeVariant, out var resource) &&
                resource is IBrush brush)
            {
                return brush;
            }
            return new SolidColorBrush(Color.Parse("#3B82F6"));
        }
        return Brushes.Transparent;
    }
}
