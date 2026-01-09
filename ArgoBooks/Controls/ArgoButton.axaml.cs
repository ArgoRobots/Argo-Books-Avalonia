using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ArgoBooks.Controls;

/// <summary>
/// Button variants for styling.
/// </summary>
public enum ButtonVariant
{
    Primary,
    Secondary,
    Ghost,
    Success,
    Danger,
    Warning,
    Icon
}

/// <summary>
/// Button size options.
/// </summary>
public enum ButtonSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// Custom button control with icon support, loading state, and various variants.
/// </summary>
public partial class ArgoButton : UserControl
{
    #region Styled Properties

    /// <summary>
    /// Defines the Text property.
    /// </summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ArgoButton, string?>(nameof(Text));

    /// <summary>
    /// Defines the Variant property.
    /// </summary>
    public static readonly StyledProperty<ButtonVariant> VariantProperty =
        AvaloniaProperty.Register<ArgoButton, ButtonVariant>(nameof(Variant));

    /// <summary>
    /// Defines the Size property.
    /// </summary>
    public static readonly StyledProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.Register<ArgoButton, ButtonSize>(nameof(Size), ButtonSize.Medium);

    /// <summary>
    /// Defines the LeftIcon property.
    /// </summary>
    public static readonly StyledProperty<Geometry?> LeftIconProperty =
        AvaloniaProperty.Register<ArgoButton, Geometry?>(nameof(LeftIcon));

    /// <summary>
    /// Defines the RightIcon property.
    /// </summary>
    public static readonly StyledProperty<Geometry?> RightIconProperty =
        AvaloniaProperty.Register<ArgoButton, Geometry?>(nameof(RightIcon));

    /// <summary>
    /// Defines the IsLoading property.
    /// </summary>
    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<ArgoButton, bool>(nameof(IsLoading));

    /// <summary>
    /// Defines the IsFullWidth property.
    /// </summary>
    public static readonly StyledProperty<bool> IsFullWidthProperty =
        AvaloniaProperty.Register<ArgoButton, bool>(nameof(IsFullWidth));

    /// <summary>
    /// Defines the Command property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<ArgoButton, ICommand?>(nameof(Command));

    /// <summary>
    /// Defines the CommandParameter property.
    /// </summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<ArgoButton, object?>(nameof(CommandParameter));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the button text.
    /// </summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the button variant (style).
    /// </summary>
    public ButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>
    /// Gets or sets the button size.
    /// </summary>
    public ButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the left icon geometry.
    /// </summary>
    public Geometry? LeftIcon
    {
        get => GetValue(LeftIconProperty);
        set => SetValue(LeftIconProperty, value);
    }

    /// <summary>
    /// Gets or sets the right icon geometry.
    /// </summary>
    public Geometry? RightIcon
    {
        get => GetValue(RightIconProperty);
        set => SetValue(RightIconProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the button is in a loading state.
    /// </summary>
    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the button takes full width.
    /// </summary>
    public bool IsFullWidth
    {
        get => GetValue(IsFullWidthProperty);
        set => SetValue(IsFullWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to execute when clicked.
    /// </summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command parameter.
    /// </summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    #endregion

    public ArgoButton()
    {
        InitializeComponent();
    }
}
