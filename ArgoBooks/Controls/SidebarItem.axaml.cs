using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ArgoBooks.Services;

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

    public static readonly StyledProperty<string?> IconDataProperty =
        AvaloniaProperty.Register<SidebarItem, string?>(nameof(IconData));

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

    public static readonly DirectProperty<SidebarItem, string?> EffectiveTooltipTextProperty =
        AvaloniaProperty.RegisterDirect<SidebarItem, string?>(
            nameof(EffectiveTooltipText),
            o => o.EffectiveTooltipText);

    public static readonly DirectProperty<SidebarItem, string?> TranslatedTextProperty =
        AvaloniaProperty.RegisterDirect<SidebarItem, string?>(
            nameof(TranslatedText),
            o => o.TranslatedText);

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
    /// Gets the translated text for display.
    /// </summary>
    public string? TranslatedText => string.IsNullOrEmpty(Text) ? Text : LanguageService.Instance.Translate(Text);

    /// <summary>
    /// Gets or sets the item icon geometry.
    /// </summary>
    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon path data string (alternative to Icon property).
    /// </summary>
    public string? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
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

    /// <summary>
    /// Gets the effective tooltip text (only shown when sidebar is collapsed).
    /// </summary>
    public string? EffectiveTooltipText => IsCollapsed ? TranslatedText : null;

    #endregion

    public SidebarItem()
    {
        InitializeComponent();

        // Subscribe to language changes
        LanguageService.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        // Refresh translated text and tooltip when language changes
        RaisePropertyChanged(TranslatedTextProperty, null, TranslatedText);
        RaisePropertyChanged(EffectiveTooltipTextProperty, null, EffectiveTooltipText);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Update TranslatedText when Text changes
        if (change.Property == TextProperty)
        {
            RaisePropertyChanged(TranslatedTextProperty, null, TranslatedText);
            // Also update tooltip to use translated text
            if (string.IsNullOrEmpty(TooltipText))
            {
                TooltipText = Text;
            }
        }

        // Auto-set command parameter to page name if not set
        if (change.Property == PageNameProperty && CommandParameter == null)
        {
            CommandParameter = PageName;
        }

        // Convert IconData string to Icon geometry
        if (change.Property == IconDataProperty && !string.IsNullOrEmpty(IconData))
        {
            try
            {
                Icon = Geometry.Parse(IconData);
            }
            catch
            {
                // Invalid path data - ignore
            }
        }

        // Update EffectiveTooltipText when IsCollapsed or TooltipText changes
        if (change.Property == IsCollapsedProperty || change.Property == TooltipTextProperty)
        {
            RaisePropertyChanged(EffectiveTooltipTextProperty, null, EffectiveTooltipText);
        }
    }

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        // Unsubscribe from language changes to prevent memory leaks
        LanguageService.Instance.LanguageChanged -= OnLanguageChanged;
    }
}
