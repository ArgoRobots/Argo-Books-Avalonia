using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Represents an exportable data item.
/// </summary>
public partial class ExportDataItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _recordCount;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Internal key used for data lookup (may differ from display name).
    /// </summary>
    public string Key { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for the Export As modal.
/// </summary>
public partial class ExportAsModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _isExporting;

    /// <summary>
    /// Gets whether backup tab is selected.
    /// </summary>
    public bool IsBackupSelected => SelectedTabIndex == 0;

    /// <summary>
    /// Gets whether spreadsheet tab is selected.
    /// </summary>
    public bool IsSpreadsheetSelected => SelectedTabIndex == 1;

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsBackupSelected));
        OnPropertyChanged(nameof(IsSpreadsheetSelected));
    }

    [ObservableProperty]
    private string _selectedFileFormat = "xlsx";

    [ObservableProperty]
    private DateTimeOffset? _startDate = DateTimeOffset.Now.AddMonths(-1);

    [ObservableProperty]
    private DateTimeOffset? _endDate = DateTimeOffset.Now;

    [ObservableProperty]
    private bool _includeAttachments = true;

    [ObservableProperty]
    private bool _selectAllData;

    /// <summary>
    /// Available file formats for spreadsheet export.
    /// </summary>
    public ObservableCollection<string> FileFormats { get; } = ["xlsx"];

    /// <summary>
    /// Data items available for export.
    /// </summary>
    public ObservableCollection<ExportDataItem> DataItems { get; } = [];

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ExportAsModalViewModel()
    {
        InitializeDataItems();
    }

    /// <summary>
    /// Initializes the data items list with all available export types.
    /// </summary>
    private void InitializeDataItems()
    {
        DataItems.Clear();

        // Entities
        DataItems.Add(new ExportDataItem { Name = "Customers", Key = "Customers", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Suppliers", Key = "Suppliers", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Products", Key = "Products", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Categories", Key = "Categories", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Departments", Key = "Departments", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Employees", Key = "Employees", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Locations", Key = "Locations", RecordCount = 0, IsSelected = true });

        // Transactions
        DataItems.Add(new ExportDataItem { Name = "Revenue (Sales)", Key = "Sales", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Expenses (Purchases)", Key = "Purchases", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Invoices", Key = "Invoices", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Payments", Key = "Payments", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Recurring Invoices", Key = "Recurring Invoices", RecordCount = 0, IsSelected = true });

        // Inventory
        DataItems.Add(new ExportDataItem { Name = "Inventory", Key = "Inventory", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Stock Adjustments", Key = "Stock Adjustments", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Purchase Orders", Key = "Purchase Orders", RecordCount = 0, IsSelected = true });

        // Rentals
        DataItems.Add(new ExportDataItem { Name = "Rental Inventory", Key = "Rental Inventory", RecordCount = 0, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Rental Records", Key = "Rental Records", RecordCount = 0, IsSelected = true });
    }

    /// <summary>
    /// Refreshes the record counts from the current company data.
    /// </summary>
    public void RefreshRecordCounts(CompanyData? companyData)
    {
        if (companyData == null)
        {
            foreach (var item in DataItems)
            {
                item.RecordCount = 0;
            }
            return;
        }

        foreach (var item in DataItems)
        {
            item.RecordCount = item.Key switch
            {
                "Customers" => companyData.Customers.Count,
                "Suppliers" => companyData.Suppliers.Count,
                "Products" => companyData.Products.Count,
                "Categories" => companyData.Categories.Count,
                "Departments" => companyData.Departments.Count,
                "Employees" => companyData.Employees.Count,
                "Locations" => companyData.Locations.Count,
                "Sales" => companyData.Sales.Count,
                "Purchases" => companyData.Purchases.Count,
                "Invoices" => companyData.Invoices.Count,
                "Payments" => companyData.Payments.Count,
                "Recurring Invoices" => companyData.RecurringInvoices.Count,
                "Inventory" => companyData.Inventory.Count,
                "Stock Adjustments" => companyData.StockAdjustments.Count,
                "Purchase Orders" => companyData.PurchaseOrders.Count,
                "Rental Inventory" => companyData.RentalInventory.Count,
                "Rental Records" => companyData.Rentals.Count,
                _ => 0
            };
        }
    }

    partial void OnSelectAllDataChanged(bool value)
    {
        foreach (var item in DataItems)
        {
            item.IsSelected = value;
        }
    }

    /// <summary>
    /// Opens the modal.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        SelectedTabIndex = 0;
        // Reset date range to last month
        StartDate = DateTimeOffset.Now.AddMonths(-1);
        EndDate = DateTimeOffset.Now;
        // Request record count refresh from the hosting code
        RefreshRecordCountsRequested?.Invoke(this, EventArgs.Empty);
        IsOpen = true;
    }

    /// <summary>
    /// Event raised when record counts should be refreshed.
    /// </summary>
    public event EventHandler? RefreshRecordCountsRequested;

    /// <summary>
    /// Closes the modal.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Performs the export.
    /// </summary>
    [RelayCommand]
    private void Export()
    {
        if (IsBackupSelected)
        {
            ExportRequested?.Invoke(this, new ExportEventArgs("backup", IncludeAttachments));
        }
        else
        {
            // Use Key for data lookup (internal identifier)
            var selectedItems = DataItems.Where(x => x.IsSelected).Select(x => x.Key).ToList();
            ExportRequested?.Invoke(this, new ExportEventArgs(SelectedFileFormat, selectedItems, StartDate?.DateTime, EndDate?.DateTime));
        }
        Close();
    }

    #region Events

    public event EventHandler<ExportEventArgs>? ExportRequested;

    #endregion
}

/// <summary>
/// Event arguments for export requests.
/// </summary>
public class ExportEventArgs(string format, List<string> selectedDataItems, DateTime? startDate, DateTime? endDate)
    : EventArgs
{
    public string Format { get; } = format;
    public bool IncludeAttachments { get; }
    public List<string> SelectedDataItems { get; } = selectedDataItems;
    public DateTime? StartDate { get; } = startDate;
    public DateTime? EndDate { get; } = endDate;

    public ExportEventArgs(string format, bool includeAttachments) : this(format, [], null, null)
    {
        IncludeAttachments = includeAttachments;
    }
}
