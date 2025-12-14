using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Settings modal.
/// </summary>
public partial class SettingsModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private int _selectedTabIndex;

    #region General Settings

    [ObservableProperty]
    private string _selectedLanguage = "English";

    [ObservableProperty]
    private string _selectedCurrency = "USD - US Dollar";

    [ObservableProperty]
    private string _selectedDateFormat = "MM/DD/YYYY";

    [ObservableProperty]
    private string _selectedTimeZone = "(UTC-05:00) Eastern Time";

    [ObservableProperty]
    private bool _anonymousDataCollection;

    public ObservableCollection<string> Languages { get; } = new()
    {
        "English",
        "French",
        "Spanish",
        "German",
        "Portuguese",
        "Chinese (Simplified)",
        "Japanese"
    };

    public ObservableCollection<string> Currencies { get; } = new()
    {
        "USD - US Dollar",
        "CAD - Canadian Dollar",
        "EUR - Euro",
        "GBP - British Pound",
        "AUD - Australian Dollar",
        "JPY - Japanese Yen",
        "CNY - Chinese Yuan"
    };

    public ObservableCollection<string> DateFormats { get; } = new()
    {
        "MM/DD/YYYY",
        "DD/MM/YYYY",
        "YYYY-MM-DD"
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

    #region Feature Toggles

    [ObservableProperty]
    private bool _invoicesEnabled = true;

    [ObservableProperty]
    private bool _paymentsEnabled = true;

    [ObservableProperty]
    private bool _inventoryEnabled = true;

    [ObservableProperty]
    private bool _employeesEnabled = true;

    [ObservableProperty]
    private bool _rentalsEnabled = true;

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
    private bool _windowsHelloEnabled;

    [ObservableProperty]
    private bool _fileEncryptionEnabled;

    [ObservableProperty]
    private string _selectedAutoLock = "5 minutes";

    public ObservableCollection<string> AutoLockOptions { get; } = new()
    {
        "Never",
        "1 minute",
        "5 minutes",
        "15 minutes",
        "30 minutes",
        "1 hour"
    };

    #endregion

    /// <summary>
    /// Default constructor.
    /// </summary>
    public SettingsModalViewModel()
    {
    }

    #region Commands

    /// <summary>
    /// Opens the settings modal.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        SelectedTabIndex = 0;
        IsOpen = true;
    }

    /// <summary>
    /// Closes the settings modal.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Saves the settings and closes the modal.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        // TODO: Persist settings
        Close();
    }

    /// <summary>
    /// Opens the change password dialog.
    /// </summary>
    [RelayCommand]
    private void ChangePassword()
    {
        // TODO: Show change password modal
    }

    /// <summary>
    /// Selects an accent color.
    /// </summary>
    [RelayCommand]
    private void SelectAccentColor(AccentColorItem? color)
    {
        if (color != null)
        {
            SelectedAccentColor = color.Name;
        }
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
