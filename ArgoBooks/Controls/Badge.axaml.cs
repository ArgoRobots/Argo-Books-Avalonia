using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ArgoBooks.Controls;

/// <summary>
/// Badge color variants.
/// </summary>
public enum BadgeVariant
{
    Primary,
    Success,
    Warning,
    Error,
    Info,
    Neutral
}

/// <summary>
/// Badge size options.
/// </summary>
public enum BadgeSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// A small status indicator/label component.
/// </summary>
public partial class Badge : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<Badge, string?>(nameof(Text));

    public static readonly StyledProperty<BadgeVariant> VariantProperty =
        AvaloniaProperty.Register<Badge, BadgeVariant>(nameof(Variant));

    public static readonly StyledProperty<BadgeSize> SizeProperty =
        AvaloniaProperty.Register<Badge, BadgeSize>(nameof(Size), BadgeSize.Medium);

    public static readonly StyledProperty<bool> IsOutlineProperty =
        AvaloniaProperty.Register<Badge, bool>(nameof(IsOutline));

    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<Badge, Geometry?>(nameof(Icon));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the badge text.
    /// </summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the badge variant (color scheme).
    /// </summary>
    public BadgeVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>
    /// Gets or sets the badge size.
    /// </summary>
    public BadgeSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the badge has an outline style.
    /// </summary>
    public bool IsOutline
    {
        get => GetValue(IsOutlineProperty);
        set => SetValue(IsOutlineProperty, value);
    }

    /// <summary>
    /// Gets or sets the optional icon geometry.
    /// </summary>
    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    #endregion

    public Badge()
    {
        InitializeComponent();
    }
}
