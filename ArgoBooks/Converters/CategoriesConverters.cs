using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that converts a color hex string to a SolidColorBrush.
/// Optional parameter for opacity (0-1).
/// </summary>
public class StringToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
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

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Bool converters for the Categories page.
/// </summary>
public static class BoolConverters
{
    /// <summary>
    /// Converts bool to "Expenses" (true) or "Revenue" (false).
    /// </summary>
    public static readonly IValueConverter ToExpensesOrRevenue =
        new FuncValueConverter<bool, string>(value => value ? "Expenses" : "Revenue");

    /// <summary>
    /// Converts bool (isChild) to background color for child rows.
    /// </summary>
    public static readonly IValueConverter ToChildRowBackground =
        new FuncValueConverter<bool, IBrush?>(value =>
        {
            if (!value) return null;

            if (Application.Current?.Resources != null &&
                Application.Current.Resources.TryGetResource("SurfaceAltBrush", Application.Current.ActualThemeVariant, out var resource) &&
                resource is IBrush brush)
            {
                return brush;
            }

            return new SolidColorBrush(Color.Parse("#F9FAFB"));
        });

    /// <summary>
    /// Converts bool (isChild) to left margin indent for child rows.
    /// </summary>
    public static readonly IValueConverter ToChildIndent =
        new FuncValueConverter<bool, Thickness>(value => value ? new Thickness(24, 0, 0, 0) : new Thickness(0));
}

/// <summary>
/// Integer converters for the Categories page.
/// </summary>
public static class IntConverters
{
    /// <summary>
    /// Returns true if the integer is greater than zero.
    /// </summary>
    public static readonly IValueConverter IsGreaterThanZero =
        new FuncValueConverter<int, bool>(value => value > 0);

    /// <summary>
    /// Returns true if the integer is zero.
    /// </summary>
    public static readonly IValueConverter IsZero =
        new FuncValueConverter<int, bool>(value => value == 0);
}

/// <summary>
/// String converters for the Categories page.
/// </summary>
public static class StringConverters
{
    /// <summary>
    /// Placeholder converter for icon selection (implemented via MultiBinding).
    /// </summary>
    public static readonly IValueConverter EqualsModalSelectedIcon =
        new FuncValueConverter<string, bool>(value => false); // Will be overridden by proper binding
}

/// <summary>
/// Multi-value converters for the Categories page.
/// </summary>
public static class MultiConverters
{
    /// <summary>
    /// Returns true if two strings are equal.
    /// </summary>
    public static readonly IMultiValueConverter StringEquals = new StringEqualsMultiConverter();

    /// <summary>
    /// Returns appropriate border brush for color selection.
    /// </summary>
    public static readonly IMultiValueConverter ColorSelectionBorder = new ColorSelectionBorderConverter();

    /// <summary>
    /// Returns border thickness based on selection state.
    /// </summary>
    public static readonly IMultiValueConverter SelectionBorderThickness = new SelectionBorderThicknessConverter();
}

/// <summary>
/// Multi-value converter that returns true if two string values are equal.
/// </summary>
public class StringEqualsMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return false;

        var value1 = values[0]?.ToString();
        var value2 = values[1]?.ToString();

        return string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Multi-value converter that returns a border brush based on selection state.
/// </summary>
public class ColorSelectionBorderConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return Brushes.Transparent;

        var value1 = values[0]?.ToString();
        var value2 = values[1]?.ToString();

        var isSelected = string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase);

        if (isSelected)
        {
            // Return the primary brush for selected items
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

/// <summary>
/// Multi-value converter that returns border thickness based on selection state.
/// </summary>
public class SelectionBorderThicknessConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return new Thickness(0);

        var value1 = values[0]?.ToString();
        var value2 = values[1]?.ToString();

        var isSelected = string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase);

        return isSelected ? new Thickness(2) : new Thickness(0);
    }
}
