using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ArgoBooks.Controls;
using ArgoBooks.Services;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that converts a color hex string to a SolidColorBrush.
/// Optional parameter for opacity (0-1).
/// </summary>
public class StringToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
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

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
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
    public new static readonly IValueConverter ToString = new BoolToStringConverter();

    /// <summary>
    /// Converts bool to "Expenses" (true) or "Revenue" (false).
    /// </summary>
    public static readonly IValueConverter ToExpensesOrRevenue =
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
    /// Converts bool (hasError) to border brush.
    /// Error = red (#dc2626), No error = transparent.
    /// </summary>
    public static readonly IValueConverter ToErrorBorderBrush =
        new FuncValueConverter<bool, IBrush>(value =>
        {
            if (value)
            {
                if (Application.Current?.Resources != null &&
                    Application.Current.Resources.TryGetResource("ErrorBrush", Application.Current.ActualThemeVariant, out var resource) &&
                    resource is IBrush brush)
                {
                    return brush;
                }
                return new SolidColorBrush(Color.Parse("#dc2626"));
            }
            return Brushes.Transparent;
        });

    /// <summary>
    /// Converts bool (isActive) to status badge foreground color.
    /// Active = green (#166534), Inactive = gray (#4B5563).
    /// </summary>
    public static readonly IValueConverter ToStatusForeground =
        new FuncValueConverter<bool, IBrush>(value =>
            new SolidColorBrush(Color.Parse(value ? "#166534" : "#4B5563")));

    /// <summary>
    /// Converts bool (isFullscreen) to modal horizontal alignment.
    /// Fullscreen = Stretch, Normal = Center.
    /// </summary>
    public static readonly IValueConverter ToFullscreenHorizontalAlignment =
        new FuncValueConverter<bool, Avalonia.Layout.HorizontalAlignment>(value =>
            value ? Avalonia.Layout.HorizontalAlignment.Stretch : Avalonia.Layout.HorizontalAlignment.Center);

    /// <summary>
    /// Converts bool (isFullscreen) to modal vertical alignment.
    /// Fullscreen = Stretch, Normal = Center.
    /// </summary>
    public static readonly IValueConverter ToFullscreenVerticalAlignment =
        new FuncValueConverter<bool, Avalonia.Layout.VerticalAlignment>(value =>
            value ? Avalonia.Layout.VerticalAlignment.Stretch : Avalonia.Layout.VerticalAlignment.Center);

    /// <summary>
    /// Converts bool (isFullscreen) to modal width.
    /// Fullscreen = NaN (stretch), Normal = 800px.
    /// </summary>
    public static readonly IValueConverter ToFullscreenWidth =
        new FuncValueConverter<bool, double>(value => value ? double.NaN : 800);

    /// <summary>
    /// Converts bool (isFullscreen) to modal height.
    /// Fullscreen = NaN (stretch), Normal = 650px.
    /// </summary>
    public static readonly IValueConverter ToFullscreenHeight =
        new FuncValueConverter<bool, double>(value => value ? double.NaN : 650);

    /// <summary>
    /// Converts bool (isFullscreen) to modal margin.
    /// Fullscreen = 24px margin, Normal = 0.
    /// </summary>
    public static readonly IValueConverter ToFullscreenMargin =
        new FuncValueConverter<bool, Thickness>(value => value ? new Thickness(24) : new Thickness(0));

    /// <summary>
    /// Converts bool (isFullscreen) to fullscreen icon path data.
    /// Fullscreen = exit fullscreen icon, Normal = enter fullscreen icon.
    /// </summary>
    public static readonly IValueConverter ToFullscreenIcon =
        new FuncValueConverter<bool, string>(value => value
            ? "M5 16h3v3h2v-5H5v2zm3-8H5v2h5V5H8v3zm6 11h2v-3h3v-2h-5v5zm2-11V5h-2v5h5V8h-3z"  // Exit fullscreen
            : "M7 14H5v5h5v-2H7v-3zm-2-4h2V7h3V5H5v5zm12 7h-3v2h5v-5h-2v3zM14 5v2h3v3h2V5h-5z"); // Enter fullscreen

    /// <summary>
    /// Converts a file path string to a Bitmap image.
    /// Returns null if the file doesn't exist or can't be loaded.
    /// </summary>
    public static readonly IValueConverter ToImageSource = new FilePathToImageConverter();

    /// <summary>
    /// Converts bool (isExpenseTab) to table title for returns page.
    /// True = "Expense Returns", False = "Customer Returns".
    /// </summary>
    public static readonly IValueConverter ToReturnTableTitle =
        new FuncValueConverter<bool, string>(value =>
            LanguageService.Instance.Translate(value ? "Expense Returns" : "Customer Returns"));

    /// <summary>
    /// Converts bool (isExpenseTab) to column header.
    /// True = "Supplier", False = "Customer".
    /// </summary>
    public static readonly IValueConverter ToSupplierOrCustomerHeader =
        new FuncValueConverter<bool, string>(value =>
            LanguageService.Instance.Translate(value ? "Supplier" : "Customer"));

    /// <summary>
    /// Converts bool to ScrollBarVisibility.
    /// True = Auto (show when needed), False = Disabled.
    /// </summary>
    public static readonly IValueConverter ToScrollBarVisibility =
        new FuncValueConverter<bool, Avalonia.Controls.Primitives.ScrollBarVisibility>(value =>
            value ? Avalonia.Controls.Primitives.ScrollBarVisibility.Auto : Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
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
            if (File.Exists(filePath))
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

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
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

    /// <summary>
    /// Returns true if the integer is not zero.
    /// </summary>
    public static readonly IValueConverter IsNotZero =
        new FuncValueConverter<int, bool>(value => value != 0);
}

/// <summary>
/// String converters for various UI elements.
/// </summary>
public static class StringConverters
{
    /// <summary>
    /// Converts item type ("Product" or "Service") to badge background color.
    /// </summary>
    public static readonly IValueConverter ToItemTypeBadgeBackground = StatusConverters.ItemTypeBadgeBackground;

    /// <summary>
    /// Converts item type ("Product" or "Service") to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToItemTypeBadgeForeground = StatusConverters.ItemTypeBadgeForeground;

    /// <summary>
    /// Converts payment status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToPaymentStatusBackground = StatusConverters.PaymentStatusBackground;

    /// <summary>
    /// Converts payment status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToPaymentStatusForeground = StatusConverters.PaymentStatusForeground;

    /// <summary>
    /// Converts history transaction type to badge background color.
    /// </summary>
    public static readonly IValueConverter ToHistoryTypeBadgeBackground = StatusConverters.HistoryTypeBadgeBackground;

    /// <summary>
    /// Converts history transaction type to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToHistoryTypeBadgeForeground = StatusConverters.HistoryTypeBadgeForeground;

    /// <summary>
    /// Converts history status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToHistoryStatusBadgeBackground = StatusConverters.HistoryStatusBadgeBackground;

    /// <summary>
    /// Converts history status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToHistoryStatusBadgeForeground = StatusConverters.HistoryStatusBadgeForeground;

    /// <summary>
    /// Converts rental record status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToRentalStatusBackground = StatusConverters.RentalStatusBackground;

    /// <summary>
    /// Converts rental record status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToRentalStatusForeground = StatusConverters.RentalStatusForeground;

    /// <summary>
    /// Converts rental item status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToRentalItemStatusBackground = StatusConverters.RentalItemStatusBackground;

    /// <summary>
    /// Converts rental item status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToRentalItemStatusForeground = StatusConverters.RentalItemStatusForeground;

    /// <summary>
    /// Converts payment transaction status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToPaymentTransactionStatusBackground = StatusConverters.PaymentTransactionStatusBackground;

    /// <summary>
    /// Converts payment transaction status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToPaymentTransactionStatusForeground = StatusConverters.PaymentTransactionStatusForeground;

    /// <summary>
    /// Converts invoice status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToInvoiceStatusBackground = StatusConverters.InvoiceStatusBackground;

    /// <summary>
    /// Converts invoice status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToInvoiceStatusForeground = StatusConverters.InvoiceStatusForeground;

    /// <summary>
    /// Returns true if the value equals the parameter.
    /// </summary>
    public new static readonly IValueConverter Equals =
        new FuncValueConverter<string, string, bool>((value, parameter) =>
            string.Equals(value, parameter, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Converts expense status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToExpenseStatusBackground = StatusConverters.TransactionStatusBackground;

    /// <summary>
    /// Converts expense status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToExpenseStatusForeground = StatusConverters.TransactionStatusForeground;

    /// <summary>
    /// Converts revenue status to badge background color.
    /// </summary>
    public static readonly IValueConverter ToRevenueStatusBackground = StatusConverters.TransactionStatusBackground;

    /// <summary>
    /// Converts revenue status to badge foreground color.
    /// </summary>
    public static readonly IValueConverter ToRevenueStatusForeground = StatusConverters.TransactionStatusForeground;
}

/// <summary>
/// Converter that returns one of two colors based on a boolean value.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public string TrueColor { get; set; } = "#DC2626";
    public object? FalseColor { get; set; } = "#374151";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
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

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
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

/// <summary>
/// Converter that converts a bool to one of two strings based on the parameter.
/// Parameter format: "TrueValue;FalseValue"
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string paramString)
            return string.Empty;

        var parts = paramString.Split(';');
        if (parts.Length != 2)
            return string.Empty;

        return boolValue ? parts[0] : parts[1];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter that returns tab button background based on active state.
/// Active = primary color, Inactive = transparent.
/// </summary>
public class BoolToTabBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isActive)
            return Brushes.Transparent;

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

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter that returns tab button foreground based on active state.
/// Active = white, Inactive = secondary text color.
/// </summary>
public class BoolToTabForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isActive)
            return GetDefaultTextBrush();

        if (isActive)
        {
            return Brushes.White;
        }
        return GetDefaultTextBrush();
    }

    private static IBrush GetDefaultTextBrush()
    {
        if (Application.Current?.Resources != null &&
            Application.Current.Resources.TryGetResource("TextSecondaryBrush", Application.Current.ActualThemeVariant, out var resource) &&
            resource is IBrush brush)
        {
            return brush;
        }
        return new SolidColorBrush(Color.Parse("#6B7280"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
