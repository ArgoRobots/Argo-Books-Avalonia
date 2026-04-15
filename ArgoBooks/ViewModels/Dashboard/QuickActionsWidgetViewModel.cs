using ArgoBooks.Core.Models.Dashboard;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels.Dashboard;

public partial class QuickActionsWidgetViewModel : WidgetViewModelBase
{
    public override WidgetType WidgetType => WidgetType.QuickActions;
    public override bool HasConfig => true;

    #region Quick Actions Visibility

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewInvoice = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewExpense = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewRevenue = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showScanReceipt = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewCustomer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewSupplier;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewProduct;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showRecordPayment;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewRentalItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewRentalRecord = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewDepartment;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewLocation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewPurchaseOrder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVisibleQuickActions))]
    [NotifyPropertyChangedFor(nameof(HasNoVisibleQuickActions))]
    private bool _showNewStockAdjustment;

    public bool HasVisibleQuickActions =>
        ShowNewInvoice || ShowNewExpense || ShowNewRevenue || ShowScanReceipt ||
        ShowNewCustomer || ShowNewSupplier || ShowNewProduct || ShowRecordPayment ||
        ShowNewRentalItem || ShowNewRentalRecord ||
        ShowNewCategory || ShowNewDepartment || ShowNewLocation ||
        ShowNewPurchaseOrder || ShowNewStockAdjustment;

    public bool HasNoVisibleQuickActions => !HasVisibleQuickActions;

    #endregion

    #region Initialization & Config

    public override void Initialize(Dictionary<string, string> config)
    {
        if (config.Count > 0)
        {
            // Apply saved widget config
            ApplyConfig(config);
        }
        else
        {
            // No widget config saved yet — load from legacy GlobalSettings
            LoadQuickActionsSettings();
        }
    }

    public override void LoadData()
    {
        // Quick actions is a pure UI configuration widget — no data to load.
        // Config is applied via Initialize/ApplyConfig only.
    }

    public void LoadQuickActionsSettings()
    {
        var globalSettings = App.SettingsService?.GlobalSettings;
        if (globalSettings != null)
        {
            var qa = globalSettings.Ui.QuickActions;

            ShowNewInvoice = qa.ShowNewInvoice;
            ShowNewExpense = qa.ShowNewExpense;
            ShowNewRevenue = qa.ShowNewRevenue;
            ShowScanReceipt = qa.ShowScanReceipt;
            ShowNewCustomer = qa.ShowNewCustomer;
            ShowNewSupplier = qa.ShowNewSupplier;
            ShowNewProduct = qa.ShowNewProduct;
            ShowRecordPayment = qa.ShowRecordPayment;
            ShowNewRentalItem = qa.ShowNewRentalItem;
            ShowNewRentalRecord = qa.ShowNewRentalRecord;
            ShowNewCategory = qa.ShowNewCategory;
            ShowNewDepartment = qa.ShowNewDepartment;
            ShowNewLocation = qa.ShowNewLocation;
            ShowNewPurchaseOrder = qa.ShowNewPurchaseOrder;
            ShowNewStockAdjustment = qa.ShowNewStockAdjustment;
        }
    }

    public override Dictionary<string, string> GetConfig()
    {
        return new Dictionary<string, string>
        {
            ["ShowNewInvoice"] = ShowNewInvoice.ToString(),
            ["ShowNewExpense"] = ShowNewExpense.ToString(),
            ["ShowNewRevenue"] = ShowNewRevenue.ToString(),
            ["ShowScanReceipt"] = ShowScanReceipt.ToString(),
            ["ShowNewCustomer"] = ShowNewCustomer.ToString(),
            ["ShowNewSupplier"] = ShowNewSupplier.ToString(),
            ["ShowNewProduct"] = ShowNewProduct.ToString(),
            ["ShowRecordPayment"] = ShowRecordPayment.ToString(),
            ["ShowNewRentalItem"] = ShowNewRentalItem.ToString(),
            ["ShowNewRentalRecord"] = ShowNewRentalRecord.ToString(),
            ["ShowNewCategory"] = ShowNewCategory.ToString(),
            ["ShowNewDepartment"] = ShowNewDepartment.ToString(),
            ["ShowNewLocation"] = ShowNewLocation.ToString(),
            ["ShowNewPurchaseOrder"] = ShowNewPurchaseOrder.ToString(),
            ["ShowNewStockAdjustment"] = ShowNewStockAdjustment.ToString()
        };
    }

    public override void ApplyConfig(Dictionary<string, string> config)
    {
        if (config.TryGetValue("ShowNewInvoice", out var v)) ShowNewInvoice = v == "True";
        if (config.TryGetValue("ShowNewExpense", out v)) ShowNewExpense = v == "True";
        if (config.TryGetValue("ShowNewRevenue", out v)) ShowNewRevenue = v == "True";
        if (config.TryGetValue("ShowScanReceipt", out v)) ShowScanReceipt = v == "True";
        if (config.TryGetValue("ShowNewCustomer", out v)) ShowNewCustomer = v == "True";
        if (config.TryGetValue("ShowNewSupplier", out v)) ShowNewSupplier = v == "True";
        if (config.TryGetValue("ShowNewProduct", out v)) ShowNewProduct = v == "True";
        if (config.TryGetValue("ShowRecordPayment", out v)) ShowRecordPayment = v == "True";
        if (config.TryGetValue("ShowNewRentalItem", out v)) ShowNewRentalItem = v == "True";
        if (config.TryGetValue("ShowNewRentalRecord", out v)) ShowNewRentalRecord = v == "True";
        if (config.TryGetValue("ShowNewCategory", out v)) ShowNewCategory = v == "True";
        if (config.TryGetValue("ShowNewDepartment", out v)) ShowNewDepartment = v == "True";
        if (config.TryGetValue("ShowNewLocation", out v)) ShowNewLocation = v == "True";
        if (config.TryGetValue("ShowNewPurchaseOrder", out v)) ShowNewPurchaseOrder = v == "True";
        if (config.TryGetValue("ShowNewStockAdjustment", out v)) ShowNewStockAdjustment = v == "True";
    }

    #endregion

    #region Quick Action Commands

    [RelayCommand]
    private void NewInvoice()
    {
        App.NavigationService?.NavigateTo("Invoices");
        App.InvoiceModalsViewModel?.OpenCreateModal();
    }

    [RelayCommand]
    private void AddExpense()
    {
        App.NavigationService?.NavigateTo("Expenses");
        App.ExpenseModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void RecordSale()
    {
        App.NavigationService?.NavigateTo("Revenue");
        App.RevenueModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private async Task ScanReceipt()
    {
        if (App.ReceiptsModalsViewModel == null) return;
        if (!await App.ReceiptsModalsViewModel.CanScanOrShowLimitAsync()) return;

        App.NavigationService?.NavigateTo("Receipts");
    }

    [RelayCommand]
    private void NewCustomer()
    {
        App.NavigationService?.NavigateTo("Customers");
        App.CustomerModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void NewSupplier()
    {
        App.NavigationService?.NavigateTo("Suppliers");
        App.SupplierModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void NewProduct()
    {
        App.NavigationService?.NavigateTo("Products");
        App.ProductModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void RecordPayment()
    {
        App.NavigationService?.NavigateTo("Payments");
        App.PaymentModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void NewRentalItem()
    {
        App.NavigationService?.NavigateTo("RentalInventory");
        App.RentalInventoryModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void NewRental()
    {
        App.NavigationService?.NavigateTo("RentalRecords");
        App.RentalRecordsModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void NewCategory()
    {
        App.NavigationService?.NavigateTo("Categories");
        App.CategoryModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void NewDepartment()
    {
        App.NavigationService?.NavigateTo("Departments");
        App.DepartmentModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void NewLocation()
    {
        App.NavigationService?.NavigateTo("Locations");
        App.LocationsModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void NewPurchaseOrder()
    {
        App.NavigationService?.NavigateTo("PurchaseOrders");
        App.PurchaseOrdersModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void NewStockAdjustment()
    {
        App.NavigationService?.NavigateTo("Adjustments");
        App.StockAdjustmentsModalsViewModel?.OpenAddModal();
    }

    #endregion
}
