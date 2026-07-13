using System.Threading.Tasks;
using ArgoBooks.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Mobile.ViewModels;

/// <summary>
/// Settings tab. Paired company label + last synced + manual refresh (Task 6), plus the
/// biometric app lock toggle (Task 7). The multi-company switcher and unpair flow are Task 8.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ShellViewModel _shell;
    private bool _isLoadingAppLockSetting;

    [ObservableProperty]
    private string _companyLabel = string.Empty;

    [ObservableProperty]
    private string _lastSyncedText = "Not synced yet";

    /// <summary>Biometric app lock on/off. Defaults to on; free for all users (no premium gate).</summary>
    [ObservableProperty]
    private bool _isAppLockEnabled = true;

    public SettingsViewModel(ShellViewModel shell)
    {
        _shell = shell;
        _ = LoadAppLockSettingAsync();
    }

    public void Update(string companyLabel, string lastSyncedText)
    {
        CompanyLabel = companyLabel;
        LastSyncedText = lastSyncedText;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await _shell.RefreshCommand.ExecuteAsync(null);

    private async Task LoadAppLockSettingAsync()
    {
        _isLoadingAppLockSetting = true;
        try
        {
            IsAppLockEnabled = await AppLockSettings.IsEnabledAsync();
        }
        finally
        {
            _isLoadingAppLockSetting = false;
        }
    }

    partial void OnIsAppLockEnabledChanged(bool value)
    {
        // Skip the write-back while LoadAppLockSettingAsync is setting the initial value from
        // storage, so we don't redundantly re-save the value we just loaded.
        if (_isLoadingAppLockSetting)
        {
            return;
        }

        _ = AppLockSettings.SetEnabledAsync(value);
    }
}
