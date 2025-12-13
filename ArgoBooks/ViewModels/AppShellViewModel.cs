using System.Collections.ObjectModel;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

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

    /// <summary>
    /// Gets the header view model.
    /// </summary>
    public HeaderViewModel HeaderViewModel { get; }

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

        // Create header with navigation service
        HeaderViewModel = new HeaderViewModel(navigationService);

        // Wire up hamburger menu to toggle sidebar
        HeaderViewModel.ToggleSidebarRequested += (_, _) => SidebarViewModel.IsCollapsed = !SidebarViewModel.IsCollapsed;
    }

    /// <summary>
    /// Sets the company information on the sidebar.
    /// </summary>
    public void SetCompanyInfo(string? companyName, string? userRole = null)
    {
        SidebarViewModel.SetCompanyInfo(companyName, null, userRole);
    }

    /// <summary>
    /// Sets the user information on the header.
    /// </summary>
    public void SetUserInfo(string? displayName, string? email = null, string? role = null)
    {
        HeaderViewModel.SetUserInfo(displayName, email, role);
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
        HeaderViewModel.SetPageTitle(pageName);
        CurrentPageName = pageName;
        _navigationService?.NavigateTo(pageName);
    }

    /// <summary>
    /// Adds a notification.
    /// </summary>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification message.</param>
    /// <param name="type">Notification type.</param>
    public void AddNotification(string title, string message, NotificationType type = NotificationType.Info)
    {
        HeaderViewModel.AddNotification(new NotificationItem
        {
            Title = title,
            Message = message,
            Type = type
        });
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
