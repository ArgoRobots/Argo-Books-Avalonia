using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// A collapsible section in the sidebar containing navigation items.
/// </summary>
public partial class SidebarSection : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SidebarSection, string?>(nameof(Title));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<SidebarSection, bool>(nameof(IsExpanded), true);

    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<SidebarSection, bool>(nameof(IsCollapsed));

    public static readonly StyledProperty<bool> IsCollapsibleProperty =
        AvaloniaProperty.Register<SidebarSection, bool>(nameof(IsCollapsible));

    public static readonly StyledProperty<bool> ShowHeaderProperty =
        AvaloniaProperty.Register<SidebarSection, bool>(nameof(ShowHeader), true);

    public static readonly StyledProperty<ObservableCollection<SidebarItemModel>?> ItemsProperty =
        AvaloniaProperty.Register<SidebarSection, ObservableCollection<SidebarItemModel>?>(nameof(Items));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the section title.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the section is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the sidebar is collapsed (icon-only mode).
    /// </summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the section can be collapsed.
    /// </summary>
    public bool IsCollapsible
    {
        get => GetValue(IsCollapsibleProperty);
        set => SetValue(IsCollapsibleProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the section header.
    /// </summary>
    public bool ShowHeader
    {
        get => GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the collection of items in this section.
    /// </summary>
    public ObservableCollection<SidebarItemModel>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    /// <summary>
    /// Command to toggle the expanded state.
    /// </summary>
    public ICommand ToggleExpandedCommand { get; }

    #endregion

    public SidebarSection()
    {
        ToggleExpandedCommand = new RelayCommand(ToggleExpanded);
        InitializeComponent();
    }

    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }
}

/// <summary>
/// Model for a sidebar navigation item.
/// </summary>
public class SidebarItemModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private string? _text;
    private string? _pageName;
    private string? _iconData;
    private bool _isActive;
    private bool _isVisible = true;
    private bool _isCollapsed;
    private string? _badgeText;
    private bool _showBadge;
    private ICommand? _command;

    /// <summary>
    /// Display text for the item.
    /// </summary>
    public string? Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    /// <summary>
    /// Page name for navigation.
    /// </summary>
    public string? PageName
    {
        get => _pageName;
        set => SetProperty(ref _pageName, value);
    }

    /// <summary>
    /// Icon geometry data as string (parsed as StreamGeometry).
    /// </summary>
    public string? IconData
    {
        get => _iconData;
        set
        {
            if (SetProperty(ref _iconData, value))
            {
                OnPropertyChanged(nameof(Icon));
            }
        }
    }

    /// <summary>
    /// Parsed icon geometry.
    /// </summary>
    public Avalonia.Media.Geometry? Icon =>
        string.IsNullOrEmpty(_iconData) ? null : Avalonia.Media.StreamGeometry.Parse(_iconData);

    /// <summary>
    /// Whether this item is currently active.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    /// <summary>
    /// Whether this item is visible (for feature toggles).
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <summary>
    /// Whether the sidebar is in collapsed mode.
    /// </summary>
    public bool IsCollapsed
    {
        get => _isCollapsed;
        set => SetProperty(ref _isCollapsed, value);
    }

    /// <summary>
    /// Badge text to display.
    /// </summary>
    public string? BadgeText
    {
        get => _badgeText;
        set
        {
            if (SetProperty(ref _badgeText, value))
            {
                OnPropertyChanged(nameof(ShowBadge));
            }
        }
    }

    /// <summary>
    /// Whether to show the badge.
    /// </summary>
    public bool ShowBadge
    {
        get => _showBadge || !string.IsNullOrEmpty(_badgeText);
        set => SetProperty(ref _showBadge, value);
    }

    /// <summary>
    /// Command to execute when clicked.
    /// </summary>
    public ICommand? Command
    {
        get => _command;
        set => SetProperty(ref _command, value);
    }
}
