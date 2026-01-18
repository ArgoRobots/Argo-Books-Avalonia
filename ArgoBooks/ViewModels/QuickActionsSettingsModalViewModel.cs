using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the quick actions settings modal.
/// Allows users to configure which quick actions are visible on the dashboard.
/// </summary>
public partial class QuickActionsSettingsModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    #region Quick Action Visibility

    [ObservableProperty]
    private bool _showNewExpense = true;

    [ObservableProperty]
    private bool _showNewSale = true;

    [ObservableProperty]
    private bool _showCreateInvoice = true;

    [ObservableProperty]
    private bool _showNewRental = true;

    #endregion

    /// <summary>
    /// Event raised when settings are saved.
    /// </summary>
    public event EventHandler? SettingsSaved;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public QuickActionsSettingsModalViewModel()
    {
        LoadSettings();
    }

    #region Commands

    /// <summary>
    /// Opens the quick actions settings modal.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        LoadSettings();
        IsOpen = true;
    }

    /// <summary>
    /// Closes the quick actions settings modal without saving.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Saves the quick actions settings and closes the modal.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        await SaveSettingsAsync();
        SettingsSaved?.Invoke(this, EventArgs.Empty);
        IsOpen = false;
    }

    #endregion

    #region Settings Persistence

    private void LoadSettings()
    {
        var globalSettings = App.SettingsService?.GlobalSettings;
        if (globalSettings != null)
        {
            ShowNewExpense = globalSettings.Ui.QuickActions.ShowNewExpense;
            ShowNewSale = globalSettings.Ui.QuickActions.ShowNewSale;
            ShowCreateInvoice = globalSettings.Ui.QuickActions.ShowCreateInvoice;
            ShowNewRental = globalSettings.Ui.QuickActions.ShowNewRental;
        }
    }

    private async Task SaveSettingsAsync()
    {
        var globalSettings = App.SettingsService?.GlobalSettings;
        if (globalSettings != null)
        {
            globalSettings.Ui.QuickActions.ShowNewExpense = ShowNewExpense;
            globalSettings.Ui.QuickActions.ShowNewSale = ShowNewSale;
            globalSettings.Ui.QuickActions.ShowCreateInvoice = ShowCreateInvoice;
            globalSettings.Ui.QuickActions.ShowNewRental = ShowNewRental;

            await App.SettingsService!.SaveGlobalSettingsAsync();
        }
    }

    #endregion
}
