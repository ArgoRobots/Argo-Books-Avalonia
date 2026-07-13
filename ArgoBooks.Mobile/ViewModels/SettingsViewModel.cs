using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Mobile.ViewModels;

/// <summary>
/// Settings tab. Minimal for Task 6 (paired company label + last synced + manual refresh); the
/// multi-company switcher, biometric lock toggle, and unpair flow are Tasks 7/8.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ShellViewModel _shell;

    [ObservableProperty]
    private string _companyLabel = string.Empty;

    [ObservableProperty]
    private string _lastSyncedText = "Not synced yet";

    public SettingsViewModel(ShellViewModel shell)
    {
        _shell = shell;
    }

    public void Update(string companyLabel, string lastSyncedText)
    {
        CompanyLabel = companyLabel;
        LastSyncedText = lastSyncedText;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await _shell.RefreshCommand.ExecuteAsync(null);
}
