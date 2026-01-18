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

    #region Quick Action Visibility - Primary Actions

    [ObservableProperty]
    private bool _showNewInvoice = true;

    [ObservableProperty]
    private bool _showNewExpense = true;

    [ObservableProperty]
    private bool _showNewRevenue = true;

    [ObservableProperty]
    private bool _showScanReceipt = true;

    #endregion

    #region Quick Action Visibility - Contact Actions

    [ObservableProperty]
    private bool _showNewCustomer;

    [ObservableProperty]
    private bool _showNewSupplier;

    #endregion

    #region Quick Action Visibility - Product & Inventory Actions

    [ObservableProperty]
    private bool _showNewProduct;

    [ObservableProperty]
    private bool _showRecordPayment;

    #endregion

    #region Quick Action Visibility - Rental Actions

    [ObservableProperty]
    private bool _showNewRentalItem;

    [ObservableProperty]
    private bool _showNewRentalRecord = true;

    #endregion

    #region Quick Action Visibility - Organization Actions

    [ObservableProperty]
    private bool _showNewCategory;

    [ObservableProperty]
    private bool _showNewDepartment;

    [ObservableProperty]
    private bool _showNewLocation;

    #endregion

    #region Quick Action Visibility - Order & Stock Actions

    [ObservableProperty]
    private bool _showNewPurchaseOrder;

    [ObservableProperty]
    private bool _showNewStockAdjustment;

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

    /// <summary>
    /// Resets all quick actions to their default values.
    /// </summary>
    [RelayCommand]
    private void ResetAll()
    {
        // Primary actions (shown by default)
        ShowNewInvoice = true;
        ShowNewExpense = true;
        ShowNewRevenue = true;
        ShowScanReceipt = true;

        // Contact actions (hidden by default)
        ShowNewCustomer = false;
        ShowNewSupplier = false;

        // Product & Inventory actions (hidden by default)
        ShowNewProduct = false;
        ShowRecordPayment = false;

        // Rental actions
        ShowNewRentalItem = false;
        ShowNewRentalRecord = true;

        // Organization actions (hidden by default)
        ShowNewCategory = false;
        ShowNewDepartment = false;
        ShowNewLocation = false;

        // Order & Stock actions (hidden by default)
        ShowNewPurchaseOrder = false;
        ShowNewStockAdjustment = false;
    }

    #endregion

    #region Settings Persistence

    private void LoadSettings()
    {
        var globalSettings = App.SettingsService?.GlobalSettings;
        if (globalSettings != null)
        {
            var qa = globalSettings.Ui.QuickActions;

            // Primary actions
            ShowNewInvoice = qa.ShowNewInvoice;
            ShowNewExpense = qa.ShowNewExpense;
            ShowNewRevenue = qa.ShowNewRevenue;
            ShowScanReceipt = qa.ShowScanReceipt;

            // Contact actions
            ShowNewCustomer = qa.ShowNewCustomer;
            ShowNewSupplier = qa.ShowNewSupplier;

            // Product & Inventory actions
            ShowNewProduct = qa.ShowNewProduct;
            ShowRecordPayment = qa.ShowRecordPayment;

            // Rental actions
            ShowNewRentalItem = qa.ShowNewRentalItem;
            ShowNewRentalRecord = qa.ShowNewRentalRecord;

            // Organization actions
            ShowNewCategory = qa.ShowNewCategory;
            ShowNewDepartment = qa.ShowNewDepartment;
            ShowNewLocation = qa.ShowNewLocation;

            // Order & Stock actions
            ShowNewPurchaseOrder = qa.ShowNewPurchaseOrder;
            ShowNewStockAdjustment = qa.ShowNewStockAdjustment;
        }
    }

    private async Task SaveSettingsAsync()
    {
        var globalSettings = App.SettingsService?.GlobalSettings;
        if (globalSettings != null)
        {
            var qa = globalSettings.Ui.QuickActions;

            // Primary actions
            qa.ShowNewInvoice = ShowNewInvoice;
            qa.ShowNewExpense = ShowNewExpense;
            qa.ShowNewRevenue = ShowNewRevenue;
            qa.ShowScanReceipt = ShowScanReceipt;

            // Contact actions
            qa.ShowNewCustomer = ShowNewCustomer;
            qa.ShowNewSupplier = ShowNewSupplier;

            // Product & Inventory actions
            qa.ShowNewProduct = ShowNewProduct;
            qa.ShowRecordPayment = ShowRecordPayment;

            // Rental actions
            qa.ShowNewRentalItem = ShowNewRentalItem;
            qa.ShowNewRentalRecord = ShowNewRentalRecord;

            // Organization actions
            qa.ShowNewCategory = ShowNewCategory;
            qa.ShowNewDepartment = ShowNewDepartment;
            qa.ShowNewLocation = ShowNewLocation;

            // Order & Stock actions
            qa.ShowNewPurchaseOrder = ShowNewPurchaseOrder;
            qa.ShowNewStockAdjustment = ShowNewStockAdjustment;

            await App.SettingsService!.SaveGlobalSettingsAsync();
        }
    }

    #endregion
}
