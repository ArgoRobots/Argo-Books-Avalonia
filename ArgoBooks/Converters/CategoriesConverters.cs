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
    /// Converts bool to one of two strings based on the parameter.
    /// Parameter format: "TrueValue;FalseValue"
    /// </summary>
    public static new readonly IValueConverter ToString = new BoolToStringConverter();

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
    /// Returns true if the integer is zero.
    /// </summary>
    public static readonly IValueConverter IsZero =
        new FuncValueConverter<int, bool>(value => value == 0);
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

    /// <summary>
    /// Converts payment status to badge background color.
    /// Current = green (#DCFCE7), Overdue = yellow (#FEF3C7), Delinquent = red (#FEE2E2).
    /// </summary>
    public static readonly IValueConverter ToPaymentStatusBackground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Current" => "#DCFCE7",
                "Overdue" => "#FEF3C7",
                "Delinquent" => "#FEE2E2",
                _ => "#F3F4F6"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts payment status to badge foreground color.
    /// Current = green (#166534), Overdue = yellow (#92400E), Delinquent = red (#DC2626).
    /// </summary>
    public static readonly IValueConverter ToPaymentStatusForeground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Current" => "#166534",
                "Overdue" => "#92400E",
                "Delinquent" => "#DC2626",
                _ => "#4B5563"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts history transaction type to badge background color.
    /// Rental = blue (#DBEAFE), Purchase = purple (#F3E8FF), Return = orange (#FFEDD5), Payment = green (#DCFCE7).
    /// </summary>
    public static readonly IValueConverter ToHistoryTypeBadgeBackground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Rental" => "#DBEAFE",
                "Purchase" => "#F3E8FF",
                "Return" => "#FFEDD5",
                "Payment" => "#DCFCE7",
                _ => "#F3F4F6"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts history transaction type to badge foreground color.
    /// Rental = blue (#1E40AF), Purchase = purple (#7C3AED), Return = orange (#C2410C), Payment = green (#166534).
    /// </summary>
    public static readonly IValueConverter ToHistoryTypeBadgeForeground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Rental" => "#1E40AF",
                "Purchase" => "#7C3AED",
                "Return" => "#C2410C",
                "Payment" => "#166534",
                _ => "#4B5563"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts history status to badge background color.
    /// Completed = green (#DCFCE7), Pending = yellow (#FEF3C7), Overdue = red (#FEE2E2), Refunded = gray (#F3F4F6).
    /// </summary>
    public static readonly IValueConverter ToHistoryStatusBadgeBackground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Completed" => "#DCFCE7",
                "Pending" => "#FEF3C7",
                "Overdue" => "#FEE2E2",
                "Refunded" => "#E0E7FF",
                _ => "#F3F4F6"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts history status to badge foreground color.
    /// Completed = green (#166534), Pending = yellow (#92400E), Overdue = red (#DC2626), Refunded = indigo (#4F46E5).
    /// </summary>
    public static readonly IValueConverter ToHistoryStatusBadgeForeground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Completed" => "#166534",
                "Pending" => "#92400E",
                "Overdue" => "#DC2626",
                "Refunded" => "#4F46E5",
                _ => "#4B5563"
            };
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

/// <summary>
/// Converter that converts a bool to one of two strings based on the parameter.
/// Parameter format: "TrueValue;FalseValue"
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string paramString)
            return string.Empty;

        var parts = paramString.Split(';');
        if (parts.Length != 2)
            return string.Empty;

        return boolValue ? parts[0] : parts[1];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
