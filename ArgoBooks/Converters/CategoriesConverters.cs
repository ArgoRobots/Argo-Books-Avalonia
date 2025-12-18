using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ArgoBooks.Controls;

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
/// Bool converters for the Categories page and general use.
/// </summary>
public static class BoolConverters
{
    /// <summary>
    /// Converts bool to "Expenses" (true) or "Revenue" (false).
    /// </summary>
    public static readonly IValueConverter ToExpensesOrRevenue =
        new FuncValueConverter<bool, string>(value => value ? "Expenses" : "Revenue");

    /// <summary>
    /// Converts bool (isExpensesTabSelected) to "Expenses" (true) or "Revenue" (false).
    /// </summary>
    public static readonly IValueConverter ToExpensesOrRevenueProducts =
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

    /// <summary>
    /// Converts bool (isActive) to status badge background color.
    /// Active = green (#DCFCE7), Inactive = gray (#F3F4F6).
    /// </summary>
    public static readonly IValueConverter ToStatusBackground =
        new FuncValueConverter<bool, IBrush>(value =>
            new SolidColorBrush(Color.Parse(value ? "#DCFCE7" : "#F3F4F6")));

    /// <summary>
    /// Converts bool (isActive) to status badge foreground color.
    /// Active = green (#166534), Inactive = gray (#4B5563).
    /// </summary>
    public static readonly IValueConverter ToStatusForeground =
        new FuncValueConverter<bool, IBrush>(value =>
            new SolidColorBrush(Color.Parse(value ? "#166534" : "#4B5563")));
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

    /// <summary>
    /// Returns true if the integer is greater than one (for pagination visibility).
    /// </summary>
    public static readonly IValueConverter IsGreaterThanOne =
        new FuncValueConverter<int, bool>(value => value > 1);

    /// <summary>
    /// Checks if the value equals the current page (for pagination active state).
    /// This is a placeholder - actual comparison happens differently.
    /// </summary>
    public static readonly IValueConverter EqualsCurrentPage =
        new FuncValueConverter<int, bool>(value => false);
}

/// <summary>
/// String converters for various UI elements.
/// </summary>
public static class StringConverters
{
    /// <summary>
    /// Converts item type ("Product" or "Service") to badge background color.
    /// Product = blue (#DBEAFE), Service = purple (#F3E8FF).
    /// </summary>
    public static readonly IValueConverter ToItemTypeBadgeBackground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value == "Service" ? "#F3E8FF" : "#DBEAFE";
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts item type ("Product" or "Service") to badge foreground color.
    /// Product = blue (#1E40AF), Service = purple (#7C3AED).
    /// </summary>
    public static readonly IValueConverter ToItemTypeBadgeForeground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value == "Service" ? "#7C3AED" : "#1E40AF";
            return new SolidColorBrush(Color.Parse(color));
        });
}

/// <summary>
/// Multi-value converter for sort indicator visibility.
/// Expects: [0] SortColumn, [1] SortDirection, Parameter: "ColumnName:Ascending" or "ColumnName:Descending"
/// </summary>
public class SortIndicatorConverter : IMultiValueConverter
{
    public static readonly SortIndicatorConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || parameter is not string param)
            return false;

        var sortColumn = values[0] as string;
        var sortDirection = values[1] is SortDirection dir ? dir : SortDirection.None;

        var parts = param.Split(':');
        if (parts.Length != 2)
            return false;

        var expectedColumn = parts[0];
        var expectedDirection = parts[1] switch
        {
            "Ascending" => SortDirection.Ascending,
            "Descending" => SortDirection.Descending,
            _ => SortDirection.None
        };

        return sortColumn == expectedColumn && sortDirection == expectedDirection;
    }
}

/// <summary>
/// Converter for checking if two values are equal and returning an "active" class name.
/// </summary>
public class PageEqualsConverter : IMultiValueConverter
{
    public static readonly PageEqualsConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
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

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
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

/// <summary>
/// Converter that returns white foreground if page equals current page, else default text color.
/// </summary>
public class PageActiveForegroundConverter : IMultiValueConverter
{
    public static readonly PageActiveForegroundConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
        {
            return GetDefaultTextBrush();
        }

        var isActive = Equals(values[0], values[1]);
        if (isActive)
        {
            return Brushes.White;
        }
        return GetDefaultTextBrush();
    }

    private static IBrush GetDefaultTextBrush()
    {
        if (Application.Current?.Resources != null &&
            Application.Current.Resources.TryGetResource("TextPrimaryBrush", Application.Current.ActualThemeVariant, out var resource) &&
            resource is IBrush brush)
        {
            return brush;
        }
        return new SolidColorBrush(Color.Parse("#374151"));
    }
}
