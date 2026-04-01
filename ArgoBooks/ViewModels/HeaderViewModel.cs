using System.Collections.ObjectModel;
using ArgoBooks.Core.Services;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
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

    #region Plan Status

    [ObservableProperty]
    private bool _hasPremium;

    /// <summary>
    /// Gets whether to show the upgrade button (only when user doesn't have Premium).
    /// </summary>
    public bool ShowUpgrade => !HasPremium;

    partial void OnHasPremiumChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowUpgrade));
    }

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

    #region Notification Toast

    [ObservableProperty]
    private bool _showNotificationToast;

    [ObservableProperty]
    private NotificationItem? _toastNotification;

    private CancellationTokenSource? _toastCancellationTokenSource;
    private CancellationTokenSource? _savedFeedbackCts;

    private bool _toastsEnabled;

    /// <summary>
    /// Enables toast popups. Call after startup notifications have been sent.
    /// </summary>
    public void EnableToasts() => _toastsEnabled = true;

    #endregion

    #region User

    [ObservableProperty]
    private string? _userDisplayName;

    [ObservableProperty]
    private string? _userInitials;

    [ObservableProperty]
    private int _userId;

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

    #region Save State

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private bool _showSavingIndicator;

    [ObservableProperty]
    private bool _showSavedIndicator;

    [ObservableProperty]
    private double _savedIndicatorOpacity;

    [ObservableProperty]
    private bool _showNoChangesIndicator;

    [ObservableProperty]
    private double _noChangesIndicatorOpacity;

    #endregion

    #region Unsaved Changes Reminder

    [ObservableProperty]
    private bool _showUnsavedChangesReminder;

    private DispatcherTimer? _unsavedChangesReminderTimer;

    /// <summary>
    /// Called when HasUnsavedChanges changes. Starts or stops the reminder timer.
    /// </summary>
    partial void OnHasUnsavedChangesChanged(bool value)
    {
        if (value)
        {
            // Start the timer when there are unsaved changes
            StartUnsavedChangesReminderTimer();
        }
        else
        {
            // Stop the timer and hide the reminder when changes are saved
            StopUnsavedChangesReminderTimer();
            ShowUnsavedChangesReminder = false;
        }
    }

    /// <summary>
    /// Starts the unsaved changes reminder timer based on settings.
    /// </summary>
    private void StartUnsavedChangesReminderTimer()
    {
        var settings = App.CompanyManager?.CompanyData?.Settings.Notifications;
        if (settings == null || !settings.UnsavedChangesReminder)
            return;

        StopUnsavedChangesReminderTimer();

        var minutes = Math.Max(1, settings.UnsavedChangesReminderMinutes);
        _unsavedChangesReminderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(minutes)
        };
        _unsavedChangesReminderTimer.Tick += (_, _) =>
        {
            // Only show if still has unsaved changes and setting is still enabled
            var currentSettings = App.CompanyManager?.CompanyData?.Settings.Notifications;
            if (HasUnsavedChanges && currentSettings?.UnsavedChangesReminder == true)
            {
                ShowUnsavedChangesReminder = true;
            }
            _unsavedChangesReminderTimer?.Stop();
        };
        _unsavedChangesReminderTimer.Start();
    }

    /// <summary>
    /// Stops the unsaved changes reminder timer.
    /// </summary>
    private void StopUnsavedChangesReminderTimer()
    {
        _unsavedChangesReminderTimer?.Stop();
        _unsavedChangesReminderTimer = null;
    }

    /// <summary>
    /// Dismisses the unsaved changes reminder banner and restarts the timer.
    /// </summary>
    [RelayCommand]
    private void DismissUnsavedChangesReminder()
    {
        ShowUnsavedChangesReminder = false;
        // Restart the timer so the banner will appear again after the configured interval
        if (HasUnsavedChanges)
        {
            StartUnsavedChangesReminderTimer();
        }
    }

    /// <summary>
    /// Restarts the unsaved changes reminder timer with current settings.
    /// Call this when the reminder settings change.
    /// </summary>
    public void RestartUnsavedChangesReminderTimer()
    {
        if (HasUnsavedChanges)
        {
            ShowUnsavedChangesReminder = false;
            StartUnsavedChangesReminderTimer();
        }
    }

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
        UserId = 1;
        UserDisplayName = "John Doe";
        UserEmail = "john@example.com";
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

        // Sync HasUnsavedChanges with undo/redo state
        SharedUndoRedoManager.StateChanged += (_, _) =>
        {
            HasUnsavedChanges = !SharedUndoRedoManager.IsAtSavedState;
        };

        // Initialize with the current undo/redo state (should be at saved state initially)
        HasUnsavedChanges = !SharedUndoRedoManager.IsAtSavedState;
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

        _navigationService?.NavigateTo("Search", new Dictionary<string, object?> { { "query", SearchQuery } });
    }

    /// <summary>
    /// Clears the search query.
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = null;
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
    /// Opens the settings modal.
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
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
    /// Saves the current company file.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (HasUnsavedChanges)
            ShowSavingIndicator = true;
        SaveRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens the version history modal.
    /// </summary>
    [RelayCommand]
    private void OpenHistory()
    {
        OpenHistoryRequested?.Invoke(this, EventArgs.Empty);
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
    /// Event raised when settings modal should be opened.
    /// </summary>
    public event EventHandler? OpenSettingsRequested;

    /// <summary>
    /// Event raised when save is requested.
    /// </summary>
    public event EventHandler? SaveRequested;

    /// <summary>
    /// Event raised when version history modal should be opened.
    /// </summary>
    public event EventHandler? OpenHistoryRequested;

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
    public void SetUserInfo(string? displayName, string? email = null, string? role = null, Bitmap? avatarSource = null, int userId = 0)
    {
        UserId = userId;
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
    /// Adds a notification and shows a toast popup for 5 seconds.
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

        ShowToast(notification);
    }

    /// <summary>
    /// Shows a toast popup for the given notification. Auto-dismisses after 5 seconds.
    /// </summary>
    private async void ShowToast(NotificationItem notification)
    {
        try
        {
            if (!_toastsEnabled)
                return;

            // Cancel and dispose any existing toast timer
            _toastCancellationTokenSource?.Cancel();
            _toastCancellationTokenSource?.Dispose();
            var cts = new CancellationTokenSource();
            _toastCancellationTokenSource = cts;

            ToastNotification = notification;
            ShowNotificationToast = true;

            try
            {
                await Task.Delay(10000, cts.Token);
                // Auto-dismiss: leave notification as unread
                ShowNotificationToast = false;
                // Delay clearing so the slide-out animation finishes
                await Task.Delay(350, CancellationToken.None);
                ToastNotification = null;
            }
            catch (TaskCanceledException)
            {
                // Toast was dismissed manually, do nothing
            }
            finally
            {
                cts.Dispose();
                if (_toastCancellationTokenSource == cts)
                    _toastCancellationTokenSource = null;
            }
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.UI, "ShowToast");
        }
    }

    /// <summary>
    /// Dismisses the notification toast and marks the notification as read.
    /// </summary>
    [RelayCommand]
    private async Task DismissNotificationToastAsync()
    {
        _toastCancellationTokenSource?.Cancel();
        _toastCancellationTokenSource?.Dispose();
        _toastCancellationTokenSource = null;

        if (ToastNotification is { IsRead: false } notification)
        {
            notification.IsRead = true;
            UnreadNotificationCount = Math.Max(0, UnreadNotificationCount - 1);
            HasUnreadNotifications = UnreadNotificationCount > 0;
        }

        ShowNotificationToast = false;
        // Delay clearing so the slide-out animation finishes
        await Task.Delay(350);
        ToastNotification = null;
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

    /// <summary>
    /// Shows the appropriate feedback when save is clicked.
    /// Shows "Saved" if there were changes, or "No changes found" if there were none.
    /// </summary>
    public async void ShowSavedFeedback()
    {
        try
        {
            ShowSavingIndicator = false;

            // Cancel any previous feedback animation
            _savedFeedbackCts?.Cancel();
            _savedFeedbackCts?.Dispose();
            var cts = new CancellationTokenSource();
            _savedFeedbackCts = cts;

            if (HasUnsavedChanges)
            {
                // There were changes - show "Saved"
                ShowNoChangesIndicator = false;
                HasUnsavedChanges = false;
                ShowSavedIndicator = true;
                SavedIndicatorOpacity = 1.0;

                await Task.Delay(3000, cts.Token);
                SavedIndicatorOpacity = 0;
                await Task.Delay(300, cts.Token);
                ShowSavedIndicator = false;
            }
            else
            {
                // No changes - show "No changes found"
                ShowSavedIndicator = false;
                ShowNoChangesIndicator = true;
                NoChangesIndicatorOpacity = 1.0;

                await Task.Delay(3000, cts.Token);
                NoChangesIndicatorOpacity = 0;
                await Task.Delay(300, cts.Token);
                ShowNoChangesIndicator = false;
            }
        }
        catch (TaskCanceledException)
        {
            // Superseded by a newer call — do nothing
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.UI, "ShowSavedFeedback");
        }
    }

    #endregion
}

/// <summary>
/// Represents a notification item.
/// </summary>
public class NotificationItem : ObservableObject
{
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
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Whether this is the last notification in the list.
    /// </summary>
    public bool IsLast
    {
        get;
        set => SetProperty(ref field, value);
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
