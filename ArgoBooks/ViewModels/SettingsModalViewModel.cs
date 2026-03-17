using System.Collections.ObjectModel;
using System.Diagnostics;
using ArgoBooks.Core;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Portal;
using ArgoBooks.Core.Services;
using ArgoBooks.Data;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Settings modal.
/// </summary>
public partial class SettingsModalViewModel : ViewModelBase
{
    // Store original values for reverting on cancel
    private string _originalTheme;
    private string _originalAccentColor;
    private string _originalLanguage = "English";
    private string _originalDateFormat = "MM/DD/YYYY";
    private TimeZoneItem _originalTimeZone = TimeZones.FindById("UTC");
    private string _originalTimeFormat = "12h";
    private int _originalMaxPieSlices = 6;
    private bool _originalLowStockAlert = true;
    private bool _originalOutOfStockAlert = true;
    private bool _originalInvoiceOverdue = true;
    private bool _originalRentalOverdue = true;
    private bool _originalUnsavedChangesReminder = true;
    private int _originalUnsavedChangesReminderMinutes = 5;

    // Flag to prevent firing LanguageChanged when loading from settings
    private bool _isLoadingLanguage;

    // Flag to indicate if language download is in progress
    [ObservableProperty]
    private bool _isDownloadingLanguage;

    /// <summary>
    /// Event raised when language changes.
    /// </summary>
    public event EventHandler<LanguageSettingsChangedEventArgs>? LanguageSettingsChanged;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private int _selectedTabIndex;

    #region General Settings

    [ObservableProperty]
    private string _selectedLanguage = "English";

    /// <summary>
    /// Called when language selection changes.
    /// </summary>
    partial void OnSelectedLanguageChanged(string value)
    {
        // Don't fire event when loading from company settings
        if (_isLoadingLanguage || string.IsNullOrEmpty(value)) return;

        // Fire event to notify language change is requested
        // The actual language service update happens on save
        LanguageSettingsChanged?.Invoke(this, new LanguageSettingsChangedEventArgs(value, false));
    }

    /// <summary>
    /// Sets the language value without triggering the change event.
    /// Used when syncing UI with company settings on load.
    /// </summary>
    public void SetLanguageWithoutNotify(string value)
    {
        _isLoadingLanguage = true;
        SelectedLanguage = value;
        _isLoadingLanguage = false;
    }

    [ObservableProperty]
    private string _selectedDateFormat = "MM/DD/YYYY";

    [ObservableProperty]
    private TimeZoneItem _selectedTimeZone = TimeZones.FindById("UTC");

    [ObservableProperty]
    private string _selectedTimeFormat = "12h";

    /// <summary>
    /// Whether the 12-hour time format is selected.
    /// </summary>
    public bool Is12HourFormat => SelectedTimeFormat == "12h";

    /// <summary>
    /// Whether the 24-hour time format is selected.
    /// </summary>
    public bool Is24HourFormat => SelectedTimeFormat == "24h";

    partial void OnSelectedTimeFormatChanged(string value)
    {
        OnPropertyChanged(nameof(Is12HourFormat));
        OnPropertyChanged(nameof(Is24HourFormat));
    }

    /// <summary>
    /// Selects the time format.
    /// </summary>
    [RelayCommand]
    private void SelectTimeFormat(string format)
    {
        if (!string.IsNullOrEmpty(format))
        {
            SelectedTimeFormat = format;
        }
    }

    [ObservableProperty]
    private bool _anonymousDataCollection;

    /// <summary>
    /// Called when anonymous data collection setting changes.
    /// </summary>
    partial void OnAnonymousDataCollectionChanged(bool value)
    {
        // Update telemetry consent when toggle changes
        App.TelemetryManager?.SetConsent(value);
    }

    [ObservableProperty]
    private int _telemetryEventCount;

    [ObservableProperty]
    private int _telemetryPendingCount;

    [ObservableProperty]
    private bool _isExportingTelemetry;

    [ObservableProperty]
    private bool _isDeletingTelemetry;

    [ObservableProperty]
    private int _maxPieSlices = 6;

    /// <summary>
    /// Available options for max pie slices.
    /// </summary>
    public int[] MaxPieSlicesOptions { get; } = [4, 5, 6, 7, 8, 10, 12];

    /// <summary>
    /// Priority/common languages shown at the top of the dropdown.
    /// </summary>
    public IReadOnlyList<string> PriorityLanguages => Data.Languages.Priority;

    /// <summary>
    /// All available languages.
    /// </summary>
    public IReadOnlyList<string> Languages => Data.Languages.All;

    public ObservableCollection<string> DateFormats { get; } =
    [
        "MM/DD/YYYY",
        "DD/MM/YYYY",
        "YYYY-MM-DD",
        "MMM D, YYYY"
    ];

    /// <summary>
    /// All available timezone options from the system.
    /// </summary>
    public IReadOnlyList<TimeZoneItem> AllTimeZones => TimeZones.All;

    /// <summary>
    /// Priority timezone options shown at the top of the dropdown.
    /// </summary>
    public IReadOnlyList<TimeZoneItem> PriorityTimeZones => TimeZones.Priority;

    #endregion

    #region Notification Settings

    [ObservableProperty]
    private bool _lowStockAlert = true;

    [ObservableProperty]
    private bool _outOfStockAlert = true;

    [ObservableProperty]
    private bool _invoiceOverdue = true;

    [ObservableProperty]
    private bool _rentalOverdue = true;

    [ObservableProperty]
    private bool _unsavedChangesReminder = true;

    [ObservableProperty]
    private int _unsavedChangesReminderMinutes = 5;

    /// <summary>
    /// Available options for the unsaved changes reminder minutes.
    /// </summary>
    public int[] ReminderMinuteOptions { get; } = [5, 10, 15, 30, 45, 60];

    #endregion

    #region Appearance Settings

    [ObservableProperty]
    private string _selectedTheme;

    [ObservableProperty]
    private string _selectedAccentColor;

    public ObservableCollection<string> Themes { get; } = new(ThemeModeExtensions.GetAllDisplayNames());

    public ObservableCollection<AccentColorItem> AccentColors { get; } =
    [
        new("Blue", AppColors.Primary),
        new("Green", AppColors.Emerald),
        new("Purple", AppColors.Violet),
        new("Pink", AppColors.Pink),
        new("Orange", AppColors.Orange),
        new("Teal", AppColors.Teal)
    ];

    #endregion

    #region Security Settings

    [ObservableProperty]
    private bool _hasPremium; // Whether user has Premium plan

    [ObservableProperty]
    private bool _windowsHelloEnabled;

    [ObservableProperty]
    private string _selectedAutoLock = "5 minutes";

    [ObservableProperty]
    private bool _hasPassword;

    /// <summary>
    /// Whether Windows Hello can be enabled (requires Premium plan AND password).
    /// </summary>
    public bool CanEnableWindowsHello => HasPremium && HasPassword;

    /// <summary>
    /// Whether the user needs to set a password before enabling Windows Hello.
    /// Shows when user has Premium plan but no password.
    /// </summary>
    public bool NeedsPasswordForWindowsHello => HasPremium && !HasPassword;

    /// <summary>
    /// Whether the user needs to set a password before enabling Auto-Lock.
    /// </summary>
    public bool NeedsPasswordForAutoLock => !HasPassword;

    /// <summary>
    /// Event raised when Windows Hello setting changes (after successful authentication).
    /// </summary>
    public event EventHandler<WindowsHelloEventArgs>? WindowsHelloChanged;

    /// <summary>
    /// Event raised to request Windows Hello authentication before enabling.
    /// The handler should authenticate and call OnWindowsHelloAuthResult with the result.
    /// </summary>
    public event EventHandler? WindowsHelloAuthRequested;

    // Flag to prevent recursive updates when setting Windows Hello programmatically
    private bool _isUpdatingWindowsHello;

    /// <summary>
    /// Event raised when user wants to upgrade their plan.
    /// </summary>
    public event EventHandler? UpgradeRequested;

    [ObservableProperty]
    private bool _isAddPasswordModalOpen;

    /// <summary>
    /// Event raised when a password should be added to the company file.
    /// </summary>
    public event EventHandler<PasswordChangeEventArgs>? AddPasswordRequested;

    /// <summary>
    /// Event raised when the password should be changed.
    /// </summary>
    public event EventHandler<PasswordChangeEventArgs>? ChangePasswordRequested;

    /// <summary>
    /// Event raised when the password should be removed.
    /// </summary>
    public event EventHandler<PasswordChangeEventArgs>? RemovePasswordRequested;

    /// <summary>
    /// Event raised when a password textbox should be focused (e.g., after error).
    /// </summary>
    public event EventHandler? FocusPasswordRequested;

    [ObservableProperty]
    private bool _isChangePasswordModalOpen;

    [ObservableProperty]
    private bool _isRemovePasswordModalOpen;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string? _passwordError;

    [ObservableProperty]
    private bool _isNewPasswordVisible;

    [ObservableProperty]
    private bool _isConfirmPasswordVisible;

    [ObservableProperty]
    private bool _isCurrentPasswordVisible;

    /// <summary>
    /// Icon for new password visibility toggle.
    /// </summary>
    public string NewPasswordVisibilityIcon => IsNewPasswordVisible
        ? "M12 7c2.76 0 5 2.24 5 5 0 .65-.13 1.26-.36 1.83l2.92 2.92c1.51-1.26 2.7-2.89 3.43-4.75-1.73-4.39-6-7.5-11-7.5-1.4 0-2.74.25-3.98.7l2.16 2.16C10.74 7.13 11.35 7 12 7zM2 4.27l2.28 2.28.46.46C3.08 8.3 1.78 10.02 1 12c1.73 4.39 6 7.5 11 7.5 1.55 0 3.03-.3 4.38-.84l.42.42L19.73 22 21 20.73 3.27 3 2 4.27zM7.53 9.8l1.55 1.55c-.05.21-.08.43-.08.65 0 1.66 1.34 3 3 3 .22 0 .44-.03.65-.08l1.55 1.55c-.67.33-1.41.53-2.2.53-2.76 0-5-2.24-5-5 0-.79.2-1.53.53-2.2zm4.31-.78l3.15 3.15.02-.16c0-1.66-1.34-3-3-3l-.17.01z"
        : "M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z";

    /// <summary>
    /// Icon for confirm password visibility toggle.
    /// </summary>
    public string ConfirmPasswordVisibilityIcon => IsConfirmPasswordVisible
        ? "M12 7c2.76 0 5 2.24 5 5 0 .65-.13 1.26-.36 1.83l2.92 2.92c1.51-1.26 2.7-2.89 3.43-4.75-1.73-4.39-6-7.5-11-7.5-1.4 0-2.74.25-3.98.7l2.16 2.16C10.74 7.13 11.35 7 12 7zM2 4.27l2.28 2.28.46.46C3.08 8.3 1.78 10.02 1 12c1.73 4.39 6 7.5 11 7.5 1.55 0 3.03-.3 4.38-.84l.42.42L19.73 22 21 20.73 3.27 3 2 4.27zM7.53 9.8l1.55 1.55c-.05.21-.08.43-.08.65 0 1.66 1.34 3 3 3 .22 0 .44-.03.65-.08l1.55 1.55c-.67.33-1.41.53-2.2.53-2.76 0-5-2.24-5-5 0-.79.2-1.53.53-2.2zm4.31-.78l3.15 3.15.02-.16c0-1.66-1.34-3-3-3l-.17.01z"
        : "M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z";

    /// <summary>
    /// Icon for current password visibility toggle.
    /// </summary>
    public string CurrentPasswordVisibilityIcon => IsCurrentPasswordVisible
        ? "M12 7c2.76 0 5 2.24 5 5 0 .65-.13 1.26-.36 1.83l2.92 2.92c1.51-1.26 2.7-2.89 3.43-4.75-1.73-4.39-6-7.5-11-7.5-1.4 0-2.74.25-3.98.7l2.16 2.16C10.74 7.13 11.35 7 12 7zM2 4.27l2.28 2.28.46.46C3.08 8.3 1.78 10.02 1 12c1.73 4.39 6 7.5 11 7.5 1.55 0 3.03-.3 4.38-.84l.42.42L19.73 22 21 20.73 3.27 3 2 4.27zM7.53 9.8l1.55 1.55c-.05.21-.08.43-.08.65 0 1.66 1.34 3 3 3 .22 0 .44-.03.65-.08l1.55 1.55c-.67.33-1.41.53-2.2.53-2.76 0-5-2.24-5-5 0-.79.2-1.53.53-2.2zm4.31-.78l3.15 3.15.02-.16c0-1.66-1.34-3-3-3l-.17.01z"
        : "M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z";

    partial void OnIsNewPasswordVisibleChanged(bool value) => OnPropertyChanged(nameof(NewPasswordVisibilityIcon));
    partial void OnIsConfirmPasswordVisibleChanged(bool value) => OnPropertyChanged(nameof(ConfirmPasswordVisibilityIcon));
    partial void OnIsCurrentPasswordVisibleChanged(bool value) => OnPropertyChanged(nameof(CurrentPasswordVisibilityIcon));

    // Flag to prevent firing AutoLockSettingsChanged when syncing UI with company settings
    private bool _isLoadingAutoLock;

    /// <summary>
    /// Called when HasPassword changes - notify dependent properties.
    /// </summary>
    partial void OnHasPasswordChanged(bool value)
    {
        // Notify Windows Hello and Auto-Lock computed properties
        OnPropertyChanged(nameof(CanEnableWindowsHello));
        OnPropertyChanged(nameof(NeedsPasswordForWindowsHello));
        OnPropertyChanged(nameof(NeedsPasswordForAutoLock));

        // Disable Windows Hello if password is removed
        if (!value && WindowsHelloEnabled)
        {
            WindowsHelloEnabled = false;
        }
    }

    /// <summary>
    /// Called when HasPremium changes - notify Windows Hello properties.
    /// </summary>
    partial void OnHasPremiumChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEnableWindowsHello));
        OnPropertyChanged(nameof(NeedsPasswordForWindowsHello));
    }

    /// <summary>
    /// Called when Windows Hello setting changes.
    /// </summary>
    partial void OnWindowsHelloEnabledChanged(bool value)
    {
        // Skip if we're programmatically updating (e.g., after auth result)
        if (_isUpdatingWindowsHello) return;

        if (value)
        {
            // User is trying to enable - request authentication first
            WindowsHelloAuthRequested?.Invoke(this, EventArgs.Empty);
            // The actual enabling will happen in OnWindowsHelloAuthResult
        }
        else
        {
            // Disabling doesn't require authentication
            WindowsHelloChanged?.Invoke(this, new WindowsHelloEventArgs(false));
        }
    }

    /// <summary>
    /// Called after Windows Hello authentication attempt.
    /// </summary>
    /// <param name="success">Whether authentication was successful.</param>
    public void OnWindowsHelloAuthResult(bool success)
    {
        _isUpdatingWindowsHello = true;
        if (success)
        {
            // Authentication succeeded - keep enabled and fire event
            WindowsHelloChanged?.Invoke(this, new WindowsHelloEventArgs(true));
        }
        else
        {
            // Authentication failed or cancelled - revert the toggle
            WindowsHelloEnabled = false;
        }
        _isUpdatingWindowsHello = false;
    }

    /// <summary>
    /// Sets Windows Hello enabled state without triggering authentication.
    /// Used when loading settings from company file.
    /// </summary>
    public void SetWindowsHelloWithoutAuth(bool enabled)
    {
        _isUpdatingWindowsHello = true;
        WindowsHelloEnabled = enabled;
        _isUpdatingWindowsHello = false;
    }

    /// <summary>
    /// Called when auto-lock setting changes.
    /// </summary>
    partial void OnSelectedAutoLockChanged(string value)
    {
        // Don't fire event when loading from company settings
        if (_isLoadingAutoLock) return;

        AutoLockSettingsChanged?.Invoke(this, new AutoLockSettingsEventArgs(value));
    }

    /// <summary>
    /// Sets the auto-lock value without triggering the change event.
    /// Used when syncing UI with company settings on load.
    /// </summary>
    public void SetAutoLockWithoutNotify(string value)
    {
        _isLoadingAutoLock = true;
        SelectedAutoLock = value;
        _isLoadingAutoLock = false;
    }

    /// <summary>
    /// Event raised when auto-lock settings change.
    /// </summary>
    public event EventHandler<AutoLockSettingsEventArgs>? AutoLockSettingsChanged;

    public ObservableCollection<string> AutoLockOptions { get; } =
    [
        "Never",
        "5 minutes",
        "15 minutes",
        "30 minutes",
        "1 hour"
    ];

    #endregion

    #region Payment Portal Settings

    /// <summary>
    /// Whether the user has authenticated to modify portal settings in this session.
    /// Reset when the settings modal is reopened.
    /// </summary>
    private bool _isPortalAuthenticated;

    // Flag to suppress auth check when loading portal settings from company data
    private bool _isLoadingPortalSettings;

    /// <summary>
    /// Event raised when portal authentication is required before making changes.
    /// The handler should verify the user's identity (password or biometric) and call
    /// <see cref="OnPortalAuthResult"/> with the result.
    /// </summary>
    public event Func<Task<bool>>? PortalAuthenticationRequested;

    /// <summary>
    /// Ensures the user has authenticated to modify portal settings.
    /// On first interaction, prompts for password/biometric. Once authenticated,
    /// subsequent changes are allowed without re-prompting.
    /// </summary>
    /// <returns>True if authenticated, false if cancelled or no password is set.</returns>
    private async Task<bool> EnsurePortalAuthenticatedAsync()
    {
        if (_isPortalAuthenticated) return true;
        if (!HasPassword) return true; // No password set, no auth needed

        if (PortalAuthenticationRequested != null)
        {
            var result = await PortalAuthenticationRequested.Invoke();
            if (result)
            {
                _isPortalAuthenticated = true;
                return true;
            }
            return false;
        }

        // No handler wired up — allow by default
        return true;
    }

    /// <summary>
    /// Called by App.axaml.cs to set the portal authentication result.
    /// </summary>
    public void OnPortalAuthResult(bool success)
    {
        _isPortalAuthenticated = success;
    }

    [ObservableProperty]
    private bool _portalNotifyOnPayment = true;

    partial void OnHasPortalLogoChanged(bool value) => OnPropertyChanged(nameof(PortalLogoButtonText));

    /// <summary>
    /// Called when PortalNotifyOnPayment changes — requires auth if password is enabled.
    /// </summary>
    partial void OnPortalNotifyOnPaymentChanged(bool value)
    {
        if (_isLoadingPortalSettings) return;
        if (!HasPassword || _isPortalAuthenticated) return;

        // Revert and request auth
        _ = RevertAndAuthPortalNotifyAsync(value);
    }

    private async Task RevertAndAuthPortalNotifyAsync(bool attemptedValue)
    {
        // Revert to opposite while we authenticate
        _isLoadingPortalSettings = true;
        PortalNotifyOnPayment = !attemptedValue;
        _isLoadingPortalSettings = false;

        if (await EnsurePortalAuthenticatedAsync())
        {
            _isLoadingPortalSettings = true;
            PortalNotifyOnPayment = attemptedValue;
            _isLoadingPortalSettings = false;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSyncIntervalNumeric))]
    private string _portalSyncInterval = "5";

    /// <summary>
    /// Called when PortalSyncInterval changes — requires auth if password is enabled.
    /// </summary>
    partial void OnPortalSyncIntervalChanged(string value)
    {
        if (_isLoadingPortalSettings) return;
        if (!HasPassword || _isPortalAuthenticated) return;

        _ = RevertAndAuthPortalSyncIntervalAsync(value);
    }

    private string? _previousSyncInterval;

    private async Task RevertAndAuthPortalSyncIntervalAsync(string attemptedValue)
    {
        var previousValue = _previousSyncInterval ?? "5";

        _isLoadingPortalSettings = true;
        PortalSyncInterval = previousValue;
        _isLoadingPortalSettings = false;

        if (await EnsurePortalAuthenticatedAsync())
        {
            _isLoadingPortalSettings = true;
            PortalSyncInterval = attemptedValue;
            _isLoadingPortalSettings = false;
        }
    }

    public bool IsSyncIntervalNumeric => PortalSyncInterval != "Manual";

    [ObservableProperty]
    private bool _stripeConnected;

    [ObservableProperty]
    private string? _stripeEmail;

    [ObservableProperty]
    private bool _paypalConnected;

    [ObservableProperty]
    private string? _paypalEmail;

    [ObservableProperty]
    private bool _squareConnected;

    [ObservableProperty]
    private string? _squareEmail;

    [ObservableProperty]
    private bool _isConnectingProvider;

    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _portalLogoSource;

    [ObservableProperty]
    private bool _hasPortalLogo;

    public string PortalLogoButtonText => HasPortalLogo ? "Change".Translate() : "Upload".Translate();

    [ObservableProperty]
    private bool _isUploadingPortalLogo;

    public string[] SyncIntervalOptions { get; } = ["Manual", "1", "2", "5", "10", "15", "30"];

    [RelayCommand]
    private async Task ConnectStripeAsync()
    {
        if (!await EnsurePortalAuthenticatedAsync()) return;
        await ConnectProviderAsync("stripe");
    }

    [RelayCommand]
    private async Task ConnectPaypalAsync()
    {
        if (!await EnsurePortalAuthenticatedAsync()) return;
        await ConnectProviderAsync("paypal");
    }

    [RelayCommand]
    private async Task ConnectSquareAsync()
    {
        if (!await EnsurePortalAuthenticatedAsync()) return;
        await ConnectProviderAsync("square");
    }

    [RelayCommand]
    private async Task DisconnectStripeAsync()
    {
        if (!await EnsurePortalAuthenticatedAsync()) return;
        await DisconnectProviderAsync("stripe");
    }

    [RelayCommand]
    private async Task DisconnectPaypalAsync()
    {
        if (!await EnsurePortalAuthenticatedAsync()) return;
        await DisconnectProviderAsync("paypal");
    }

    [RelayCommand]
    private async Task DisconnectSquareAsync()
    {
        if (!await EnsurePortalAuthenticatedAsync()) return;
        await DisconnectProviderAsync("square");
    }

    /// <summary>
    /// Event raised when the portal logo browse button is clicked.
    /// Handled in App.axaml.cs to open the file picker.
    /// </summary>
    public event EventHandler? BrowsePortalLogoRequested;

    [RelayCommand]
    private async Task BrowsePortalLogoAsync()
    {
        if (!await EnsurePortalAuthenticatedAsync()) return;
        BrowsePortalLogoRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Uploads the selected file as the portal logo.
    /// Called from App.axaml.cs after the file picker returns.
    /// </summary>
    public async Task UploadPortalLogoFromFileAsync(string filePath)
    {
        var portalService = App.PaymentPortalService;
        if (portalService == null || !PortalSettings.IsConfigured) return;

        IsUploadingPortalLogo = true;
        try
        {
            var result = await portalService.UploadCompanyLogoAsync(filePath);
            if (result.Success)
            {
                try
                {
                    PortalLogoSource = new Avalonia.Media.Imaging.Bitmap(filePath);
                    HasPortalLogo = true;
                }
                catch
                {
                    // File may not be a valid image for Avalonia, but upload succeeded
                    HasPortalLogo = !string.IsNullOrEmpty(result.LogoUrl);
                }
            }
            else
            {
                await ShowErrorDialogAsync("Upload Failed".Translate(),
                    (result.Message ?? "Failed to upload logo.").Translate());
            }
        }
        catch
        {
            await ShowErrorDialogAsync("Error".Translate(),
                "Failed to upload logo. Please check your internet connection.".Translate());
        }
        finally
        {
            IsUploadingPortalLogo = false;
        }
    }

    [RelayCommand]
    private async Task RemovePortalLogoAsync()
    {
        if (!await EnsurePortalAuthenticatedAsync()) return;

        var portalService = App.PaymentPortalService;
        if (portalService == null || !PortalSettings.IsConfigured) return;

        IsUploadingPortalLogo = true;
        try
        {
            var result = await portalService.DeleteCompanyLogoAsync();
            if (result.Success)
            {
                PortalLogoSource = null;
                HasPortalLogo = false;
            }
        }
        catch
        {
            // Silently fail
        }
        finally
        {
            IsUploadingPortalLogo = false;
        }
    }

    /// <summary>
    /// Loads the portal logo from its URL on the server.
    /// </summary>
    private async Task LoadPortalLogoFromUrlAsync(string? logoUrl)
    {
        if (string.IsNullOrEmpty(logoUrl))
        {
            PortalLogoSource = null;
            HasPortalLogo = false;
            return;
        }

        HasPortalLogo = true;

        try
        {
            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(logoUrl);
            using var stream = new MemoryStream(imageBytes);
            PortalLogoSource = new Avalonia.Media.Imaging.Bitmap(stream);
        }
        catch
        {
            // Failed to load image — still show HasPortalLogo since URL exists
            PortalLogoSource = null;
        }
    }

    private async Task ConnectProviderAsync(string provider)
    {
        var portalService = App.PaymentPortalService;
        if (portalService == null) return;

        // If no API key exists, try to auto-register first
        if (!PortalSettings.IsConfigured)
        {
            var registered = await TryRegisterPortalAsync(portalService);
            if (!registered) return;
        }

        IsConnectingProvider = true;
        try
        {
            var response = await portalService.InitiateConnectAsync(provider);
            if (response.Success && !string.IsNullOrEmpty(response.AuthUrl)
                && Uri.TryCreate(response.AuthUrl, UriKind.Absolute, out var authUri)
                && (authUri.Scheme == "https" || authUri.Scheme == "http"))
            {
                // Open OAuth URL in default browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = response.AuthUrl,
                    UseShellExecute = true
                });

                // Poll for status updates so the UI refreshes when the user completes OAuth
                _ = PollForProviderConnectionAsync(provider);
            }
            else
            {
                var message = !string.IsNullOrEmpty(response.Message)
                    ? response.Message
                    : $"Could not connect to {provider}. The payment portal server may be unavailable.";
                await ShowErrorDialogAsync("Connection Failed".Translate(), message.Translate());
            }
        }
        catch
        {
            await ShowErrorDialogAsync("Error".Translate(),
                "Failed to connect payment provider. Please check your internet connection.".Translate());
        }
        finally
        {
            IsConnectingProvider = false;
        }
    }

    /// <summary>
    /// Attempts to register the company with the portal using the premium license key.
    /// Returns true if registration succeeded (or was already done), false otherwise.
    /// </summary>
    private async Task<bool> TryRegisterPortalAsync(PaymentPortalService portalService)
    {
        var licenseService = App.LicenseService;
        var licenseKey = licenseService?.GetLicenseKey();
        if (string.IsNullOrEmpty(licenseKey))
        {
            await ShowErrorDialogAsync(
                "Premium License Required".Translate(),
                "Please activate a premium license first to use portal features.".Translate());
            return false;
        }

        var deviceId = licenseService!.GetDeviceId();

        IsConnectingProvider = true;
        try
        {
            var companyData = App.CompanyManager?.CompanyData;
            var companyName = companyData?.Settings.Company.Name ?? "My Company";
            var ownerEmail = companyData?.Settings.Company.Email;

            var result = await portalService.RegisterCompanyAsync(licenseKey, deviceId, companyName, ownerEmail);
            if (result.Success && !string.IsNullOrEmpty(result.ApiKey))
            {
                return true;
            }

            var message = result.Message ?? "Registration failed. Please check your license key.";
            await ShowErrorDialogAsync("Registration Failed".Translate(), message.Translate());
            return false;
        }
        catch
        {
            await ShowErrorDialogAsync("Error".Translate(),
                "Failed to register with the payment portal. Please check your internet connection.".Translate());
            return false;
        }
        finally
        {
            IsConnectingProvider = false;
        }
    }

    private async Task DisconnectProviderAsync(string provider)
    {
        var dialog = App.ConfirmationDialog;
        if (dialog != null)
        {
            var result = await dialog.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Disconnect Provider".Translate(),
                Message = "Are you sure you want to disconnect this payment provider? Customers will no longer be able to pay using this method.".Translate(),
                PrimaryButtonText = "Disconnect".Translate(),
                CancelButtonText = "Cancel".Translate()
            });

            if (result != ConfirmationResult.Primary) return;
        }

        var portalService = App.PaymentPortalService;
        if (portalService == null) return;

        try
        {
            var response = await portalService.DisconnectProviderAsync(provider);
            if (response.Success)
            {
                // Use the server's authoritative connected provider state from the response
                if (response.ConnectedProviders != null)
                {
                    StripeConnected = response.ConnectedProviders.StripeConnected;
                    StripeEmail = response.ConnectedProviders.StripeEmail;
                    PaypalConnected = response.ConnectedProviders.PaypalConnected;
                    PaypalEmail = response.ConnectedProviders.PaypalEmail;
                    SquareConnected = response.ConnectedProviders.SquareConnected;
                    SquareEmail = response.ConnectedProviders.SquareEmail;
                }
                else
                {
                    // Fallback: clear the specific provider if response didn't include full state
                    switch (provider)
                    {
                        case "stripe":
                            StripeConnected = false;
                            StripeEmail = null;
                            break;
                        case "paypal":
                            PaypalConnected = false;
                            PaypalEmail = null;
                            break;
                        case "square":
                            SquareConnected = false;
                            SquareEmail = null;
                            break;
                    }
                }

                // Persist changes to local settings immediately
                SavePortalSettings();

                // Notify invoice views and other subscribers that provider state changed
                PaymentProviderService.NotifyProvidersChanged();
            }
        }
        catch
        {
            await ShowErrorDialogAsync("Error".Translate(),
                "Failed to disconnect provider. Please try again.".Translate());
        }
    }

    private static async Task ShowErrorDialogAsync(string title, string message)
    {
        var dialog = App.ConfirmationDialog;
        if (dialog != null)
        {
            await dialog.ShowAsync(new ConfirmationDialogOptions
            {
                Title = title,
                Message = message,
                PrimaryButtonText = "OK".Translate(),
                CancelButtonText = null
            });
        }
    }

    private void LoadPortalSettings()
    {
        var settings = App.CompanyManager?.CompanyData?.Settings.PaymentPortal;
        if (settings == null) return;

        _isLoadingPortalSettings = true;
        PortalNotifyOnPayment = settings.NotifyOnPayment;
        PortalSyncInterval = settings.AutoSyncIntervalMinutes == 0
            ? "Manual"
            : settings.AutoSyncIntervalMinutes.ToString();
        _previousSyncInterval = PortalSyncInterval;
        _isLoadingPortalSettings = false;

        StripeConnected = settings.ConnectedAccounts.StripeConnected;
        StripeEmail = settings.ConnectedAccounts.StripeEmail;
        PaypalConnected = settings.ConnectedAccounts.PaypalConnected;
        PaypalEmail = settings.ConnectedAccounts.PaypalEmail;
        SquareConnected = settings.ConnectedAccounts.SquareConnected;
        SquareEmail = settings.ConnectedAccounts.SquareEmail;

        // Fetch fresh provider status from the server in the background
        _ = RefreshProviderStatusAsync();
    }

    private async Task RefreshProviderStatusAsync()
    {
        var portalService = App.PaymentPortalService;
        if (portalService == null || !PortalSettings.IsConfigured) return;

        try
        {
            var status = await portalService.CheckStatusAsync();
            if (status.Success && status.ConnectedProviders != null)
            {
                // Only treat a provider as connected if the server also returns a valid email,
                // which confirms the OAuth flow actually completed (not just initiated).
                StripeConnected = status.ConnectedProviders.StripeConnected
                    && !string.IsNullOrEmpty(status.ConnectedProviders.StripeEmail);
                StripeEmail = status.ConnectedProviders.StripeEmail;
                PaypalConnected = status.ConnectedProviders.PaypalConnected
                    && !string.IsNullOrEmpty(status.ConnectedProviders.PaypalEmail);
                PaypalEmail = status.ConnectedProviders.PaypalEmail;
                SquareConnected = status.ConnectedProviders.SquareConnected
                    && !string.IsNullOrEmpty(status.ConnectedProviders.SquareEmail);
                SquareEmail = status.ConnectedProviders.SquareEmail;
                SavePortalSettings();

                // Persist PortalUrl so other pages (e.g. Invoices) see it immediately
                if (!string.IsNullOrEmpty(status.PortalUrl))
                {
                    var portalSettings = App.CompanyManager?.CompanyData?.Settings.PaymentPortal;
                    if (portalSettings != null)
                        portalSettings.PortalUrl = status.PortalUrl;
                }

                // Notify invoice views and other subscribers that provider state changed
                PaymentProviderService.NotifyProvidersChanged();
            }

            // Load portal logo from server
            if (status.Success)
            {
                await LoadPortalLogoFromUrlAsync(status.Company?.LogoUrl);
            }
        }
        catch
        {
            // Silently fail — local cached values are still shown
        }
    }

    private async Task PollForProviderConnectionAsync(string provider)
    {
        var portalService = App.PaymentPortalService;
        if (portalService == null) return;

        // Poll every 3 seconds for up to 5 minutes
        const int intervalMs = 3000;
        const int maxAttempts = 100;

        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(intervalMs);

            try
            {
                var status = await portalService.CheckStatusAsync();
                if (status.Success && status.ConnectedProviders != null)
                {
                    var connected = provider switch
                    {
                        "stripe" => status.ConnectedProviders.StripeConnected,
                        "paypal" => status.ConnectedProviders.PaypalConnected,
                        "square" => status.ConnectedProviders.SquareConnected,
                        _ => false
                    };

                    // Require both connected flag AND a valid email to confirm OAuth completed
                    var email = provider switch
                    {
                        "stripe" => status.ConnectedProviders.StripeEmail,
                        "paypal" => status.ConnectedProviders.PaypalEmail,
                        "square" => status.ConnectedProviders.SquareEmail,
                        _ => null
                    };

                    if (connected && !string.IsNullOrEmpty(email))
                    {
                        // Dispatch property updates to the UI thread to ensure bindings refresh
                        Dispatcher.UIThread.Post(() =>
                        {
                            StripeConnected = status.ConnectedProviders.StripeConnected;
                            StripeEmail = status.ConnectedProviders.StripeEmail;
                            PaypalConnected = status.ConnectedProviders.PaypalConnected;
                            PaypalEmail = status.ConnectedProviders.PaypalEmail;
                            SquareConnected = status.ConnectedProviders.SquareConnected;
                            SquareEmail = status.ConnectedProviders.SquareEmail;
                            SavePortalSettings();

                            // Persist PortalUrl so other pages (e.g. Invoices) see it immediately
                            if (!string.IsNullOrEmpty(status.PortalUrl))
                            {
                                var portalSettings = App.CompanyManager?.CompanyData?.Settings.PaymentPortal;
                                if (portalSettings != null)
                                    portalSettings.PortalUrl = status.PortalUrl;
                            }

                            // Notify invoice views and other subscribers that provider state changed
                            PaymentProviderService.NotifyProvidersChanged();
                        });
                        return;
                    }
                }
            }
            catch
            {
                // Ignore transient errors and keep polling
            }
        }
    }

    private void SavePortalSettings()
    {
        var settings = App.CompanyManager?.CompanyData?.Settings.PaymentPortal;
        if (settings == null) return;

        settings.NotifyOnPayment = PortalNotifyOnPayment;
        settings.AutoSyncIntervalMinutes = PortalSyncInterval == "Manual"
            ? 0
            : int.TryParse(PortalSyncInterval, out var mins) ? mins : 5;

        // Track the saved sync interval for revert-on-auth
        _previousSyncInterval = PortalSyncInterval;

        settings.ConnectedAccounts.StripeConnected = StripeConnected;
        settings.ConnectedAccounts.StripeEmail = StripeEmail;
        settings.ConnectedAccounts.PaypalConnected = PaypalConnected;
        settings.ConnectedAccounts.PaypalEmail = PaypalEmail;
        settings.ConnectedAccounts.SquareConnected = SquareConnected;
        settings.ConnectedAccounts.SquareEmail = SquareEmail;
    }

    #endregion

    /// <summary>
    /// Whether there are unsaved changes in the settings.
    /// </summary>
    public bool HasUnsavedChanges =>
        SelectedTheme != _originalTheme ||
        SelectedAccentColor != _originalAccentColor ||
        SelectedLanguage != _originalLanguage ||
        SelectedDateFormat != _originalDateFormat ||
        SelectedTimeZone.Id != _originalTimeZone.Id ||
        SelectedTimeFormat != _originalTimeFormat ||
        MaxPieSlices != _originalMaxPieSlices ||
        LowStockAlert != _originalLowStockAlert ||
        OutOfStockAlert != _originalOutOfStockAlert ||
        InvoiceOverdue != _originalInvoiceOverdue ||
        RentalOverdue != _originalRentalOverdue ||
        UnsavedChangesReminder != _originalUnsavedChangesReminder ||
        UnsavedChangesReminderMinutes != _originalUnsavedChangesReminderMinutes;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public SettingsModalViewModel()
    {
        // Sync with current ThemeService values
        _selectedTheme = ThemeService.Instance.CurrentThemeName;
        _selectedAccentColor = ThemeService.Instance.CurrentAccentColor;
        _originalTheme = _selectedTheme;
        _originalAccentColor = _selectedAccentColor;
    }

    #region Commands

    /// <summary>
    /// Opens the settings modal.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        OpenWithTab(0);
    }

    /// <summary>
    /// Opens the settings modal with a specific tab selected.
    /// </summary>
    /// <param name="tabIndex">The tab index to select (0=General, 1=Notifications, 2=Appearance, 3=Security, 4=Payment Portal).</param>
    public void OpenWithTab(int tabIndex)
    {
        // Reset portal authentication — require re-auth each time settings opens
        _isPortalAuthenticated = false;

        // Sync with current ThemeService values
        SelectedTheme = ThemeService.Instance.CurrentThemeName;
        SelectedAccentColor = ThemeService.Instance.CurrentAccentColor;

        // Load language, date format and currency from company settings
        var settings = App.CompanyManager?.CompanyData?.Settings;
        if (settings != null)
        {
            // Load language without triggering change event
            SetLanguageWithoutNotify(settings.Localization.Language);
            SelectedDateFormat = settings.Localization.DateFormat;

            // Load notification settings
            LowStockAlert = settings.Notifications.LowStockAlert;
            OutOfStockAlert = settings.Notifications.OutOfStockAlert;
            InvoiceOverdue = settings.Notifications.InvoiceOverdueAlert;
            RentalOverdue = settings.Notifications.RentalOverdueAlert;
            UnsavedChangesReminder = settings.Notifications.UnsavedChangesReminder;
            UnsavedChangesReminderMinutes = settings.Notifications.UnsavedChangesReminderMinutes;
        }
        else
        {
            // Load from global settings when no company is open
            var globalSettings = App.SettingsService?.GlobalSettings;
            SetLanguageWithoutNotify(globalSettings != null
                ? globalSettings.Ui.Language
                : LanguageService.Instance.CurrentLanguage);
        }

        // Load max pie slices, timezone and time format from global settings
        {
            var globalSettings = App.SettingsService?.GlobalSettings;
            if (globalSettings != null)
            {
                MaxPieSlices = globalSettings.Ui.Chart.MaxPieSlices;
                SelectedTimeZone = TimeZones.FindById(globalSettings.Ui.TimeZone);
                SelectedTimeFormat = globalSettings.Ui.TimeFormat;

                // Load privacy settings
                AnonymousDataCollection = globalSettings.Privacy.AnonymousDataCollectionConsent;
            }
        }

        // Load portal settings
        LoadPortalSettings();

        // Refresh telemetry stats
        _ = RefreshTelemetryStatsAsync();

        // Store original values for potential revert
        _originalTheme = SelectedTheme;
        _originalAccentColor = SelectedAccentColor;
        _originalLanguage = SelectedLanguage;
        _originalDateFormat = SelectedDateFormat;
        _originalTimeZone = SelectedTimeZone;
        _originalTimeFormat = SelectedTimeFormat;
        _originalMaxPieSlices = MaxPieSlices;
        _originalLowStockAlert = LowStockAlert;
        _originalOutOfStockAlert = OutOfStockAlert;
        _originalInvoiceOverdue = InvoiceOverdue;
        _originalRentalOverdue = RentalOverdue;
        _originalUnsavedChangesReminder = UnsavedChangesReminder;
        _originalUnsavedChangesReminderMinutes = UnsavedChangesReminderMinutes;
        SelectedTabIndex = tabIndex;
        IsOpen = true;
    }

    /// <summary>
    /// Closes the settings modal, prompting to save if there are unsaved changes.
    /// </summary>
    [RelayCommand]
    private async Task CloseAsync()
    {
        if (HasUnsavedChanges)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions()
                {
                    Title = "Unsaved Changes".Translate(),
                    Message = "You have unsaved changes to your settings. Do you want to save them before closing?".Translate(),
                    PrimaryButtonText = "Save".Translate(),
                    SecondaryButtonText = "Don't Save".Translate(),
                    CancelButtonText = "Cancel".Translate()
                });

                switch (result)
                {
                    case ConfirmationResult.Primary:
                        // Save and close
                        await SaveAsync();
                        return;
                    case ConfirmationResult.Secondary:
                        // Don't save, revert and close
                        RevertChanges();
                        IsOpen = false;
                        return;
                    case ConfirmationResult.Cancel:
                    case ConfirmationResult.None:
                        // Stay open
                        return;
                }
            }
        }

        // No unsaved changes or dialog not available
        IsOpen = false;
    }

    /// <summary>
    /// Reverts changes to original values.
    /// </summary>
    private void RevertChanges()
    {
        if (SelectedTheme != _originalTheme)
        {
            SelectedTheme = _originalTheme;
            ApplyTheme(_originalTheme);
        }
        if (SelectedAccentColor != _originalAccentColor)
        {
            SelectedAccentColor = _originalAccentColor;
            ApplyAccentColor(_originalAccentColor);
        }
        if (SelectedLanguage != _originalLanguage)
        {
            SetLanguageWithoutNotify(_originalLanguage);
        }
        if (SelectedDateFormat != _originalDateFormat)
        {
            SelectedDateFormat = _originalDateFormat;
        }
        if (SelectedTimeZone.Id != _originalTimeZone.Id)
        {
            SelectedTimeZone = _originalTimeZone;
        }
        if (SelectedTimeFormat != _originalTimeFormat)
        {
            SelectedTimeFormat = _originalTimeFormat;
        }
        if (MaxPieSlices != _originalMaxPieSlices)
        {
            MaxPieSlices = _originalMaxPieSlices;
        }
        LowStockAlert = _originalLowStockAlert;
        OutOfStockAlert = _originalOutOfStockAlert;
        InvoiceOverdue = _originalInvoiceOverdue;
        RentalOverdue = _originalRentalOverdue;
        UnsavedChangesReminder = _originalUnsavedChangesReminder;
        UnsavedChangesReminderMinutes = _originalUnsavedChangesReminderMinutes;
    }

    /// <summary>
    /// Saves the settings and closes the modal.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        // Check what changed before updating original values
        var languageChanged = SelectedLanguage != _originalLanguage;
        var dateFormatChanged = SelectedDateFormat != _originalDateFormat;
        var timeSettingsChanged = SelectedTimeZone.Id != _originalTimeZone.Id ||
                                   SelectedTimeFormat != _originalTimeFormat;
        var maxPieSlicesChanged = MaxPieSlices != _originalMaxPieSlices;

        // Save the previous values in case download/fetch fails
        var previousLanguage = _originalLanguage;

        // Update original values to current (so close doesn't revert)
        _originalTheme = SelectedTheme;
        _originalAccentColor = SelectedAccentColor;
        _originalLanguage = SelectedLanguage;
        _originalDateFormat = SelectedDateFormat;
        _originalTimeZone = SelectedTimeZone;
        _originalTimeFormat = SelectedTimeFormat;
        _originalMaxPieSlices = MaxPieSlices;
        _originalLowStockAlert = LowStockAlert;
        _originalOutOfStockAlert = OutOfStockAlert;
        _originalInvoiceOverdue = InvoiceOverdue;
        _originalRentalOverdue = RentalOverdue;
        _originalUnsavedChangesReminder = UnsavedChangesReminder;
        _originalUnsavedChangesReminderMinutes = UnsavedChangesReminderMinutes;

        // Save language, date format and currency to company settings
        var settings = App.CompanyManager?.CompanyData?.Settings;
        if (settings != null)
        {
            settings.Localization.Language = SelectedLanguage;
            settings.Localization.DateFormat = SelectedDateFormat;

            // Save notification settings
            settings.Notifications.LowStockAlert = LowStockAlert;
            settings.Notifications.OutOfStockAlert = OutOfStockAlert;
            settings.Notifications.InvoiceOverdueAlert = InvoiceOverdue;
            settings.Notifications.RentalOverdueAlert = RentalOverdue;
            settings.Notifications.UnsavedChangesReminder = UnsavedChangesReminder;
            settings.Notifications.UnsavedChangesReminderMinutes = UnsavedChangesReminderMinutes;

            // Save payment portal settings
            SavePortalSettings();

            // Restart the timer with new settings
            App.HeaderViewModel?.RestartUnsavedChangesReminderTimer();
        }

        // Save max pie slices, language, timezone and time format to global settings
        var globalSettings = App.SettingsService?.GlobalSettings;
        if (globalSettings != null)
        {
            globalSettings.Ui.Chart.MaxPieSlices = MaxPieSlices;
            globalSettings.Ui.Language = SelectedLanguage;
            globalSettings.Ui.TimeZone = SelectedTimeZone.Id;
            globalSettings.Ui.TimeFormat = SelectedTimeFormat;
            await App.SettingsService!.SaveGlobalSettingsAsync();
        }

        // Notify that date format changed so views can refresh
        if (dateFormatChanged)
        {
            DateFormatService.NotifyDateFormatChanged();
        }

        // Notify that timezone or time format changed so views can refresh
        if (timeSettingsChanged)
        {
            TimeZoneService.NotifyTimeSettingsChanged();
        }

        // Notify that chart settings changed so charts can reload
        if (maxPieSlicesChanged)
        {
            ChartSettingsService.NotifyMaxPieSlicesChanged();
        }

        // Apply language change via LanguageService
        if (languageChanged)
        {
            IsDownloadingLanguage = true;
            try
            {
                var success = await LanguageService.Instance.SetLanguageAsync(SelectedLanguage);
                if (success)
                {
                    // Notify that language was saved successfully
                    LanguageSettingsChanged?.Invoke(this, new LanguageSettingsChangedEventArgs(SelectedLanguage, true));
                }
                else
                {
                    // Download failed - revert to previous language
                    SetLanguageWithoutNotify(previousLanguage);
                    _originalLanguage = previousLanguage;

                    // Revert in company settings
                    if (settings != null)
                    {
                        settings.Localization.Language = previousLanguage;
                    }

                    // Revert in global settings and save
                    if (globalSettings != null)
                    {
                        globalSettings.Ui.Language = previousLanguage;
                        await App.SettingsService!.SaveGlobalSettingsAsync();
                    }

                    // Show error message
                    var dialog = App.ConfirmationDialog;
                    if (dialog != null)
                    {
                        await dialog.ShowAsync(new ConfirmationDialogOptions
                        {
                            Title = "Language Download Failed".Translate(),
                            Message = "Could not download the language file from the server. Please check your internet connection and try again.".Translate(),
                            PrimaryButtonText = "OK".Translate(),
                            SecondaryButtonText = null,
                            CancelButtonText = null
                        });
                    }
                }
            }
            finally
            {
                IsDownloadingLanguage = false;
            }
        }

        IsOpen = false;
    }

    /// <summary>
    /// Opens the add password modal.
    /// </summary>
    [RelayCommand]
    private void OpenAddPassword()
    {
        ClearPasswordFields();
        IsAddPasswordModalOpen = true;
    }

    /// <summary>
    /// Opens the change password modal.
    /// </summary>
    [RelayCommand]
    private void OpenChangePassword()
    {
        ClearPasswordFields();
        IsChangePasswordModalOpen = true;
    }

    /// <summary>
    /// Opens the remove password modal.
    /// </summary>
    [RelayCommand]
    private void OpenRemovePassword()
    {
        ClearPasswordFields();
        IsRemovePasswordModalOpen = true;
    }

    /// <summary>
    /// Returns whether any password fields have been filled in.
    /// </summary>
    private bool HasPasswordModalInput =>
        !string.IsNullOrEmpty(CurrentPassword) ||
        !string.IsNullOrEmpty(NewPassword) ||
        !string.IsNullOrEmpty(ConfirmPassword);

    /// <summary>
    /// Closes all password modals, prompting to confirm if there is input.
    /// </summary>
    [RelayCommand]
    private async Task ClosePasswordModalAsync()
    {
        if ((IsAddPasswordModalOpen || IsChangePasswordModalOpen || IsRemovePasswordModalOpen) && HasPasswordModalInput)
        {
            if (!await ConfirmDiscardNewAsync()) return;
        }

        ClosePasswordModalInternal();
    }

    private void ClosePasswordModalInternal()
    {
        IsAddPasswordModalOpen = false;
        IsChangePasswordModalOpen = false;
        IsRemovePasswordModalOpen = false;
        ClearPasswordFields();
    }

    /// <summary>
    /// Confirms adding a new password.
    /// </summary>
    [RelayCommand]
    private void ConfirmAddPassword()
    {
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            PasswordError = "Password is required";
            return;
        }
        if (NewPassword.Length < 6)
        {
            PasswordError = "Password must be at least 6 characters";
            return;
        }
        if (NewPassword != ConfirmPassword)
        {
            PasswordError = "Passwords do not match";
            return;
        }

        // Raise event to add password
        AddPasswordRequested?.Invoke(this, new PasswordChangeEventArgs(NewPassword));
        HasPassword = true;
        ClosePasswordModalInternal();
    }

    /// <summary>
    /// Confirms changing the password.
    /// </summary>
    [RelayCommand]
    private void ConfirmChangePassword()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            PasswordError = "Current password is required";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            PasswordError = "New password is required";
            return;
        }
        if (NewPassword.Length < 6)
        {
            PasswordError = "New password must be at least 6 characters";
            return;
        }
        if (NewPassword != ConfirmPassword)
        {
            PasswordError = "Passwords do not match";
            return;
        }

        // Raise event to change password - handler will verify and call back
        ChangePasswordRequested?.Invoke(this, new PasswordChangeEventArgs(NewPassword, CurrentPassword));
        // Note: Don't close immediately - handler will call OnPasswordChanged or OnPasswordVerificationFailed
    }

    /// <summary>
    /// Called when password change succeeds.
    /// </summary>
    public void OnPasswordChanged()
    {
        ClosePasswordModalInternal();
    }

    /// <summary>
    /// Confirms removing the password.
    /// </summary>
    [RelayCommand]
    private void ConfirmRemovePassword()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            PasswordError = "Current password is required";
            return;
        }

        // Raise event to remove password - handler will verify and call back
        RemovePasswordRequested?.Invoke(this, new PasswordChangeEventArgs(null, CurrentPassword));
        // Note: Don't close immediately - handler will call OnPasswordRemoved or OnPasswordError
    }

    /// <summary>
    /// Called when password removal succeeds.
    /// </summary>
    public void OnPasswordRemoved()
    {
        HasPassword = false;
        ClosePasswordModalInternal();
    }

    /// <summary>
    /// Called when password verification fails during removal.
    /// </summary>
    public void OnPasswordVerificationFailed()
    {
        PasswordError = "Incorrect password";
        CurrentPassword = string.Empty;

        // Request focus on the current password textbox
        FocusPasswordRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ClearPasswordFields()
    {
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        CurrentPassword = string.Empty;
        PasswordError = null;
        IsNewPasswordVisible = false;
        IsConfirmPasswordVisible = false;
        IsCurrentPasswordVisible = false;
    }

    /// <summary>
    /// Toggles new password visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleNewPasswordVisibility()
    {
        IsNewPasswordVisible = !IsNewPasswordVisible;
    }

    /// <summary>
    /// Toggles confirm password visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
    }

    /// <summary>
    /// Toggles current password visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleCurrentPasswordVisibility()
    {
        IsCurrentPasswordVisible = !IsCurrentPasswordVisible;
    }

    /// <summary>
    /// Opens the telemetry data folder in the system file explorer.
    /// </summary>
    [RelayCommand]
    private async Task OpenTelemetryFolderAsync()
    {
        try
        {
            // Use Roaming AppData on Windows to match TelemetryStorageService
            var telemetryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ArgoBooks",
                "telemetry");

            if (!Directory.Exists(telemetryPath))
            {
                Directory.CreateDirectory(telemetryPath);
            }

            // Open folder using platform-specific method
            if (OperatingSystem.IsWindows())
            {
                Process.Start("explorer.exe", telemetryPath);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", telemetryPath);
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", telemetryPath);
            }
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.FileSystem, "Failed to open telemetry folder");
            await ShowErrorDialogAsync("Error".Translate(), "Failed to open folder: {0}".TranslateFormat(ex.Message));
        }
    }

    /// <summary>
    /// Deletes all collected telemetry data.
    /// </summary>
    [RelayCommand]
    private async Task DeleteTelemetryDataAsync()
    {
        if (App.TelemetryManager == null) return;

        // Confirm deletion
        var dialog = App.ConfirmationDialog;
        if (dialog != null)
        {
            var result = await dialog.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Delete Telemetry Data".Translate(),
                Message = "Are you sure you want to delete all collected telemetry data? This action cannot be undone.".Translate(),
                PrimaryButtonText = "Delete".Translate(),
                SecondaryButtonText = null,
                CancelButtonText = "Cancel".Translate()
            });

            if (result != ConfirmationResult.Primary)
                return;
        }

        IsDeletingTelemetry = true;
        try
        {
            await App.TelemetryManager.ClearAllDataAsync();
            TelemetryEventCount = 0;
            TelemetryPendingCount = 0;
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.FileSystem, "Failed to delete telemetry data");
            await ShowErrorDialogAsync("Error".Translate(), "Failed to delete telemetry data: {0}".TranslateFormat(ex.Message));
        }
        finally
        {
            IsDeletingTelemetry = false;
        }
    }

    /// <summary>
    /// Refreshes the telemetry statistics.
    /// </summary>
    private async Task RefreshTelemetryStatsAsync()
    {
        if (App.TelemetryManager == null) return;

        try
        {
            var stats = await App.TelemetryManager.GetStatisticsAsync();
            TelemetryEventCount = stats.TotalEvents;
            TelemetryPendingCount = stats.PendingEvents;
        }
        catch
        {
            // Ignore errors loading stats
        }
    }

    /// <summary>
    /// Closes the settings modal and opens the upgrade modal.
    /// </summary>
    [RelayCommand]
    private void UpgradeNow()
    {
        IsOpen = false;
        UpgradeRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Selects a theme.
    /// </summary>
    [RelayCommand]
    private void SelectTheme(string? theme)
    {
        if (!string.IsNullOrEmpty(theme))
        {
            SelectedTheme = theme;
            ApplyTheme(theme);
        }
    }

    /// <summary>
    /// Selects an accent color.
    /// </summary>
    [RelayCommand]
    private void SelectAccentColor(string? colorName)
    {
        if (!string.IsNullOrEmpty(colorName))
        {
            // Apply the accent color FIRST so PrimaryBrush is updated
            // before the binding triggers the MultiValueConverter
            ApplyAccentColor(colorName);
            SelectedAccentColor = colorName;
        }
    }

    private void ApplyTheme(string theme)
    {
        // Theme application will be handled by the ThemeService
        ThemeService.Instance.SetTheme(theme);
    }

    private void ApplyAccentColor(string colorName)
    {
        // Apply accent color via ThemeService
        ThemeService.Instance.SetAccentColor(colorName);
    }

    #endregion
}

/// <summary>
/// Represents an accent color option.
/// </summary>
public class AccentColorItem(string name, string colorHex)
{
    public string Name { get; } = name;
    public string ColorHex { get; } = colorHex;
}

/// <summary>
/// Event args for password change operations.
/// </summary>
public class PasswordChangeEventArgs(string? newPassword, string? currentPassword = null) : EventArgs
{
    /// <summary>
    /// The new password (null to remove password).
    /// </summary>
    public string? NewPassword { get; } = newPassword;

    /// <summary>
    /// The current password (for verification when changing/removing).
    /// </summary>
    public string? CurrentPassword { get; } = currentPassword;
}

/// <summary>
/// Event args for auto-lock settings change.
/// </summary>
public class AutoLockSettingsEventArgs(string timeoutString) : EventArgs
{
    /// <summary>
    /// The selected auto-lock timeout string (e.g., "5 minutes", "Never").
    /// </summary>
    public string TimeoutString { get; } = timeoutString;

    /// <summary>
    /// The timeout in minutes (0 for "Never").
    /// </summary>
    public int TimeoutMinutes { get; } = ParseTimeoutMinutes(timeoutString);

    private static int ParseTimeoutMinutes(string? timeoutString)
    {
        if (string.IsNullOrEmpty(timeoutString) || timeoutString == "Never")
            return 0;

        if (timeoutString.Contains("hour"))
            return 60;

        var parts = timeoutString.Split(' ');
        if (parts.Length >= 1 && int.TryParse(parts[0], out var minutes))
            return minutes;

        return 0;
    }
}

/// <summary>
/// Event args for Windows Hello setting changes.
/// </summary>
public class WindowsHelloEventArgs(bool enabled) : EventArgs
{
    /// <summary>
    /// Whether Windows Hello is enabled.
    /// </summary>
    public bool Enabled { get; } = enabled;
}

/// <summary>
/// Event args for language settings changes.
/// </summary>
public class LanguageSettingsChangedEventArgs(string language, bool applied) : EventArgs
{
    /// <summary>
    /// The selected language name (e.g., "French", "German").
    /// </summary>
    public string Language { get; } = language;

    /// <summary>
    /// Whether the language change has been applied (translations downloaded and active).
    /// </summary>
    public bool Applied { get; } = applied;
}
