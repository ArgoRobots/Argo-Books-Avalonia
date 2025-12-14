using System.Collections.ObjectModel;
using ArgoBooks.Core.Services;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the application header.
/// </summary>
public partial class HeaderViewModel : ViewModelBase
{
    private readonly INavigationService? _navigationService;

    #region Page Title

    [ObservableProperty]
    private string? _pageTitle = "Dashboard";

    [ObservableProperty]
    private string? _pageSubtitle;

    [ObservableProperty]
    private bool _showPageTitle = true;

    #endregion

    #region Search

    [ObservableProperty]
    private string? _searchQuery;

    [ObservableProperty]
    private string _searchPlaceholder = "Search transactions, customers, products...";

    [ObservableProperty]
    private bool _showSearch = true;

    [ObservableProperty]
    private bool _showSearchHint = true;

    /// <summary>
    /// Recent search queries for autocomplete.
    /// </summary>
    public ObservableCollection<string> RecentSearches { get; } = [];

    /// <summary>
    /// Search suggestions based on current query.
    /// </summary>
    public ObservableCollection<SearchSuggestion> SearchSuggestions { get; } = [];

    #endregion

    #region Buttons Visibility

    [ObservableProperty]
    private bool _showQuickActions = true;

    [ObservableProperty]
    private bool _showHelp = true;

    [ObservableProperty]
    private bool _showNotifications = true;

    [ObservableProperty]
    private bool _showSettings = true;

    [ObservableProperty]
    private bool _showUserMenu = true;

    #endregion

    #region Notifications

    [ObservableProperty]
    private bool _hasUnreadNotifications;

    [ObservableProperty]
    private int _unreadNotificationCount;

    [ObservableProperty]
    private bool _showNotificationCount = true;

    /// <summary>
    /// List of notifications.
    /// </summary>
    public ObservableCollection<NotificationItem> Notifications { get; } = [];

    #endregion

    #region User

    [ObservableProperty]
    private string? _userDisplayName;

    [ObservableProperty]
    private string? _userInitials;

    [ObservableProperty]
    private string? _userEmail;

    [ObservableProperty]
    private string? _userRole;

    [ObservableProperty]
    private bool _showUserName;

    [ObservableProperty]
    private bool _showUserInitials;

    [ObservableProperty]
    private bool _hasUserAvatar;

    [ObservableProperty]
    private Bitmap? _userAvatarSource;

    #endregion

    #region Undo/Redo

    /// <summary>
    /// The undo/redo button group view model.
    /// </summary>
    public UndoRedoButtonGroupViewModel UndoRedoViewModel { get; } = new();

    /// <summary>
    /// The shared undo/redo manager for the application.
    /// </summary>
    public static Services.UndoRedoManager SharedUndoRedoManager { get; } = new();

    #endregion

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public HeaderViewModel() : this(null)
    {
        // Design-time defaults
        PageTitle = "Dashboard";
        HasUnreadNotifications = true;
        UnreadNotificationCount = 3;
        UserDisplayName = "John Doe";
        UserInitials = "JD";
        UserRole = "Administrator";
    }

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    /// <param name="navigationService">Navigation service.</param>
    public HeaderViewModel(INavigationService? navigationService)
    {
        _navigationService = navigationService;

        // Initialize undo/redo with the shared manager
        UndoRedoViewModel.SetUndoRedoManager(SharedUndoRedoManager);

        // Add test data for undo/redo testing
        AddTestUndoRedoData();
    }

    /// <summary>
    /// Adds test data to the undo/redo stacks for testing purposes.
    /// </summary>
    private void AddTestUndoRedoData()
    {
        // Add some test actions to the undo stack
        SharedUndoRedoManager.RecordAction(new TestAction("Create Invoice #1001"));
        SharedUndoRedoManager.RecordAction(new TestAction("Add Customer: John Smith"));
        SharedUndoRedoManager.RecordAction(new TestAction("Edit Product Price"));
        SharedUndoRedoManager.RecordAction(new TestAction("Delete Payment Record"));
        SharedUndoRedoManager.RecordAction(new TestAction("Update Tax Rate"));

        // Undo a couple to have items in the redo stack
        SharedUndoRedoManager.Undo();
        SharedUndoRedoManager.Undo();
    }

    /// <summary>
    /// Simple test action for undo/redo testing.
    /// </summary>
    private class TestAction : Services.IUndoableAction
    {
        public string Description { get; }

        public TestAction(string description)
        {
            Description = description;
        }

        public void Undo() { /* Test action - no actual operation */ }
        public void Redo() { /* Test action - no actual operation */ }
    }

    #region Commands

    /// <summary>
    /// Performs a global search.
    /// </summary>
    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        // Add to recent searches if not already present
        if (!RecentSearches.Contains(SearchQuery))
        {
            RecentSearches.Insert(0, SearchQuery);

            // Keep only last 10 searches
            while (RecentSearches.Count > 10)
            {
                RecentSearches.RemoveAt(RecentSearches.Count - 1);
            }
        }

        // TODO: Implement global search navigation
        _navigationService?.NavigateTo("Search", new Dictionary<string, object?> { { "query", SearchQuery } });
    }

    /// <summary>
    /// Clears the search query.
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = null;
        SearchSuggestions.Clear();
    }

    /// <summary>
    /// Opens the quick actions panel.
    /// </summary>
    [RelayCommand]
    private void OpenQuickActions()
    {
        OpenQuickActionsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens the file menu.
    /// </summary>
    [RelayCommand]
    private void OpenFileMenu()
    {
        OpenFileMenuRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens the help panel.
    /// </summary>
    [RelayCommand]
    private void OpenHelp()
    {
        OpenHelpRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens the notifications panel.
    /// </summary>
    [RelayCommand]
    private void OpenNotifications()
    {
        OpenNotificationsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens the settings page.
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        _navigationService?.NavigateTo("Settings");
    }

    /// <summary>
    /// Opens the user menu.
    /// </summary>
    [RelayCommand]
    private void OpenUserMenu()
    {
        OpenUserMenuRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    [RelayCommand]
    private void SignOut()
    {
        // TODO: Implement sign out logic
    }

    /// <summary>
    /// Toggles the sidebar collapsed state.
    /// </summary>
    [RelayCommand]
    private void ToggleSidebar()
    {
        // This will be connected to the AppShell to toggle sidebar
        ToggleSidebarRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Performs an undo operation.
    /// </summary>
    [RelayCommand]
    private void Undo()
    {
        // TODO: Implement undo logic
    }

    /// <summary>
    /// Performs a redo operation.
    /// </summary>
    [RelayCommand]
    private void Redo()
    {
        // TODO: Implement redo logic
    }

    /// <summary>
    /// Saves the current company file.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        // TODO: Implement save logic
    }

    /// <summary>
    /// Opens the upgrade dialog.
    /// </summary>
    [RelayCommand]
    private void OpenUpgrade()
    {
        OpenUpgradeRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event raised when sidebar toggle is requested.
    /// </summary>
    public event EventHandler? ToggleSidebarRequested;

    /// <summary>
    /// Event raised when quick actions panel should be opened.
    /// </summary>
    public event EventHandler? OpenQuickActionsRequested;

    /// <summary>
    /// Event raised when notifications panel should be opened.
    /// </summary>
    public event EventHandler? OpenNotificationsRequested;

    /// <summary>
    /// Event raised when user menu panel should be opened.
    /// </summary>
    public event EventHandler? OpenUserMenuRequested;

    /// <summary>
    /// Event raised when file menu should be opened.
    /// </summary>
    public event EventHandler? OpenFileMenuRequested;

    /// <summary>
    /// Event raised when help panel should be opened.
    /// </summary>
    public event EventHandler? OpenHelpRequested;

    /// <summary>
    /// Event raised when upgrade modal should be opened.
    /// </summary>
    public event EventHandler? OpenUpgradeRequested;

    /// <summary>
    /// Event raised when a search key is pressed (for Quick Actions navigation).
    /// </summary>
    public event EventHandler<SearchKeyAction>? SearchKeyPressed;

    /// <summary>
    /// Raises the SearchKeyPressed event.
    /// </summary>
    public void OnSearchKeyPressed(SearchKeyAction action)
    {
        SearchKeyPressed?.Invoke(this, action);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the current page title.
    /// </summary>
    /// <param name="title">Page title.</param>
    /// <param name="subtitle">Optional subtitle.</param>
    public void SetPageTitle(string? title, string? subtitle = null)
    {
        PageTitle = title;
        PageSubtitle = subtitle;
    }

    /// <summary>
    /// Sets the user information.
    /// </summary>
    /// <param name="displayName">User display name.</param>
    /// <param name="email">User email.</param>
    /// <param name="role">User role.</param>
    /// <param name="avatarSource">Optional avatar image.</param>
    public void SetUserInfo(string? displayName, string? email = null, string? role = null, Bitmap? avatarSource = null)
    {
        UserDisplayName = displayName;
        UserEmail = email;
        UserRole = role;
        UserAvatarSource = avatarSource;
        HasUserAvatar = avatarSource != null;

        // Generate initials from display name
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            UserInitials = parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
                : displayName[..Math.Min(2, displayName.Length)].ToUpper();
            ShowUserInitials = !HasUserAvatar;
        }
        else
        {
            UserInitials = null;
            ShowUserInitials = false;
        }
    }

    /// <summary>
    /// Adds a notification.
    /// </summary>
    /// <param name="notification">Notification to add.</param>
    public void AddNotification(NotificationItem notification)
    {
        Notifications.Insert(0, notification);
        if (!notification.IsRead)
        {
            UnreadNotificationCount++;
            HasUnreadNotifications = true;
        }
    }

    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    public void MarkAllNotificationsAsRead()
    {
        foreach (var notification in Notifications)
        {
            notification.IsRead = true;
        }
        UnreadNotificationCount = 0;
        HasUnreadNotifications = false;
    }

    /// <summary>
    /// Clears all notifications.
    /// </summary>
    public void ClearNotifications()
    {
        Notifications.Clear();
        UnreadNotificationCount = 0;
        HasUnreadNotifications = false;
    }

    #endregion

    partial void OnSearchQueryChanged(string? value)
    {
        // Update search suggestions as user types
        UpdateSearchSuggestions(value);
    }

    private void UpdateSearchSuggestions(string? query)
    {
        SearchSuggestions.Clear();

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return;

        // Add matching recent searches
        foreach (var recent in RecentSearches.Where(s => s.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(3))
        {
            SearchSuggestions.Add(new SearchSuggestion
            {
                Text = recent,
                Type = SearchSuggestionType.Recent
            });
        }

        // TODO: Add suggestions from actual data (customers, products, etc.)
    }
}

/// <summary>
/// Represents a search suggestion item.
/// </summary>
public class SearchSuggestion
{
    /// <summary>
    /// The suggestion text.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Secondary text (e.g., category or description).
    /// </summary>
    public string? SecondaryText { get; set; }

    /// <summary>
    /// Type of suggestion for styling.
    /// </summary>
    public SearchSuggestionType Type { get; set; }

    /// <summary>
    /// Optional icon data.
    /// </summary>
    public string? IconData { get; set; }
}

/// <summary>
/// Types of search suggestions.
/// </summary>
public enum SearchSuggestionType
{
    /// <summary>
    /// Recent search query.
    /// </summary>
    Recent,

    /// <summary>
    /// Customer suggestion.
    /// </summary>
    Customer,

    /// <summary>
    /// Product suggestion.
    /// </summary>
    Product,

    /// <summary>
    /// Invoice suggestion.
    /// </summary>
    Invoice,

    /// <summary>
    /// Page/navigation suggestion.
    /// </summary>
    Page
}

/// <summary>
/// Represents a notification item.
/// </summary>
public class NotificationItem : ObservableObject
{
    private bool _isRead;

    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Notification title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Notification message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// When the notification was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Notification type for styling.
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Whether the notification has been read.
    /// </summary>
    public bool IsRead
    {
        get => _isRead;
        set => SetProperty(ref _isRead, value);
    }

    /// <summary>
    /// Optional action to perform when clicked.
    /// </summary>
    public string? ActionRoute { get; set; }

    /// <summary>
    /// Optional parameters for the action.
    /// </summary>
    public Dictionary<string, object?>? ActionParameters { get; set; }
}

/// <summary>
/// Types of notifications.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Informational notification.
    /// </summary>
    Info,

    /// <summary>
    /// Success notification.
    /// </summary>
    Success,

    /// <summary>
    /// Warning notification.
    /// </summary>
    Warning,

    /// <summary>
    /// Error notification.
    /// </summary>
    Error,

    /// <summary>
    /// System/update notification.
    /// </summary>
    System
}

/// <summary>
/// Keyboard actions for search navigation.
/// </summary>
public enum SearchKeyAction
{
    /// <summary>
    /// Escape key pressed - close panel.
    /// </summary>
    Escape,

    /// <summary>
    /// Up arrow key pressed - move selection up.
    /// </summary>
    Up,

    /// <summary>
    /// Down arrow key pressed - move selection down.
    /// </summary>
    Down,

    /// <summary>
    /// Enter key pressed - execute selection.
    /// </summary>
    Enter
}
