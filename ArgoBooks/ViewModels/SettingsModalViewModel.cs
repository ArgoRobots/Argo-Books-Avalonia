using System.Collections.ObjectModel;
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
    private string _originalTheme = "Dark";
    private string _originalAccentColor = "Blue";
    private string _originalDateFormat = "MM/DD/YYYY";
    private int _originalMaxPieSlices = 6;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private int _selectedTabIndex;

    #region General Settings

    [ObservableProperty]
    private string _selectedLanguage = "English";

    [ObservableProperty]
    private string _selectedCurrency = "USD - US Dollar ($)";

    [ObservableProperty]
    private string _selectedDateFormat = "MM/DD/YYYY";

    [ObservableProperty]
    private string _selectedTimeZone = "(UTC-05:00) Eastern Time";

    [ObservableProperty]
    private bool _anonymousDataCollection;

    [ObservableProperty]
    private int _maxPieSlices = 6;

    /// <summary>
    /// Available options for max pie slices.
    /// </summary>
    public int[] MaxPieSlicesOptions { get; } = [4, 5, 6, 7, 8, 10, 12];

    public ObservableCollection<string> Languages { get; } = new()
    {
        "Albanian",
        "Arabic",
        "Basque",
        "Belarusian",
        "Bengali",
        "Bosnian",
        "Bulgarian",
        "Catalan",
        "Chinese (Simplified)",
        "Chinese (Traditional)",
        "Croatian",
        "Czech",
        "Danish",
        "Dutch",
        "English",
        "Estonian",
        "Filipino",
        "Finnish",
        "French",
        "Galician",
        "German",
        "Greek",
        "Hebrew",
        "Hindi",
        "Hungarian",
        "Icelandic",
        "Indonesian",
        "Irish",
        "Italian",
        "Japanese",
        "Korean",
        "Latvian",
        "Lithuanian",
        "Luxembourgish",
        "Macedonian",
        "Malay",
        "Maltese",
        "Norwegian",
        "Persian",
        "Polish",
        "Portuguese",
        "Romanian",
        "Russian",
        "Serbian",
        "Slovak",
        "Slovenian",
        "Spanish",
        "Swahili",
        "Swedish",
        "Thai",
        "Turkish",
        "Ukrainian",
        "Urdu",
        "Vietnamese"
    };

    public ObservableCollection<string> Currencies { get; } = new()
    {
        "ALL - Albanian Lek (L)",
        "AUD - Australian Dollar ($)",
        "BAM - Bosnia-Herzegovina Mark (KM)",
        "BGN - Bulgarian Lev (лв)",
        "BRL - Brazilian Real (R$)",
        "BYN - Belarusian Ruble (Br)",
        "CAD - Canadian Dollar ($)",
        "CHF - Swiss Franc (CHF)",
        "CNY - Chinese Yuan (¥)",
        "CZK - Czech Koruna (Kč)",
        "DKK - Danish Krone (kr)",
        "EUR - Euro (€)",
        "GBP - British Pound (£)",
        "HUF - Hungarian Forint (Ft)",
        "ISK - Icelandic Króna (kr)",
        "JPY - Japanese Yen (¥)",
        "KRW - South Korean Won (₩)",
        "MKD - Macedonian Denar (ден)",
        "NOK - Norwegian Krone (kr)",
        "PLN - Polish Złoty (zł)",
        "RON - Romanian Leu (lei)",
        "RSD - Serbian Dinar (дин)",
        "RUB - Russian Ruble (₽)",
        "SEK - Swedish Krona (kr)",
        "TRY - Turkish Lira (₺)",
        "TWD - Taiwan Dollar (NT$)",
        "UAH - Ukrainian Hryvnia (₴)",
        "USD - US Dollar ($)"
    };

    public ObservableCollection<string> DateFormats { get; } = new()
    {
        "MM/DD/YYYY",
        "DD/MM/YYYY",
        "YYYY-MM-DD",
        "MMM D, YYYY"
    };

    public ObservableCollection<string> TimeZones { get; } = new()
    {
        "(UTC-08:00) Pacific Time",
        "(UTC-07:00) Mountain Time",
        "(UTC-06:00) Central Time",
        "(UTC-05:00) Eastern Time",
        "(UTC+00:00) UTC",
        "(UTC+01:00) Central European Time",
        "(UTC+08:00) China Standard Time",
        "(UTC+09:00) Japan Standard Time"
    };

    #endregion

    #region Notification Settings

    [ObservableProperty]
    private bool _lowStockAlert = true;

    [ObservableProperty]
    private bool _invoiceOverdue = true;

    [ObservableProperty]
    private bool _paymentReceived = true;

    [ObservableProperty]
    private bool _largeTransactionAlert = true;

    #endregion

    #region Appearance Settings

    [ObservableProperty]
    private string _selectedTheme = "Dark";

    [ObservableProperty]
    private string _selectedAccentColor = "Blue";

    public ObservableCollection<string> Themes { get; } = new()
    {
        "Light",
        "Dark",
        "System"
    };

    public ObservableCollection<AccentColorItem> AccentColors { get; } = new()
    {
        new AccentColorItem("Blue", "#3B82F6"),
        new AccentColorItem("Green", "#10B981"),
        new AccentColorItem("Purple", "#8B5CF6"),
        new AccentColorItem("Pink", "#EC4899"),
        new AccentColorItem("Orange", "#F97316"),
        new AccentColorItem("Teal", "#14B8A6")
    };

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
    /// Called when HasPassword changes - sync with FileEncryptionEnabled.
    /// </summary>
    partial void OnHasPasswordChanged(bool value)
    {
        _isUpdatingEncryption = true;
        FileEncryptionEnabled = value;
        _isUpdatingEncryption = false;
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

    public ObservableCollection<string> AutoLockOptions { get; } = new()
    {
        "Never",
        "5 minutes",
        "15 minutes",
        "30 minutes",
        "1 hour"
    };

    #endregion

    /// <summary>
    /// Whether there are unsaved changes in the settings.
    /// </summary>
    public bool HasUnsavedChanges =>
        SelectedTheme != _originalTheme ||
        SelectedAccentColor != _originalAccentColor ||
        SelectedDateFormat != _originalDateFormat ||
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

        // Load date format from company settings
        var settings = App.CompanyManager?.CompanyData?.Settings;
        if (settings != null)
        {
            SelectedDateFormat = settings.Localization.DateFormat;
        }

        // Load max pie slices from global settings
        var globalSettings = App.SettingsService?.GlobalSettings;
        if (globalSettings != null)
        {
            MaxPieSlices = globalSettings.Ui.Chart.MaxPieSlices;
        }

        // Store original values for potential revert
        _originalTheme = SelectedTheme;
        _originalAccentColor = SelectedAccentColor;
        _originalDateFormat = SelectedDateFormat;
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
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Unsaved Changes",
                    Message = "You have unsaved changes. Do you want to save them before closing?",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Don't Save",
                    CancelButtonText = "Cancel"
                });

                switch (result)
                {
                    case ConfirmationResult.Primary:
                        // Save and close
                        SaveCommand.Execute(null);
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
        if (SelectedDateFormat != _originalDateFormat)
        {
            SelectedDateFormat = _originalDateFormat;
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
    private void Save()
    {
        // Check if date format changed before updating original values
        var dateFormatChanged = SelectedDateFormat != _originalDateFormat;
        var maxPieSlicesChanged = MaxPieSlices != _originalMaxPieSlices;

        // Update original values to current (so close doesn't revert)
        _originalTheme = SelectedTheme;
        _originalAccentColor = SelectedAccentColor;
        _originalDateFormat = SelectedDateFormat;
        _originalMaxPieSlices = MaxPieSlices;

        // Save date format to company settings
        var settings = App.CompanyManager?.CompanyData?.Settings;
        if (settings != null)
        {
            settings.Localization.DateFormat = SelectedDateFormat;
            settings.ChangesMade = true;
        }

        // Save max pie slices to global settings
        var globalSettings = App.SettingsService?.GlobalSettings;
        if (globalSettings != null)
        {
            globalSettings.Ui.Chart.MaxPieSlices = MaxPieSlices;
            _ = App.SettingsService?.SaveGlobalSettingsAsync();
        }

        // Notify that date format changed so views can refresh
        if (dateFormatChanged)
        {
            DateFormatService.NotifyDateFormatChanged();
        }

        // Notify that chart settings changed so charts can reload
        if (maxPieSlicesChanged)
        {
            ChartSettingsService.NotifyMaxPieSlicesChanged();
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
            SelectedAccentColor = colorName;
            ApplyAccentColor(colorName);
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
public class AccentColorItem
{
    public string Name { get; }
    public string ColorHex { get; }

    public AccentColorItem(string name, string colorHex)
    {
        Name = name;
        ColorHex = colorHex;
    }
}

/// <summary>
/// Event args for password change operations.
/// </summary>
public class PasswordChangeEventArgs : EventArgs
{
    /// <summary>
    /// The new password (null to remove password).
    /// </summary>
    public string? NewPassword { get; }

    /// <summary>
    /// The current password (for verification when changing/removing).
    /// </summary>
    public string? CurrentPassword { get; }

    public PasswordChangeEventArgs(string? newPassword, string? currentPassword = null)
    {
        NewPassword = newPassword;
        CurrentPassword = currentPassword;
    }
}

/// <summary>
/// Event args for auto-lock settings change.
/// </summary>
public class AutoLockSettingsEventArgs : EventArgs
{
    /// <summary>
    /// The selected auto-lock timeout string (e.g., "5 minutes", "Never").
    /// </summary>
    public string TimeoutString { get; }

    /// <summary>
    /// The timeout in minutes (0 for "Never").
    /// </summary>
    public int TimeoutMinutes { get; }

    public AutoLockSettingsEventArgs(string timeoutString)
    {
        TimeoutString = timeoutString;
        TimeoutMinutes = ParseTimeoutMinutes(timeoutString);
    }

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
