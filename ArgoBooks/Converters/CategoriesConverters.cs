using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
    /// Converts bool (isActive) to view toggle button background.
    /// Active = surface hover color, Inactive = transparent.
    /// </summary>
    public static readonly IValueConverter ToViewToggleBackground =
        new FuncValueConverter<bool, IBrush>(value =>
        {
            if (value)
            {
                if (Application.Current?.Resources != null &&
                    Application.Current.Resources.TryGetResource("SurfaceHoverBrush", Application.Current.ActualThemeVariant, out var resource) &&
                    resource is IBrush brush)
                {
                    return brush;
                }
                return new SolidColorBrush(Color.Parse("#F3F4F6"));
            }
            return Brushes.Transparent;
        });

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

    /// <summary>
    /// Converts bool (isFullscreen) to modal width.
    /// Fullscreen = 95% of window, Normal = 600px.
    /// </summary>
    public static readonly IValueConverter ToFullscreenWidth =
        new FuncValueConverter<bool, double>(value => value ? 1200 : 600);

    /// <summary>
    /// Converts bool (isFullscreen) to modal height.
    /// Fullscreen = 95% of window, Normal = 500px.
    /// </summary>
    public static readonly IValueConverter ToFullscreenHeight =
        new FuncValueConverter<bool, double>(value => value ? 800 : 500);

    /// <summary>
    /// Converts a file path string to a Bitmap image.
    /// Returns null if the file doesn't exist or can't be loaded.
    /// </summary>
    public static readonly IValueConverter ToImageSource = new FilePathToImageConverter();
}

/// <summary>
/// Converter that loads a Bitmap image from a file path.
/// </summary>
public class FilePathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath || string.IsNullOrEmpty(filePath))
            return null;

        try
        {
            if (System.IO.File.Exists(filePath))
            {
                return new Bitmap(filePath);
            }
        }
        catch
        {
            // Failed to load image
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
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

    /// <summary>
    /// Returns true if the integer is positive (greater than zero).
    /// </summary>
    public static readonly IValueConverter IsPositive =
        new FuncValueConverter<int, bool>(value => value > 0);

    /// <summary>
    /// Returns true if the integer is greater than one.
    /// Useful for showing pagination controls only when there are multiple pages.
    /// </summary>
    public static readonly IValueConverter IsGreaterThanOne =
        new FuncValueConverter<int, bool>(value => value > 1);
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

    /// <summary>
    /// Converts rental record status to badge background color.
    /// Active = green (#DCFCE7), Returned = blue (#DBEAFE), Overdue = red (#FEE2E2), Cancelled = gray (#F3F4F6).
    /// </summary>
    public static readonly IValueConverter ToRentalStatusBackground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Active" => "#DCFCE7",
                "Returned" => "#DBEAFE",
                "Overdue" => "#FEE2E2",
                "Cancelled" => "#F3F4F6",
                _ => "#F3F4F6"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts rental record status to badge foreground color.
    /// Active = green (#166534), Returned = blue (#1E40AF), Overdue = red (#DC2626), Cancelled = gray (#4B5563).
    /// </summary>
    public static readonly IValueConverter ToRentalStatusForeground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Active" => "#166534",
                "Returned" => "#1E40AF",
                "Overdue" => "#DC2626",
                "Cancelled" => "#4B5563",
                _ => "#4B5563"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts rental item status to badge background color.
    /// Available = green (#DCFCE7), In Maintenance = yellow (#FEF3C7), All Rented = purple (#F3E8FF).
    /// </summary>
    public static readonly IValueConverter ToRentalItemStatusBackground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Available" => "#DCFCE7",
                "In Maintenance" => "#FEF3C7",
                "All Rented" => "#F3E8FF",
                _ => "#F3F4F6"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts rental item status to badge foreground color.
    /// Available = green (#166534), In Maintenance = yellow (#92400E), All Rented = purple (#7C3AED).
    /// </summary>
    public static readonly IValueConverter ToRentalItemStatusForeground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Available" => "#166534",
                "In Maintenance" => "#92400E",
                "All Rented" => "#7C3AED",
                _ => "#4B5563"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts payment transaction status to badge background color.
    /// Completed = green (#DCFCE7), Pending = yellow (#FEF3C7), Partial = blue (#DBEAFE), Refunded = purple (#F3E8FF).
    /// </summary>
    public static readonly IValueConverter ToPaymentTransactionStatusBackground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Completed" => "#DCFCE7",
                "Pending" => "#FEF3C7",
                "Partial" => "#DBEAFE",
                "Refunded" => "#F3E8FF",
                _ => "#F3F4F6"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts payment transaction status to badge foreground color.
    /// Completed = green (#166534), Pending = yellow (#92400E), Partial = blue (#1E40AF), Refunded = purple (#7C3AED).
    /// </summary>
    public static readonly IValueConverter ToPaymentTransactionStatusForeground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Completed" => "#166534",
                "Pending" => "#92400E",
                "Partial" => "#1E40AF",
                "Refunded" => "#7C3AED",
                _ => "#4B5563"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts invoice status to badge background color.
    /// Paid = green (#DCFCE7), Pending = yellow (#FEF3C7), Overdue = red (#FEE2E2), Draft = gray (#F3F4F6),
    /// Sent = blue (#DBEAFE), Partial = purple (#F3E8FF), Cancelled = gray (#F3F4F6).
    /// </summary>
    public static readonly IValueConverter ToInvoiceStatusBackground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Paid" => "#DCFCE7",
                "Pending" => "#FEF3C7",
                "Overdue" => "#FEE2E2",
                "Draft" => "#F3F4F6",
                "Sent" => "#DBEAFE",
                "Viewed" => "#DBEAFE",
                "Partial" => "#F3E8FF",
                "Cancelled" => "#F3F4F6",
                _ => "#F3F4F6"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts invoice status to badge foreground color.
    /// Paid = green (#166534), Pending = yellow (#92400E), Overdue = red (#DC2626), Draft = gray (#4B5563),
    /// Sent = blue (#1E40AF), Partial = purple (#7C3AED), Cancelled = gray (#4B5563).
    /// </summary>
    public static readonly IValueConverter ToInvoiceStatusForeground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Paid" => "#166534",
                "Pending" => "#92400E",
                "Overdue" => "#DC2626",
                "Draft" => "#4B5563",
                "Sent" => "#1E40AF",
                "Viewed" => "#1E40AF",
                "Partial" => "#7C3AED",
                "Cancelled" => "#4B5563",
                _ => "#4B5563"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Returns true if the value equals the parameter.
    /// </summary>
    public static new readonly IValueConverter Equals =
        new FuncValueConverter<string, string, bool>((value, parameter) =>
            string.Equals(value, parameter, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Converts expense status to badge background color.
    /// Completed = green (#DCFCE7), Pending = yellow (#FEF3C7), Returned = blue (#DBEAFE), Cancelled = gray (#F3F4F6).
    /// </summary>
    public static readonly IValueConverter ToExpenseStatusBackground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Completed" => "#DCFCE7",
                "Pending" => "#FEF3C7",
                "Returned" => "#DBEAFE",
                "Partial Return" => "#F3E8FF",
                "Cancelled" => "#F3F4F6",
                _ => "#F3F4F6"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts expense status to badge foreground color.
    /// Completed = green (#166534), Pending = yellow (#92400E), Returned = blue (#1E40AF), Cancelled = gray (#4B5563).
    /// </summary>
    public static readonly IValueConverter ToExpenseStatusForeground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Completed" => "#166534",
                "Pending" => "#92400E",
                "Returned" => "#1E40AF",
                "Partial Return" => "#7C3AED",
                "Cancelled" => "#4B5563",
                _ => "#4B5563"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts revenue status to badge background color.
    /// Completed = green (#DCFCE7), Pending = yellow (#FEF3C7), Returned = blue (#DBEAFE), Cancelled = gray (#F3F4F6).
    /// </summary>
    public static readonly IValueConverter ToRevenueStatusBackground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Completed" => "#DCFCE7",
                "Pending" => "#FEF3C7",
                "Returned" => "#DBEAFE",
                "Partial Return" => "#F3E8FF",
                "Cancelled" => "#F3F4F6",
                _ => "#F3F4F6"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts revenue status to badge foreground color.
    /// Completed = green (#166534), Pending = yellow (#92400E), Returned = blue (#1E40AF), Cancelled = gray (#4B5563).
    /// </summary>
    public static readonly IValueConverter ToRevenueStatusForeground =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Completed" => "#166534",
                "Pending" => "#92400E",
                "Returned" => "#1E40AF",
                "Partial Return" => "#7C3AED",
                "Cancelled" => "#4B5563",
                _ => "#4B5563"
            };
            return new SolidColorBrush(Color.Parse(color));
        });
}

/// <summary>
/// Converter for status badge colors (used in Expenses/Revenue pages).
/// </summary>
public static class StatusColorConverter
{
    /// <summary>
    /// Converts status to badge background color.
    /// Completed = green, Pending = yellow, Partial Return = purple, Returned = blue, Cancelled = gray.
    /// </summary>
    public static readonly IValueConverter BackgroundInstance =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Completed" => "#DCFCE7",
                "Pending" => "#FEF3C7",
                "Partial Return" => "#F3E8FF",
                "Returned" => "#DBEAFE",
                "Cancelled" => "#F3F4F6",
                _ => "#F3F4F6"
            };
            return new SolidColorBrush(Color.Parse(color));
        });

    /// <summary>
    /// Converts status to badge foreground color.
    /// Completed = green, Pending = yellow, Partial Return = purple, Returned = blue, Cancelled = gray.
    /// </summary>
    public static readonly IValueConverter ForegroundInstance =
        new FuncValueConverter<string, IBrush>(value =>
        {
            var color = value switch
            {
                "Completed" => "#166534",
                "Pending" => "#92400E",
                "Partial Return" => "#7C3AED",
                "Returned" => "#1E40AF",
                "Cancelled" => "#4B5563",
                _ => "#4B5563"
            };
            return new SolidColorBrush(Color.Parse(color));
        });
}

/// <summary>
/// Converter for sort icons in table headers.
/// </summary>
public class SortIconConverter : IValueConverter
{
    public static readonly SortIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Return the appropriate sort icon path based on direction
        // This returns a path data string for ascending/descending arrows
        if (value is string sortColumn && parameter is string columnName)
        {
            if (sortColumn == columnName)
            {
                // Return generic arrow icon (will be styled by direction)
                return "M7 10l5 5 5-5z"; // Down arrow
            }
        }
        return "M7 14l5-5 5 5z"; // Up arrow (default)
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter that returns one of two colors based on a boolean value.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public string TrueColor { get; set; } = "#DC2626";
    public object? FalseColor { get; set; } = "#374151";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
            return GetBrush(FalseColor);

        return boolValue ? new SolidColorBrush(Color.Parse(TrueColor)) : GetBrush(FalseColor);
    }

    private static IBrush GetBrush(object? colorValue)
    {
        if (colorValue is string colorString)
        {
            try
            {
                return new SolidColorBrush(Color.Parse(colorString));
            }
            catch
            {
                // Try to get from resources
            }
        }

        if (colorValue is IBrush brush)
            return brush;

        // Try to get TextPrimaryBrush from resources
        if (Application.Current?.Resources != null &&
            Application.Current.Resources.TryGetResource("TextPrimaryBrush", Application.Current.ActualThemeVariant, out var resource) &&
            resource is IBrush textBrush)
        {
            return textBrush;
        }

        return new SolidColorBrush(Color.Parse("#374151"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
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
/// Converter for checking if two values are equal.
/// Returns true if both values are equal.
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
