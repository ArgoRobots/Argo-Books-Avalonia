using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Services;
using ArgoBooks.Data;
using ArgoBooks.Localization;
using ArgoBooks.Services;
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
    private string _originalCurrency = "USD - US Dollar ($)";
    private TimeZoneItem _originalTimeZone = TimeZones.FindById("UTC");
    private string _originalTimeFormat = "12h";
    private int _originalMaxPieSlices = 6;

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
    private string _selectedCurrency = "USD - US Dollar ($)";

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

    /// <summary>
    /// Event raised when telemetry data should be exported.
    /// </summary>
    public event EventHandler<string>? TelemetryDataExported;

    [ObservableProperty]
    private int _maxPieSlices = 6;

    // Currency change error state
    [ObservableProperty]
    private bool _hasCurrencyError;

    [ObservableProperty]
    private string _currencyErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _isSavingCurrency;

    [RelayCommand]
    private void DismissCurrencyError()
    {
        HasCurrencyError = false;
        CurrencyErrorMessage = string.Empty;
    }

    [RelayCommand]
    private async Task RetryCurrencySaveAsync()
    {
        HasCurrencyError = false;
        CurrencyErrorMessage = string.Empty;
        await SaveAsync();
    }

    /// <summary>
    /// Available options for max pie slices.
    /// </summary>
    public int[] MaxPieSlicesOptions { get; } = [4, 5, 6, 7, 8, 10, 12];

    /// <summary>
    /// Priority/common languages shown at the top of the dropdown.
    /// </summary>
    public IReadOnlyList<string> PriorityLanguages => Data.Languages.Priority;

    /// <summary>
    /// Priority/common currencies shown at the top of the dropdown.
    /// </summary>
    public IReadOnlyList<string> PriorityCurrencies => Data.Currencies.Priority;

    /// <summary>
    /// All available languages.
    /// </summary>
    public IReadOnlyList<string> Languages => Data.Languages.All;

    /// <summary>
    /// All available currencies.
    /// </summary>
    public IReadOnlyList<string> Currencies => Data.Currencies.All;

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

    public ObservableCollection<string> Themes { get; } =
    [
        "Light",
        "Dark",
        "System"
    ];

    public ObservableCollection<AccentColorItem> AccentColors { get; } =
    [
        new("Blue", "#3B82F6"),
        new("Green", "#10B981"),
        new("Purple", "#8B5CF6"),
        new("Pink", "#EC4899"),
        new("Orange", "#F97316"),
        new("Teal", "#14B8A6")
    ];

    #endregion

    #region Security Settings

    [ObservableProperty]
    private bool _hasStandard; // Whether user has Standard plan or higher

    [ObservableProperty]
    private bool _windowsHelloEnabled;

    [ObservableProperty]
    private bool _fileEncryptionEnabled;

    [ObservableProperty]
    private string _selectedAutoLock = "5 minutes";

    [ObservableProperty]
    private bool _hasPassword;

    /// <summary>
    /// Whether Windows Hello can be enabled (requires Standard plan AND password).
    /// </summary>
    public bool CanEnableWindowsHello => HasStandard && HasPassword;

    /// <summary>
    /// Whether the user needs to set a password before enabling Windows Hello.
    /// Shows when user has Standard plan but no password.
    /// </summary>
    public bool NeedsPasswordForWindowsHello => HasStandard && !HasPassword;

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

    // Flag to prevent recursive updates when syncing FileEncryptionEnabled with HasPassword
    private bool _isUpdatingEncryption;

    // Flag to prevent firing AutoLockSettingsChanged when syncing UI with company settings
    private bool _isLoadingAutoLock;

    /// <summary>
    /// Called when FileEncryptionEnabled changes - opens appropriate password modal.
    /// </summary>
    partial void OnFileEncryptionEnabledChanged(bool value)
    {
        if (_isUpdatingEncryption) return;

        if (value && !HasPassword)
        {
            // User wants to enable encryption but no password set - open Add Password modal
            OpenAddPasswordCommand.Execute(null);
        }
        else if (!value && HasPassword)
        {
            // User wants to disable encryption but has password - open Remove Password modal
            OpenRemovePasswordCommand.Execute(null);
        }
    }

    /// <summary>
    /// Called when HasPassword changes - sync with FileEncryptionEnabled and notify Windows Hello properties.
    /// </summary>
    partial void OnHasPasswordChanged(bool value)
    {
        _isUpdatingEncryption = true;
        FileEncryptionEnabled = value;
        _isUpdatingEncryption = false;

        // Notify Windows Hello computed properties
        OnPropertyChanged(nameof(CanEnableWindowsHello));
        OnPropertyChanged(nameof(NeedsPasswordForWindowsHello));

        // Disable Windows Hello if password is removed
        if (!value && WindowsHelloEnabled)
        {
            WindowsHelloEnabled = false;
        }
    }

    /// <summary>
    /// Called when HasStandard changes - notify Windows Hello properties.
    /// </summary>
    partial void OnHasStandardChanged(bool value)
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

    /// <summary>
    /// Whether there are unsaved changes in the settings.
    /// </summary>
    public bool HasUnsavedChanges =>
        SelectedTheme != _originalTheme ||
        SelectedAccentColor != _originalAccentColor ||
        SelectedLanguage != _originalLanguage ||
        SelectedDateFormat != _originalDateFormat ||
        SelectedCurrency != _originalCurrency ||
        SelectedTimeZone.Id != _originalTimeZone.Id ||
        SelectedTimeFormat != _originalTimeFormat ||
        MaxPieSlices != _originalMaxPieSlices;

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
    /// <param name="tabIndex">The tab index to select (0=General, 1=Notifications, 2=Appearance, 3=Security).</param>
    public void OpenWithTab(int tabIndex)
    {
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
            // Convert currency code to display string
            SelectedCurrency = CurrencyService.GetDisplayString(settings.Localization.Currency);

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

        // Refresh telemetry stats
        _ = RefreshTelemetryStatsAsync();

        // Store original values for potential revert
        _originalTheme = SelectedTheme;
        _originalAccentColor = SelectedAccentColor;
        _originalLanguage = SelectedLanguage;
        _originalDateFormat = SelectedDateFormat;
        _originalCurrency = SelectedCurrency;
        _originalTimeZone = SelectedTimeZone;
        _originalTimeFormat = SelectedTimeFormat;
        _originalMaxPieSlices = MaxPieSlices;
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
        if (SelectedCurrency != _originalCurrency)
        {
            SelectedCurrency = _originalCurrency;
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
        var currencyChanged = SelectedCurrency != _originalCurrency;
        var timeSettingsChanged = SelectedTimeZone.Id != _originalTimeZone.Id ||
                                   SelectedTimeFormat != _originalTimeFormat;
        var maxPieSlicesChanged = MaxPieSlices != _originalMaxPieSlices;

        // Save the previous values in case download/fetch fails
        var previousLanguage = _originalLanguage;
        var previousCurrency = _originalCurrency;

        // Extract the new currency code before updating originals
        var newCurrencyCode = CurrencyService.ParseCurrencyCode(SelectedCurrency);

        // Update original values to current (so close doesn't revert)
        _originalTheme = SelectedTheme;
        _originalAccentColor = SelectedAccentColor;
        _originalLanguage = SelectedLanguage;
        _originalDateFormat = SelectedDateFormat;
        _originalCurrency = SelectedCurrency;
        _originalTimeZone = SelectedTimeZone;
        _originalTimeFormat = SelectedTimeFormat;
        _originalMaxPieSlices = MaxPieSlices;

        // Save language, date format and currency to company settings
        var settings = App.CompanyManager?.CompanyData?.Settings;
        if (settings != null)
        {
            settings.Localization.Language = SelectedLanguage;
            settings.Localization.DateFormat = SelectedDateFormat;
            // Extract currency code from display string (e.g., "USD - US Dollar ($)" -> "USD")
            settings.Localization.Currency = newCurrencyCode;

            // Save notification settings
            settings.Notifications.LowStockAlert = LowStockAlert;
            settings.Notifications.OutOfStockAlert = OutOfStockAlert;
            settings.Notifications.InvoiceOverdueAlert = InvoiceOverdue;
            settings.Notifications.RentalOverdueAlert = RentalOverdue;
            settings.Notifications.UnsavedChangesReminder = UnsavedChangesReminder;
            settings.Notifications.UnsavedChangesReminderMinutes = UnsavedChangesReminderMinutes;

            // Restart the timer with new settings
            App.HeaderViewModel?.RestartUnsavedChangesReminderTimer();

            settings.ChangesMade = true;
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

        // Notify that currency changed so views can refresh
        // Also preload exchange rates for the new currency
        if (currencyChanged)
        {
            IsSavingCurrency = true;
            var success = await PreloadExchangeRatesForCurrencyAsync(newCurrencyCode);
            IsSavingCurrency = false;

            if (!success)
            {
                // Revert currency change
                SelectedCurrency = _originalCurrency;
                _originalCurrency = previousCurrency;

                // Revert in company settings
                if (settings != null)
                {
                    settings.Localization.Currency = CurrencyService.ParseCurrencyCode(previousCurrency);
                }

                // Show error state modal
                return;
            }

            CurrencyService.NotifyCurrencyChanged();
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
    /// Preloads exchange rates for the selected currency.
    /// Returns true if successful, false if exchange rates could not be fetched.
    /// </summary>
    private async Task<bool> PreloadExchangeRatesForCurrencyAsync(string currencyCode)
    {
        // Skip if USD (no conversion needed)
        if (string.Equals(currencyCode, "USD", StringComparison.OrdinalIgnoreCase))
            return true;

        var exchangeService = ExchangeRateService.Instance;
        if (exchangeService == null)
        {
            HasCurrencyError = true;
            CurrencyErrorMessage = "Exchange rate service is not available. Please restart the application.".Translate();
            return false;
        }

        // Check connectivity first
        var connectivityService = new ConnectivityService();
        var hasInternet = await connectivityService.IsInternetAvailableAsync();

        // Try to get exchange rate for today
        var today = DateTime.Today;
        var rate = await exchangeService.GetExchangeRateAsync(currencyCode, "USD", today, fetchIfMissing: true);

        if (rate <= 0)
        {
            // Rate fetch failed
            HasCurrencyError = true;
            CurrencyErrorMessage = hasInternet
                ? "Unable to fetch exchange rates. Please try again.".Translate()
                : "No internet connection. Exchange rates are required for non-USD currencies.".Translate();
            return false;
        }

        // Rate available - try to preload more dates in the background
        try
        {
            var dates = Enumerable.Range(1, 30).Select(i => today.AddDays(-i)).ToList();
            await exchangeService.PreloadRatesAsync(dates);
        }
        catch
        {
            // Preloading additional dates failed, but we have today's rate so it's OK
        }

        return true;
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
    /// Closes all password modals.
    /// </summary>
    [RelayCommand]
    private void ClosePasswordModal()
    {
        // If user was adding password but cancelled, revert the toggle
        if (IsAddPasswordModalOpen && !HasPassword)
        {
            _isUpdatingEncryption = true;
            FileEncryptionEnabled = false;
            _isUpdatingEncryption = false;
        }
        // If user was removing password but cancelled, revert the toggle
        else if (IsRemovePasswordModalOpen && HasPassword)
        {
            _isUpdatingEncryption = true;
            FileEncryptionEnabled = true;
            _isUpdatingEncryption = false;
        }

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
        ClosePasswordModal();
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
        ClosePasswordModal();
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
        ClosePasswordModal();
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
    /// Exports telemetry data as JSON for user review.
    /// </summary>
    [RelayCommand]
    private async Task ExportTelemetryDataAsync()
    {
        if (App.TelemetryManager == null) return;

        IsExportingTelemetry = true;
        try
        {
            var json = await App.TelemetryManager.ExportDataAsJsonAsync();
            TelemetryDataExported?.Invoke(this, json);
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.FileSystem, "Failed to export telemetry data");
            App.AddNotification("Error".Translate(), "Failed to export telemetry data: {0}".TranslateFormat(ex.Message), NotificationType.Error);
        }
        finally
        {
            IsExportingTelemetry = false;
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
            App.AddNotification("Error".Translate(), "Failed to delete telemetry data: {0}".TranslateFormat(ex.Message), NotificationType.Error);
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
