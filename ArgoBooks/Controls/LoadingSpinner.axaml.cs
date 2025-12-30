using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ArgoBooks.Controls;

/// <summary>
/// Spinner size presets.
/// </summary>
public enum SpinnerSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// An animated loading spinner component.
/// </summary>
public partial class LoadingSpinner : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<bool> IsSpinningProperty =
        AvaloniaProperty.Register<LoadingSpinner, bool>(nameof(IsSpinning), true);

    public static readonly StyledProperty<SpinnerSize> SizePresetProperty =
        AvaloniaProperty.Register<LoadingSpinner, SpinnerSize>(nameof(SizePreset), SpinnerSize.Medium);

    public static readonly StyledProperty<IBrush?> SpinnerBrushProperty =
        AvaloniaProperty.Register<LoadingSpinner, IBrush?>(nameof(SpinnerBrush));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether the spinner is animating.
    /// </summary>
    public bool IsSpinning
    {
        get => GetValue(IsSpinningProperty);
        set => SetValue(IsSpinningProperty, value);
    }

    /// <summary>
    /// Gets or sets the spinner size preset.
    /// </summary>
    public SpinnerSize SizePreset
    {
        get => GetValue(SizePresetProperty);
        set => SetValue(SizePresetProperty, value);
    }

    /// <summary>
    /// Gets or sets the spinner color brush.
    /// </summary>
    public IBrush? SpinnerBrush
    {
        get => GetValue(SpinnerBrushProperty);
        set => SetValue(SpinnerBrushProperty, value);
    }

    /// <summary>
    /// Gets the computed spinner size based on the preset.
    /// </summary>
    public double ComputedSize => SizePreset switch
    {
        SpinnerSize.Small => 16,
        SpinnerSize.Large => 48,
        _ => 24
    };

    /// <summary>
    /// Gets the stroke thickness based on size.
    /// </summary>
    public double StrokeThickness => SizePreset switch
    {
        SpinnerSize.Small => 2,
        SpinnerSize.Large => 4,
        _ => 3
    };

    /// <summary>
    /// Gets the arc start point.
    /// </summary>
    public Point ArcStart
    {
        get
        {
            var center = ComputedSize / 2;
            var radius = center - StrokeThickness / 2;
            return new Point(center, StrokeThickness / 2);
        }
    }

    /// <summary>
    /// Gets the arc end point (90 degrees sweep).
    /// </summary>
    public Point ArcEnd
    {
        get
        {
            var center = ComputedSize / 2;
            var radius = center - StrokeThickness / 2;
            return new Point(center + radius, center);
        }
    }

    /// <summary>
    /// Gets the arc size for the path.
    /// </summary>
    public Size ArcSize
    {
        get
        {
            var radius = (ComputedSize - StrokeThickness) / 2;
            return new Size(radius, radius);
        }
    }

    #endregion

    public LoadingSpinner()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SpinnerBrushProperty)
        {
            UpdateBrush();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateBrush();
    }

    private void UpdateBrush()
    {
        if (SpinnerBrush == null)
        {
            // Try to get the primary brush from resources
            if (this.TryFindResource("PrimaryBrush", ActualThemeVariant, out var brush) && brush is IBrush primaryBrush)
            {
                SetCurrentValue(SpinnerBrushProperty, primaryBrush);
            }
        }
    }
}
