using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArgoBooks.Converters;

/// <summary>
/// Multi-value converter that returns true if both values are equal (object reference or Equals).
/// Used for comparing items in lists to a highlighted/selected item.
/// </summary>
public class ObjectEqualsMultiConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        => values.Count >= 2 && ConverterUtils.AreEqual(values[0], values[1]);
}

/// <summary>
/// Multi-value converter that returns a highlight brush if both values are equal, otherwise transparent.
/// Used for visual highlighting of selected items in lists. Matches the menu-item focus style.
/// </summary>
public class HighlightBrushMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || !ConverterUtils.AreEqual(values[0], values[1]))
            return Brushes.Transparent;

        // Return SurfaceHoverBrush to match menu-item :focus style
        if (Application.Current?.Resources != null &&
            Application.Current.Resources.TryGetResource("SurfaceHoverBrush", Application.Current.ActualThemeVariant, out var resource))
        {
            return resource;
        }
        // Fallback hover color
        return new SolidColorBrush(Color.Parse("#F3F4F6"));
    }
}

/// <summary>
/// Multi-value converter that returns PrimaryBrush if both values are equal, otherwise transparent.
/// Used for the left border highlight on selected items.
/// </summary>
public class HighlightBorderBrushMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || !ConverterUtils.AreEqual(values[0], values[1]))
            return Brushes.Transparent;

        // Return PrimaryBrush to match menu-item :focus border style
        if (Application.Current?.Resources != null &&
            Application.Current.Resources.TryGetResource("PrimaryBrush", Application.Current.ActualThemeVariant, out var resource))
        {
            return resource;
        }
        // Fallback primary color
        return new SolidColorBrush(Color.Parse("#3B82F6"));
    }
}

/// <summary>
/// Multi-value converter that returns a thickness with left border if both values are equal.
/// Used for the left border highlight on selected items.
/// </summary>
public class HighlightBorderThicknessMultiConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        => values.Count >= 2 && ConverterUtils.AreEqual(values[0], values[1])
            ? new Thickness(2, 0, 0, 0)
            : new Thickness(0);
}
