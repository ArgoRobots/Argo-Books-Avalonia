using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ArgoBooks.Services;

namespace ArgoBooks.Converters;

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
    /// Converts bool (isExpanded) to rotation angle.
    /// Expanded = 0, Collapsed = -90.
    /// </summary>
    public static readonly IValueConverter ToCollapseAngle =
        new FuncValueConverter<bool, double>(value => value ? 0 : -90);

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
    /// Error = red (#dc2626), No error = default border color.
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
            // Return BorderBrush for no error state to preserve the control's default border
            if (Application.Current?.Resources != null &&
                Application.Current.Resources.TryGetResource("BorderBrush", Application.Current.ActualThemeVariant, out var borderResource) &&
                borderResource is IBrush borderBrush)
            {
                return borderBrush;
            }
            return new SolidColorBrush(Color.Parse("#e5e7eb"));
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
    /// Both fullscreen and normal modes use Center alignment.
    /// </summary>
    public static readonly IValueConverter ToFullscreenHorizontalAlignment =
        new FuncValueConverter<bool, Avalonia.Layout.HorizontalAlignment>(value =>
            Avalonia.Layout.HorizontalAlignment.Center);

    /// <summary>
    /// Converts bool (isFullscreen) to modal vertical alignment.
    /// Fullscreen = Stretch, Normal = Center.
    /// </summary>
    public static readonly IValueConverter ToFullscreenVerticalAlignment =
        new FuncValueConverter<bool, Avalonia.Layout.VerticalAlignment>(value =>
            value ? Avalonia.Layout.VerticalAlignment.Stretch : Avalonia.Layout.VerticalAlignment.Center);

    /// <summary>
    /// Converts bool (isFullscreen) to modal width.
    /// Fullscreen = NaN (stretch), Normal = default or parameter value.
    /// Supports ConverterParameter for custom normal width.
    /// </summary>
    public static readonly IValueConverter ToFullscreenWidth = new FullscreenDimensionConverter(800);

    /// <summary>
    /// Converts bool (isFullscreen) to modal height.
    /// Fullscreen = NaN (stretch), Normal = default or parameter value.
    /// Supports ConverterParameter for custom normal height.
    /// </summary>
    public static readonly IValueConverter ToFullscreenHeight = new FullscreenDimensionConverter(650);

    /// <summary>
    /// Converts bool to width based on parameter "TrueValue,FalseValue".
    /// </summary>
    public static readonly IValueConverter ToWidth = new BoolToDoubleConverter();

    /// <summary>
    /// Converts bool to height based on parameter "TrueValue,FalseValue".
    /// </summary>
    public static readonly IValueConverter ToHeight = new BoolToDoubleConverter();

    /// <summary>
    /// Converts bool (isFullscreen) to modal margin.
    /// Fullscreen = 24px vertical margin only (width is fixed), Normal = 0.
    /// </summary>
    public static readonly IValueConverter ToFullscreenMargin =
        new FuncValueConverter<bool, Thickness>(value => value ? new Thickness(0, 24, 0, 24) : new Thickness(0));

    /// <summary>
    /// Converts bool (isFullscreen) to modal min width.
    /// Fullscreen = 0 (no min), Normal = parameter or 800.
    /// </summary>
    public static readonly IValueConverter ToFullscreenMinWidth = new FullscreenMinMaxConverter(800, true);

    /// <summary>
    /// Converts bool (isFullscreen) to modal max width.
    /// Fullscreen = Infinity (no max), Normal = parameter or 1200.
    /// </summary>
    public static readonly IValueConverter ToFullscreenMaxWidth = new FullscreenMinMaxConverter(1200, false);

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

    /// <summary>
    /// Returns the ConverterParameter when the bool value is true, null otherwise.
    /// Useful for conditionally showing tooltips only in compact mode.
    /// </summary>
    public static readonly IValueConverter ToParameterWhenTrue = new BoolToParameterConverter(true);

    /// <summary>
    /// Returns the ConverterParameter when the bool value is false, null otherwise.
    /// </summary>
    public static readonly IValueConverter ToParameterWhenFalse = new BoolToParameterConverter(false);

    /// <summary>
    /// Converts bool (isExpanded) to header border thickness.
    /// Expanded = bottom border only, Collapsed = no border.
    /// </summary>
    public static readonly IValueConverter ToHeaderBorderThickness =
        new FuncValueConverter<bool, Thickness>(value => value ? new Thickness(0, 0, 0, 1) : new Thickness(0));

    /// <summary>
    /// Converts bool (isExpanded) to max height for collapsible content.
    /// Expanded = large value to show content, Collapsed = 0 to hide.
    /// </summary>
    public static readonly IValueConverter ToExpandedMaxHeight =
        new FuncValueConverter<bool, double>(value => value ? 500 : 0);

    /// <summary>
    /// Converts bool (isCompleted) to circle border thickness.
    /// Completed = 0 (no border), Not completed = 2px border.
    /// </summary>
    public static readonly IValueConverter ToCircleBorderThickness =
        new FuncValueConverter<bool, Thickness>(value => value ? new Thickness(0) : new Thickness(2));

    /// <summary>
    /// Multi-value converter that returns one of two brushes based on a boolean condition.
    /// Values[0] = bool condition, Values[1] = brush when true, Values[2] = brush when false.
    /// </summary>
    public static readonly IMultiValueConverter ToConditionalBrush = new ConditionalBrushMultiConverter();
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
        => throw new NotSupportedException();
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
        => throw new NotSupportedException();
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
        => throw new NotSupportedException();
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
        => throw new NotSupportedException();
}

/// <summary>
/// Converter that returns the parameter when the bool matches the expected value.
/// </summary>
public class BoolToParameterConverter : IValueConverter
{
    private readonly bool _returnWhen;

    public BoolToParameterConverter(bool returnWhen)
    {
        _returnWhen = returnWhen;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue == _returnWhen)
        {
            return parameter;
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converter that returns one of two brushes based on a boolean value.
/// Useful for dynamically changing colors in XAML.
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public object? TrueValue { get; set; }
    public object? FalseValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
            return FalseValue;

        var selectedValue = boolValue ? TrueValue : FalseValue;

        if (selectedValue is IBrush brush)
            return brush;

        if (selectedValue is string colorString)
        {
            try
            {
                return new SolidColorBrush(Color.Parse(colorString));
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converter that returns strikethrough text decoration when the bool value is true.
/// </summary>
public class BoolToTextDecorationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
        {
            return TextDecorations.Strikethrough;
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converter for fullscreen modal dimensions.
/// Parameter format: "normalValue" or "normalValue,fullscreenValue"
/// When fullscreen is true: returns fullscreenValue if provided, otherwise NaN (stretch).
/// When fullscreen is false: returns normalValue or the default.
/// </summary>
public class FullscreenDimensionConverter : IValueConverter
{
    private readonly double _defaultValue;

    public FullscreenDimensionConverter(double defaultValue)
    {
        _defaultValue = defaultValue;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isFullscreen)
            return _defaultValue;

        double normalValue = _defaultValue;
        double? fullscreenValue = null;

        if (parameter is string stringParam)
        {
            var parts = stringParam.Split(',');
            if (double.TryParse(parts[0].Trim(), out var parsed))
                normalValue = parsed;
            if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out var parsedFullscreen))
                fullscreenValue = parsedFullscreen;
        }
        else if (parameter is double doubleParam)
        {
            normalValue = doubleParam;
        }

        if (isFullscreen)
            return fullscreenValue ?? double.NaN;

        return normalValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converter that returns one of two double values based on a boolean.
/// Parameter format: "TrueValue,FalseValue" (e.g., "850,750")
/// </summary>
public class BoolToDoubleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string paramString)
            return 0.0;

        var parts = paramString.Split(',');
        if (parts.Length != 2)
            return 0.0;

        if (double.TryParse(parts[0].Trim(), out var trueValue) &&
            double.TryParse(parts[1].Trim(), out var falseValue))
        {
            return boolValue ? trueValue : falseValue;
        }

        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converter for fullscreen modal min/max width constraints.
/// When fullscreen is true: returns 0 for min, Infinity for max (no constraints).
/// When fullscreen is false: returns the default or parameter value.
/// </summary>
public class FullscreenMinMaxConverter : IValueConverter
{
    private readonly double _defaultValue;
    private readonly bool _isMin;

    public FullscreenMinMaxConverter(double defaultValue, bool isMin)
    {
        _defaultValue = defaultValue;
        _isMin = isMin;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isFullscreen)
            return _defaultValue;

        if (isFullscreen)
            return _isMin ? 0.0 : double.PositiveInfinity;

        // Use parameter if provided, otherwise use default
        if (parameter is double doubleParam)
            return doubleParam;

        if (parameter is string stringParam && double.TryParse(stringParam, out var parsedValue))
            return parsedValue;

        return _defaultValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Multi-value converter that returns one of two brushes based on a boolean condition.
/// Values[0] = bool condition, Values[1] = brush when true, Values[2] = brush when false.
/// </summary>
public class ConditionalBrushMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 3)
            return Brushes.Transparent;

        var condition = values[0] is bool b && b;
        var trueBrush = values[1] as IBrush ?? Brushes.Transparent;
        var falseBrush = values[2] as IBrush ?? Brushes.Transparent;

        return condition ? trueBrush : falseBrush;
    }
}
