using System.Collections.ObjectModel;
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
    public ObservableCollection<string> FileFormats { get; } = new()
    {
        "xlsx",
        "csv",
        "pdf"
    };

    /// <summary>
    /// Data items available for export.
    /// </summary>
    public ObservableCollection<ExportDataItem> DataItems { get; } = new();

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ExportAsModalViewModel()
    {
        // Initialize data items
        DataItems.Add(new ExportDataItem { Name = "Customers", RecordCount = 156, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Invoices", RecordCount = 1245, IsSelected = true });
        DataItems.Add(new ExportDataItem { Name = "Expenses", RecordCount = 892 });
        DataItems.Add(new ExportDataItem { Name = "Products", RecordCount = 342 });
        DataItems.Add(new ExportDataItem { Name = "Inventory", RecordCount = 1847 });
        DataItems.Add(new ExportDataItem { Name = "Payments", RecordCount = 2156 });
        DataItems.Add(new ExportDataItem { Name = "Suppliers", RecordCount = 48 });
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
        IsOpen = true;
    }

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
            var selectedItems = DataItems.Where(x => x.IsSelected).Select(x => x.Name).ToList();
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
public class ExportEventArgs : EventArgs
{
    public string Format { get; }
    public bool IncludeAttachments { get; }
    public List<string> SelectedDataItems { get; }
    public DateTime? StartDate { get; }
    public DateTime? EndDate { get; }

    public ExportEventArgs(string format, bool includeAttachments)
    {
        Format = format;
        IncludeAttachments = includeAttachments;
        SelectedDataItems = new List<string>();
    }

    public ExportEventArgs(string format, List<string> selectedDataItems, DateTime? startDate, DateTime? endDate)
    {
        Format = format;
        SelectedDataItems = selectedDataItems;
        StartDate = startDate;
        EndDate = endDate;
    }
}
