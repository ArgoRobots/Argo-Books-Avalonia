using System.Collections.ObjectModel;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the application shell, managing sidebar navigation and header.
/// </summary>
public partial class AppShellViewModel : ViewModelBase
{
    private const double ExpandedSidebarWidth = 240;
    private const double CollapsedSidebarWidth = 64;

    private readonly INavigationService? _navigationService;

    #region Sidebar Properties

    [ObservableProperty]
    private bool _isSidebarCollapsed;

    [ObservableProperty]
    private double _sidebarWidth = ExpandedSidebarWidth;

    [ObservableProperty]
    private string _sidebarToggleTooltip = "Collapse sidebar";

    [ObservableProperty]
    private string? _companyName;

    #endregion

    #region Header Properties

    [ObservableProperty]
    private string? _searchQuery;

    [ObservableProperty]
    private bool _hasUnreadNotifications;

    [ObservableProperty]
    private int _unreadNotificationCount;

    #endregion

    #region Navigation Properties

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private string _currentPageName = "Dashboard";

    /// <summary>
    /// Dictionary to track which page is active for sidebar highlighting.
    /// </summary>
    public ObservableDictionary<string, bool> IsPageActive { get; } = new()
    {
        ["Dashboard"] = true,
        ["Analytics"] = false,
        ["Revenue"] = false,
        ["Expenses"] = false,
        ["Invoices"] = false,
        ["Products"] = false,
        ["StockLevels"] = false,
        ["Customers"] = false,
        ["Suppliers"] = false,
        ["Employees"] = false
    };

    #endregion

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public AppShellViewModel() : this(null)
    {
        // Design-time defaults
        CompanyName = "Sample Company";
        HasUnreadNotifications = true;
        UnreadNotificationCount = 3;
    }

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    /// <param name="navigationService">Navigation service.</param>
    public AppShellViewModel(INavigationService? navigationService)
    {
        _navigationService = navigationService;
    }

    /// <summary>
    /// Updates sidebar width when collapsed state changes.
    /// </summary>
    partial void OnIsSidebarCollapsedChanged(bool value)
    {
        SidebarWidth = value ? CollapsedSidebarWidth : ExpandedSidebarWidth;
        SidebarToggleTooltip = value ? "Expand sidebar" : "Collapse sidebar";
    }

    /// <summary>
    /// Toggles the sidebar between collapsed and expanded states.
    /// </summary>
    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    /// <summary>
    /// Navigates to the specified page.
    /// </summary>
    /// <param name="pageName">Name of the page to navigate to.</param>
    [RelayCommand]
    private void Navigate(string pageName)
    {
        // Update active states
        foreach (var key in IsPageActive.Keys.ToList())
        {
            IsPageActive[key] = key == pageName;
        }

        CurrentPageName = pageName;
        _navigationService?.NavigateTo(pageName);

        // TODO: Set CurrentPage to actual view
        // CurrentPage = _navigationService?.GetView(pageName);
    }

    /// <summary>
    /// Opens the quick actions panel.
    /// </summary>
    [RelayCommand]
    private void OpenQuickActions()
    {
        // TODO: Show quick actions panel
    }

    /// <summary>
    /// Opens the notifications panel.
    /// </summary>
    [RelayCommand]
    private void OpenNotifications()
    {
        // TODO: Show notifications panel
    }

    /// <summary>
    /// Opens the user menu.
    /// </summary>
    [RelayCommand]
    private void OpenUserMenu()
    {
        // TODO: Show user menu
    }

    /// <summary>
    /// Opens the settings dialog.
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        // TODO: Show settings dialog
    }

    /// <summary>
    /// Opens the help panel.
    /// </summary>
    [RelayCommand]
    private void OpenHelp()
    {
        // TODO: Show help panel
    }
}

/// <summary>
/// Observable dictionary that notifies on changes for individual keys.
/// </summary>
public class ObservableDictionary<TKey, TValue> : ObservableCollection<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary = new();

    public TValue this[TKey key]
    {
        get => _dictionary[key];
        set
        {
            _dictionary[key] = value;
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
    }

    public void Add(TKey key, TValue value)
    {
        _dictionary.Add(key, value);
        Add(new KeyValuePair<TKey, TValue>(key, value));
    }

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public ICollection<TKey> Keys => _dictionary.Keys;

    public bool TryGetValue(TKey key, out TValue? value) => _dictionary.TryGetValue(key, out value);
}
