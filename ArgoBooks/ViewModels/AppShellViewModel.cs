using System.Collections.ObjectModel;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the application shell, managing sidebar and header.
/// </summary>
public partial class AppShellViewModel : ViewModelBase
{
    private readonly INavigationService? _navigationService;

    #region ViewModels

    /// <summary>
    /// Gets the sidebar view model.
    /// </summary>
    public SidebarViewModel SidebarViewModel { get; }

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

    #endregion

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public AppShellViewModel() : this(null, null)
    {
    }

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    /// <param name="navigationService">Navigation service.</param>
    /// <param name="settingsService">Settings service.</param>
    public AppShellViewModel(INavigationService? navigationService, ISettingsService? settingsService)
    {
        _navigationService = navigationService;

        // Create sidebar with navigation service
        SidebarViewModel = new SidebarViewModel(navigationService, settingsService);

        // Design-time defaults
        HasUnreadNotifications = true;
        UnreadNotificationCount = 3;
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
        // Clear unread badge when opened
        HasUnreadNotifications = false;
        UnreadNotificationCount = 0;
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
    /// Performs a global search.
    /// </summary>
    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        // TODO: Implement global search
    }

    /// <summary>
    /// Sets the company information on the sidebar.
    /// </summary>
    public void SetCompanyInfo(string? companyName, string? userRole = null)
    {
        SidebarViewModel.SetCompanyInfo(companyName, null, userRole);
    }

    /// <summary>
    /// Updates feature visibility based on settings.
    /// </summary>
    public void UpdateFeatureVisibility(bool showTransactions, bool showInventory, bool showRentals, bool showPayroll)
    {
        SidebarViewModel.UpdateFeatureVisibility(showTransactions, showInventory, showRentals, showPayroll);
    }

    /// <summary>
    /// Navigates to a page programmatically.
    /// </summary>
    public void NavigateTo(string pageName)
    {
        SidebarViewModel.SetActivePage(pageName);
        CurrentPageName = pageName;
        _navigationService?.NavigateTo(pageName);
    }

    /// <summary>
    /// Adds a notification badge.
    /// </summary>
    public void AddNotification()
    {
        UnreadNotificationCount++;
        HasUnreadNotifications = true;
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
