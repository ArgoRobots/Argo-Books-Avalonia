using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace ArgoBooks.Controls;

/// <summary>
/// An animated arrow control that rotates 180 degrees when collapsed.
/// Used for toggle buttons in sidebars and collapsible panels.
/// Points left when expanded, points right when collapsed.
/// </summary>
public partial class ToggleArrow : UserControl
{
    private readonly RotateTransform _rotateTransform;
    private DispatcherTimer? _animationTimer;
    private double _startAngle;
    private double _targetAngle;
    private double _animationProgress;
    private const double AnimationDuration = 500.0; // milliseconds
    private const double FrameInterval = 16.0; // ~60fps

    /// <summary>
    /// Defines the IsCollapsed property.
    /// When true, the arrow rotates 180 degrees to point right.
    /// </summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<ToggleArrow, bool>(nameof(IsCollapsed));

    /// <summary>
    /// Defines the IconSize property.
    /// Controls both width and height of the arrow icon.
    /// </summary>
    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<ToggleArrow, double>(nameof(IconSize), 18.0);

    /// <summary>
    /// Defines the Foreground property.
    /// Controls the color of the arrow icon.
    /// </summary>
    public new static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<ToggleArrow, IBrush?>(nameof(Foreground));

    /// <summary>
    /// Gets or sets whether the arrow is in collapsed state (rotated 180 degrees).
    /// </summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <summary>
    /// Gets or sets the size of the arrow icon (width and height).
    /// </summary>
    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground brush for the arrow icon.
    /// </summary>
    public new IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public ToggleArrow()
    {
        InitializeComponent();

        _rotateTransform = new RotateTransform();
        ArrowIcon.RenderTransform = _rotateTransform;
        ArrowIcon.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCollapsedProperty)
        {
            var isCollapsed = change.GetNewValue<bool>();
            AnimateRotation(isCollapsed ? 180 : 0);
        }
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // Set initial rotation without animation
        _rotateTransform.Angle = IsCollapsed ? 180 : 0;
    }

    private void AnimateRotation(double targetAngle)
    {
        _animationTimer?.Stop();

        _startAngle = _rotateTransform.Angle;
        _targetAngle = targetAngle;
        _animationProgress = 0;

        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(FrameInterval)
        };
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        _animationProgress += FrameInterval;
        var t = Math.Min(_animationProgress / AnimationDuration, 1.0);

        // Cubic ease-in-out
        var easedT = t < 0.5
            ? 4 * t * t * t
            : 1 - Math.Pow(-2 * t + 2, 3) / 2;

        _rotateTransform.Angle = _startAngle + (_targetAngle - _startAngle) * easedT;

        if (t >= 1.0)
        {
            _animationTimer?.Stop();
            _rotateTransform.Angle = _targetAngle;
        }
    }
}
