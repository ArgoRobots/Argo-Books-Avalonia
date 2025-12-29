using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

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

    public static readonly StyledProperty<string?> ChangeLabelProperty =
        AvaloniaProperty.Register<StatCard, string?>(nameof(ChangeLabel), "from last period");

    public static readonly StyledProperty<bool> ShowChangeProperty =
        AvaloniaProperty.Register<StatCard, bool>(nameof(ShowChange));

    public static readonly StyledProperty<bool> UseAltBackgroundProperty =
        AvaloniaProperty.Register<StatCard, bool>(nameof(UseAltBackground));

    public static readonly StyledProperty<StatCardColor?> ValueColorProperty =
        AvaloniaProperty.Register<StatCard, StatCardColor?>(nameof(ValueColor));

    public static readonly StyledProperty<bool> IsCompactProperty =
        AvaloniaProperty.Register<StatCard, bool>(nameof(IsCompact));

    /// <summary>
    /// The width threshold below which the card switches to compact mode.
    /// </summary>
    private const double CompactThreshold = 250;

    #endregion

    #region Computed Properties for Class Binding

    // Icon Color Classes
    public static readonly DirectProperty<StatCard, bool> IsPrimaryColorProperty =
        AvaloniaProperty.RegisterDirect<StatCard, bool>(nameof(IsPrimaryColor), o => o.IsPrimaryColor);

    public static readonly DirectProperty<StatCard, bool> IsSuccessColorProperty =
        AvaloniaProperty.RegisterDirect<StatCard, bool>(nameof(IsSuccessColor), o => o.IsSuccessColor);

    public static readonly DirectProperty<StatCard, bool> IsDangerColorProperty =
        AvaloniaProperty.RegisterDirect<StatCard, bool>(nameof(IsDangerColor), o => o.IsDangerColor);

    public static readonly DirectProperty<StatCard, bool> IsWarningColorProperty =
        AvaloniaProperty.RegisterDirect<StatCard, bool>(nameof(IsWarningColor), o => o.IsWarningColor);

    public static readonly DirectProperty<StatCard, bool> IsInfoColorProperty =
        AvaloniaProperty.RegisterDirect<StatCard, bool>(nameof(IsInfoColor), o => o.IsInfoColor);

    // Trend Classes
    public static readonly DirectProperty<StatCard, bool> IsPositiveTrendProperty =
        AvaloniaProperty.RegisterDirect<StatCard, bool>(nameof(IsPositiveTrend), o => o.IsPositiveTrend);

    public static readonly DirectProperty<StatCard, bool> IsNegativeTrendProperty =
        AvaloniaProperty.RegisterDirect<StatCard, bool>(nameof(IsNegativeTrend), o => o.IsNegativeTrend);

    public static readonly DirectProperty<StatCard, bool> IsNeutralTrendProperty =
        AvaloniaProperty.RegisterDirect<StatCard, bool>(nameof(IsNeutralTrend), o => o.IsNeutralTrend);

    // Trend Arrow Classes
    public static readonly DirectProperty<StatCard, bool> IsTrendUpProperty =
        AvaloniaProperty.RegisterDirect<StatCard, bool>(nameof(IsTrendUp), o => o.IsTrendUp);

    public static readonly DirectProperty<StatCard, bool> IsTrendDownProperty =
        AvaloniaProperty.RegisterDirect<StatCard, bool>(nameof(IsTrendDown), o => o.IsTrendDown);

    public static readonly DirectProperty<StatCard, bool> IsTrendFlatProperty =
        AvaloniaProperty.RegisterDirect<StatCard, bool>(nameof(IsTrendFlat), o => o.IsTrendFlat);

    /// <summary>Gets whether icon color is Primary.</summary>
    public bool IsPrimaryColor => IconColor == StatCardColor.Primary;

    /// <summary>Gets whether icon color is Success.</summary>
    public bool IsSuccessColor => IconColor == StatCardColor.Success;

    /// <summary>Gets whether icon color is Danger.</summary>
    public bool IsDangerColor => IconColor == StatCardColor.Danger;

    /// <summary>Gets whether icon color is Warning.</summary>
    public bool IsWarningColor => IconColor == StatCardColor.Warning;

    /// <summary>Gets whether icon color is Info.</summary>
    public bool IsInfoColor => IconColor == StatCardColor.Info;

    // Value Color Classes
    /// <summary>Gets whether value color is Success.</summary>
    public bool IsValueColorSuccess => ValueColor == StatCardColor.Success;

    /// <summary>Gets whether value color is Danger.</summary>
    public bool IsValueColorDanger => ValueColor == StatCardColor.Danger;

    /// <summary>Gets whether trend is positive (value > 0).</summary>
    public bool IsPositiveTrend => ChangeValue is > 0;

    /// <summary>Gets whether trend is negative (value < 0).</summary>
    public bool IsNegativeTrend => ChangeValue is < 0;

    /// <summary>Gets whether trend is neutral (value == 0 or null).</summary>
    public bool IsNeutralTrend => !ChangeValue.HasValue || ChangeValue.Value == 0;

    /// <summary>Gets whether trend arrow should point up.</summary>
    public bool IsTrendUp => ChangeValue is > 0;

    /// <summary>Gets whether trend arrow should point down.</summary>
    public bool IsTrendDown => ChangeValue is < 0;

    /// <summary>Gets whether trend arrow should be flat.</summary>
    public bool IsTrendFlat => !ChangeValue.HasValue || ChangeValue.Value == 0;

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
    /// Gets or sets the label for the change comparison (e.g., "from last month", "from last period").
    /// </summary>
    public string? ChangeLabel
    {
        get => GetValue(ChangeLabelProperty);
        set => SetValue(ChangeLabelProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the change indicator.
    /// </summary>
    public bool ShowChange
    {
        get => GetValue(ShowChangeProperty);
        set => SetValue(ShowChangeProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to use the alternate (darker) background.
    /// </summary>
    public bool UseAltBackground
    {
        get => GetValue(UseAltBackgroundProperty);
        set => SetValue(UseAltBackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the color for the main value text (optional).
    /// </summary>
    public StatCardColor? ValueColor
    {
        get => GetValue(ValueColorProperty);
        set => SetValue(ValueColorProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the card is in compact mode (smaller text and icon).
    /// This is automatically set based on the card's width.
    /// </summary>
    public bool IsCompact
    {
        get => GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    #endregion

    public StatCard()
    {
        InitializeComponent();
        UpdateIconColorClass();
        UpdateValueColorClass();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Update compact mode based on available width
        var wasCompact = IsCompact;
        var shouldBeCompact = finalSize.Width > 0 && finalSize.Width < CompactThreshold;

        if (wasCompact != shouldBeCompact)
        {
            IsCompact = shouldBeCompact;
            UpdateCompactClass();
        }

        return base.ArrangeOverride(finalSize);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Update icon color computed properties and CSS class
        if (change.Property == IconColorProperty)
        {
            RaisePropertyChanged(IsPrimaryColorProperty, !IsPrimaryColor, IsPrimaryColor);
            RaisePropertyChanged(IsSuccessColorProperty, !IsSuccessColor, IsSuccessColor);
            RaisePropertyChanged(IsDangerColorProperty, !IsDangerColor, IsDangerColor);
            RaisePropertyChanged(IsWarningColorProperty, !IsWarningColor, IsWarningColor);
            RaisePropertyChanged(IsInfoColorProperty, !IsInfoColor, IsInfoColor);
            UpdateIconColorClass();
        }

        // Update value color CSS class
        if (change.Property == ValueColorProperty)
        {
            UpdateValueColorClass();
        }

        // Update alt-bg class
        if (change.Property == UseAltBackgroundProperty)
        {
            UpdateAltBgClass();
        }

        // Update compact class
        if (change.Property == IsCompactProperty)
        {
            UpdateCompactClass();
        }

        // Auto-show change indicator when ChangeValue or ChangeText is set
        if (change.Property == ChangeValueProperty || change.Property == ChangeTextProperty)
        {
            // Only show change indicator when there's a meaningful change (non-zero)
            ShowChange = ChangeValue.HasValue && ChangeValue.Value != 0;

            // Auto-generate ChangeText if only ChangeValue is set
            if (change.Property == ChangeValueProperty && ChangeValue.HasValue && string.IsNullOrEmpty(ChangeText))
            {
                // Use absolute value since the arrow indicates direction
                ChangeText = $"{Math.Abs(ChangeValue.Value):N1}%";
            }

            // Update trend computed properties
            RaisePropertyChanged(IsPositiveTrendProperty, !IsPositiveTrend, IsPositiveTrend);
            RaisePropertyChanged(IsNegativeTrendProperty, !IsNegativeTrend, IsNegativeTrend);
            RaisePropertyChanged(IsNeutralTrendProperty, !IsNeutralTrend, IsNeutralTrend);
            RaisePropertyChanged(IsTrendUpProperty, !IsTrendUp, IsTrendUp);
            RaisePropertyChanged(IsTrendDownProperty, !IsTrendDown, IsTrendDown);
            RaisePropertyChanged(IsTrendFlatProperty, !IsTrendFlat, IsTrendFlat);
        }
    }

    private void UpdateIconColorClass()
    {
        // Remove all icon color classes
        Classes.Remove("icon-primary");
        Classes.Remove("icon-success");
        Classes.Remove("icon-danger");
        Classes.Remove("icon-warning");
        Classes.Remove("icon-info");

        // Add the appropriate class
        var className = IconColor switch
        {
            StatCardColor.Primary => "icon-primary",
            StatCardColor.Success => "icon-success",
            StatCardColor.Danger => "icon-danger",
            StatCardColor.Warning => "icon-warning",
            StatCardColor.Info => "icon-info",
            _ => "icon-primary"
        };
        Classes.Add(className);
    }

    private void UpdateValueColorClass()
    {
        // Remove all value color classes
        Classes.Remove("value-success");
        Classes.Remove("value-danger");

        // Add the appropriate class if ValueColor is set
        if (ValueColor == StatCardColor.Success)
            Classes.Add("value-success");
        else if (ValueColor == StatCardColor.Danger)
            Classes.Add("value-danger");
    }

    private void UpdateAltBgClass()
    {
        if (UseAltBackground)
            Classes.Add("alt-bg");
        else
            Classes.Remove("alt-bg");
    }

    private void UpdateCompactClass()
    {
        if (IsCompact)
            Classes.Add("compact");
        else
            Classes.Remove("compact");
    }
}
