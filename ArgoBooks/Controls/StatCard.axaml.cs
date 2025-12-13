using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace ArgoBooks.Controls;

/// <summary>
/// Color variants for the stat card icon.
/// </summary>
public enum StatCardColor
{
    /// <summary>
    /// Primary/blue color.
    /// </summary>
    Primary,

    /// <summary>
    /// Success/green color.
    /// </summary>
    Success,

    /// <summary>
    /// Danger/red color.
    /// </summary>
    Danger,

    /// <summary>
    /// Warning/yellow color.
    /// </summary>
    Warning,

    /// <summary>
    /// Info/cyan color.
    /// </summary>
    Info
}

/// <summary>
/// A dashboard stat card component displaying a value with icon and optional trend indicator.
/// </summary>
public partial class StatCard : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<StatCard, string?>(nameof(Label));

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<StatCard, string?>(nameof(Value));

    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<StatCard, Geometry?>(nameof(Icon));

    public static readonly StyledProperty<StatCardColor> IconColorProperty =
        AvaloniaProperty.Register<StatCard, StatCardColor>(nameof(IconColor), StatCardColor.Primary);

    public static readonly StyledProperty<double?> ChangeValueProperty =
        AvaloniaProperty.Register<StatCard, double?>(nameof(ChangeValue));

    public static readonly StyledProperty<string?> ChangeTextProperty =
        AvaloniaProperty.Register<StatCard, string?>(nameof(ChangeText));

    public static readonly StyledProperty<string?> SecondaryTextProperty =
        AvaloniaProperty.Register<StatCard, string?>(nameof(SecondaryText));

    public static readonly StyledProperty<bool> ShowChangeProperty =
        AvaloniaProperty.Register<StatCard, bool>(nameof(ShowChange));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the label text displayed above the value.
    /// </summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// Gets or sets the main value to display.
    /// </summary>
    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon geometry.
    /// </summary>
    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon color variant.
    /// </summary>
    public StatCardColor IconColor
    {
        get => GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the change value (positive = increase, negative = decrease).
    /// </summary>
    public double? ChangeValue
    {
        get => GetValue(ChangeValueProperty);
        set => SetValue(ChangeValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the change text to display (e.g., "+12%", "-5%").
    /// </summary>
    public string? ChangeText
    {
        get => GetValue(ChangeTextProperty);
        set => SetValue(ChangeTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the secondary text displayed below the value.
    /// </summary>
    public string? SecondaryText
    {
        get => GetValue(SecondaryTextProperty);
        set => SetValue(SecondaryTextProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the change indicator.
    /// </summary>
    public bool ShowChange
    {
        get => GetValue(ShowChangeProperty);
        set => SetValue(ShowChangeProperty, value);
    }

    #endregion

    #region Converters

    /// <summary>
    /// Converter to get CSS class for icon color.
    /// </summary>
    public static readonly IMultiValueConverter IconColorClassConverter = new IconColorToClassConverter();

    /// <summary>
    /// Converter to get CSS class for trend (positive/negative/neutral).
    /// </summary>
    public static readonly IMultiValueConverter TrendClassConverter = new TrendToClassConverter();

    /// <summary>
    /// Converter to get CSS class for trend arrow direction.
    /// </summary>
    public static readonly IMultiValueConverter TrendArrowClassConverter = new TrendArrowToClassConverter();

    private class IconColorToClassConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 0 || values[0] is not StatCardColor color)
                return new Classes("primary");

            var className = color switch
            {
                StatCardColor.Primary => "primary",
                StatCardColor.Success => "success",
                StatCardColor.Danger => "danger",
                StatCardColor.Warning => "warning",
                StatCardColor.Info => "info",
                _ => "primary"
            };

            return new Classes(className);
        }
    }

    private class TrendToClassConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 0 || values[0] is not double changeValue)
                return new Classes("neutral");

            var className = changeValue switch
            {
                > 0 => "positive",
                < 0 => "negative",
                _ => "neutral"
            };

            return new Classes(className);
        }
    }

    private class TrendArrowToClassConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 0 || values[0] is not double changeValue)
                return new Classes("flat");

            var className = changeValue switch
            {
                > 0 => "up",
                < 0 => "down",
                _ => "flat"
            };

            return new Classes(className);
        }
    }

    #endregion

    public StatCard()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Auto-show change indicator when ChangeValue or ChangeText is set
        if (change.Property == ChangeValueProperty || change.Property == ChangeTextProperty)
        {
            ShowChange = ChangeValue.HasValue || !string.IsNullOrEmpty(ChangeText);

            // Auto-generate ChangeText if only ChangeValue is set
            if (change.Property == ChangeValueProperty && ChangeValue.HasValue && string.IsNullOrEmpty(ChangeText))
            {
                var prefix = ChangeValue.Value >= 0 ? "+" : "";
                ChangeText = $"{prefix}{ChangeValue.Value:N1}%";
            }
        }
    }
}
