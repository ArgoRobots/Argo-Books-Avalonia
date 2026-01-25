using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that returns PrimaryBrush if value equals CompareValue, otherwise BorderBrush.
/// </summary>
public class ThemeBorderBrushConverter : IValueConverter
{
    /// <summary>
    /// The value to compare against.
    /// </summary>
    public string? CompareValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isSelected = false;
        if (value is string stringValue)
        {
            isSelected = string.Equals(stringValue, CompareValue, StringComparison.OrdinalIgnoreCase);
        }

        // Get the appropriate brush from Application resources
        if (Application.Current?.Resources != null)
        {
            var resourceKey = isSelected ? "PrimaryBrush" : "BorderBrush";
            if (Application.Current.Resources.TryGetResource(resourceKey, Application.Current.ActualThemeVariant, out var resource))
            {
                return resource;
            }
        }

        // Fallback colors
        return isSelected
            ? new SolidColorBrush(Color.Parse("#3B82F6"))
            : new SolidColorBrush(Color.Parse("#E5E7EB"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Multi-value converter that returns PrimaryBrush if the selected theme matches the compare value.
/// Binds to both SelectedTheme and SelectedAccentColor so it updates when either changes.
/// Values[0] = SelectedTheme, Values[1] = SelectedAccentColor (used to trigger re-evaluation).
/// Parameter = the theme value to compare against (e.g., "Light", "Dark", "System").
/// </summary>
public class ThemeBorderBrushMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 1)
            return GetBorderBrush();

        var selectedTheme = values[0] as string;
        var compareValue = parameter as string;

        // Values[1] is SelectedAccentColor - we don't use its value directly,
        // but binding to it ensures this converter re-runs when accent color changes

        bool isSelected = string.Equals(selectedTheme, compareValue, StringComparison.OrdinalIgnoreCase);

        // Get the appropriate brush from Application resources
        if (Application.Current?.Resources != null)
        {
            var resourceKey = isSelected ? "PrimaryBrush" : "BorderBrush";
            if (Application.Current.Resources.TryGetResource(resourceKey, Application.Current.ActualThemeVariant, out var resource))
            {
                return resource;
            }
        }

        // Fallback colors
        return isSelected
            ? new SolidColorBrush(Color.Parse("#3B82F6"))
            : new SolidColorBrush(Color.Parse("#E5E7EB"));
    }

    private static IBrush GetBorderBrush()
    {
        if (Application.Current?.Resources != null &&
            Application.Current.Resources.TryGetResource("BorderBrush", Application.Current.ActualThemeVariant, out var resource))
        {
            return resource as IBrush ?? new SolidColorBrush(Color.Parse("#E5E7EB"));
        }
        return new SolidColorBrush(Color.Parse("#E5E7EB"));
    }
}
