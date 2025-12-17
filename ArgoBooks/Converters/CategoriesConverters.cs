using System;
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
    /// Converts bool (isExpensesTabSelected) to "Purchased Products/Services" (true) or "Products/Services for Sale" (false).
    /// </summary>
    public static readonly IValueConverter ToExpensesOrRevenueProducts =
        new FuncValueConverter<bool, string>(value => value ? "Purchased Products/Services" : "Products/Services for Sale");

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
