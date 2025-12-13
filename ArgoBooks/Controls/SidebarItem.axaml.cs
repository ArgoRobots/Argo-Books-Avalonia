using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ArgoBooks.Controls;

/// <summary>
/// A navigation item for the sidebar with icon, text, and optional badge.
/// </summary>
public partial class SidebarItem : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<SidebarItem, string?>(nameof(Text));

    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<SidebarItem, Geometry?>(nameof(Icon));

    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<SidebarItem, bool>(nameof(IsActive));

    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<SidebarItem, bool>(nameof(IsCollapsed));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<SidebarItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<SidebarItem, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<SidebarItem, string?>(nameof(BadgeText));

    public static readonly StyledProperty<bool> ShowBadgeProperty =
        AvaloniaProperty.Register<SidebarItem, bool>(nameof(ShowBadge));

    public static readonly StyledProperty<string?> TooltipTextProperty =
        AvaloniaProperty.Register<SidebarItem, string?>(nameof(TooltipText));

    public static readonly StyledProperty<string?> PageNameProperty =
        AvaloniaProperty.Register<SidebarItem, string?>(nameof(PageName));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the item text.
    /// </summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the item icon geometry.
    /// </summary>
    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets whether this item is currently active/selected.
    /// </summary>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the sidebar is collapsed.
    /// </summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
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

    /// <summary>
    /// Gets or sets the badge text (e.g., notification count).
    /// </summary>
    public string? BadgeText
    {
        get => GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the badge.
    /// </summary>
    public bool ShowBadge
    {
        get => GetValue(ShowBadgeProperty);
        set => SetValue(ShowBadgeProperty, value);
    }

    /// <summary>
    /// Gets or sets the tooltip text (shown when collapsed).
    /// </summary>
    public string? TooltipText
    {
        get => GetValue(TooltipTextProperty);
        set => SetValue(TooltipTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the page name this item navigates to.
    /// </summary>
    public string? PageName
    {
        get => GetValue(PageNameProperty);
        set => SetValue(PageNameProperty, value);
    }

    #endregion

    public SidebarItem()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Auto-set tooltip when text changes
        if (change.Property == TextProperty && string.IsNullOrEmpty(TooltipText))
        {
            TooltipText = Text;
        }

        // Auto-set command parameter to page name if not set
        if (change.Property == PageNameProperty && CommandParameter == null)
        {
            CommandParameter = PageName;
        }
    }
}
