using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Receipts page displaying receipt archive management.
/// </summary>
public partial class ReceiptsPageViewModel : ViewModelBase
{
    #region Statistics

    [ObservableProperty]
    private int _totalReceipts;

    [ObservableProperty]
    private int _expenseReceipts;

    [ObservableProperty]
    private int _revenueReceipts;

    [ObservableProperty]
    private int _aiScannedReceipts;

    #endregion

    #region Plan Status

    [ObservableProperty]
    private bool _hasPremium;

    #endregion

    #region View Mode

    [ObservableProperty]
    private bool _isGridView = true;

    [ObservableProperty]
    private bool _isListView;

    partial void OnIsGridViewChanged(bool value)
    {
        if (value) IsListView = false;
    }

    partial void OnIsListViewChanged(bool value)
    {
        if (value) IsGridView = false;
    }

    [RelayCommand]
    private void SetGridView()
    {
        IsGridView = true;
    }

    [RelayCommand]
    private void SetListView()
    {
        IsListView = true;
    }

    #endregion

    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterReceipts();
    }

    [ObservableProperty]
    private string _filterType = "All";

    [ObservableProperty]
    private string _filterSource = "All";

    [ObservableProperty]
    private string _filterFileType = "All";

    [ObservableProperty]
    private string? _filterAmountMin;

    [ObservableProperty]
    private string? _filterAmountMax;

    [ObservableProperty]
    private DateTimeOffset? _filterDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterDateTo;

    [ObservableProperty]
    private bool _isFilterModalOpen;

    public ObservableCollection<string> TypeOptions { get; } = ["All", "Expense", "Revenue"];
    public ObservableCollection<string> SourceOptions { get; } = ["All", "Manual", "AI Scanned"];
    public ObservableCollection<string> FileTypeOptions { get; } = ["All", "Image", "PDF"];

    #endregion

    #region Selection

    [ObservableProperty]
    private bool _hasSelectedReceipts;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private bool _isAllSelected;

    partial void OnIsAllSelectedChanged(bool value)
    {
        foreach (var receipt in Receipts)
        {
            receipt.IsSelected = value;
        }
        UpdateSelectionState();
    }

    #endregion

    #region Receipts Collection

    private readonly List<Receipt> _allReceipts = [];

    public ObservableCollection<ReceiptDisplayItem> Receipts { get; } = [];

    #endregion

    #region Pagination

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _pageSize = 12; // Grid view typically shows more items

    public ObservableCollection<int> PageSizeOptions { get; } = [8, 12, 16, 24, 48];

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        FilterReceipts();
    }

    [ObservableProperty]
    private string _paginationText = "0 receipts";

    public ObservableCollection<int> PageNumbers { get; } = [];

    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages;

    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        FilterReceipts();
    }

    [RelayCommand]
    private void GoToPreviousPage()
    {
        if (CanGoToPreviousPage)
            CurrentPage--;
    }

    [RelayCommand]
    private void GoToNextPage()
    {
        if (CanGoToNextPage)
            CurrentPage++;
    }

    [RelayCommand]
    private void GoToPage(int page)
    {
        if (page >= 1 && page <= TotalPages)
            CurrentPage = page;
    }

    #endregion

    #region Preview Modal

    [ObservableProperty]
    private bool _isPreviewModalOpen;

    [ObservableProperty]
    private ReceiptDisplayItem? _previewReceipt;

    [ObservableProperty]
    private bool _isPreviewFullscreen;

    [RelayCommand]
    private void OpenPreview(ReceiptDisplayItem? receipt)
    {
        if (receipt == null) return;
        PreviewReceipt = receipt;
        IsPreviewModalOpen = true;
        IsPreviewFullscreen = false;
    }

    [RelayCommand]
    private void ClosePreview()
    {
        IsPreviewModalOpen = false;
        IsPreviewFullscreen = false;
        PreviewReceipt = null;
    }

    [RelayCommand]
    private void TogglePreviewFullscreen()
    {
        IsPreviewFullscreen = !IsPreviewFullscreen;
    }

    #endregion

    #region Column Management

    [ObservableProperty]
    private bool _isColumnMenuOpen;

    public ReceiptsTableColumnWidths ColumnWidths => App.ReceiptsColumnWidths;

    [ObservableProperty]
    private bool _showIdColumn = true;

    [ObservableProperty]
    private bool _showVendorColumn = true;

    [ObservableProperty]
    private bool _showDateColumn = true;

    [ObservableProperty]
    private bool _showTypeColumn = true;

    [ObservableProperty]
    private bool _showAmountColumn = true;

    partial void OnShowIdColumnChanged(bool value)
    {
        ColumnWidths.SetColumnVisibility("Id", value);
    }

    partial void OnShowVendorColumnChanged(bool value)
    {
        ColumnWidths.SetColumnVisibility("Vendor", value);
    }

    partial void OnShowDateColumnChanged(bool value)
    {
        ColumnWidths.SetColumnVisibility("Date", value);
    }

    partial void OnShowTypeColumnChanged(bool value)
    {
        ColumnWidths.SetColumnVisibility("Type", value);
    }

    partial void OnShowAmountColumnChanged(bool value)
    {
        ColumnWidths.SetColumnVisibility("Amount", value);
    }

    [RelayCommand]
    private void ToggleColumnMenu()
    {
        IsColumnMenuOpen = !IsColumnMenuOpen;
    }

    [RelayCommand]
    private void CloseColumnMenu()
    {
        IsColumnMenuOpen = false;
    }

    #endregion

    #region Constructor

    public ReceiptsPageViewModel()
    {
        LoadReceipts();

        // Subscribe to undo/redo state changes to refresh UI
        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }
    }

    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadReceipts();
    }

    #endregion

    #region Data Loading

    private void LoadReceipts()
    {
        _allReceipts.Clear();
        Receipts.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Receipts == null)
            return;

        _allReceipts.AddRange(companyData.Receipts);
        UpdateStatistics();
        FilterReceipts();
    }

    private void UpdateStatistics()
    {
        TotalReceipts = _allReceipts.Count;
        ExpenseReceipts = _allReceipts.Count(r => r.TransactionType == "Expense");
        RevenueReceipts = _allReceipts.Count(r => r.TransactionType == "Revenue");
        AiScannedReceipts = _allReceipts.Count(r => r.IsAiScanned);
    }

    [RelayCommand]
    private void RefreshReceipts()
    {
        LoadReceipts();
    }

    private void FilterReceipts()
    {
        Receipts.Clear();

        var filtered = _allReceipts.ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(r =>
                r.Id.ToLowerInvariant().Contains(query) ||
                r.Vendor.ToLowerInvariant().Contains(query) ||
                r.FileName.ToLowerInvariant().Contains(query) ||
                r.TransactionId.ToLowerInvariant().Contains(query)
            ).ToList();
        }

        // Apply type filter
        if (FilterType != "All")
        {
            filtered = filtered.Where(r => r.TransactionType == FilterType).ToList();
        }

        // Apply source filter
        if (FilterSource != "All")
        {
            filtered = FilterSource switch
            {
                "AI Scanned" => filtered.Where(r => r.IsAiScanned).ToList(),
                "Manual" => filtered.Where(r => !r.IsAiScanned).ToList(),
                _ => filtered
            };
        }

        // Apply file type filter
        if (FilterFileType != "All")
        {
            filtered = FilterFileType switch
            {
                "Image" => filtered.Where(r => IsImageFile(r.FileType)).ToList(),
                "PDF" => filtered.Where(r => r.FileType.Contains("pdf", StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => filtered
            };
        }

        // Apply amount filter
        if (decimal.TryParse(FilterAmountMin, out var minAmount))
        {
            filtered = filtered.Where(r => r.Amount >= minAmount).ToList();
        }
        if (decimal.TryParse(FilterAmountMax, out var maxAmount))
        {
            filtered = filtered.Where(r => r.Amount <= maxAmount).ToList();
        }

        // Apply date filter
        if (FilterDateFrom.HasValue)
        {
            filtered = filtered.Where(r => r.Date >= FilterDateFrom.Value.DateTime).ToList();
        }
        if (FilterDateTo.HasValue)
        {
            filtered = filtered.Where(r => r.Date <= FilterDateTo.Value.DateTime).ToList();
        }

        // Sort by date descending (newest first)
        filtered = filtered.OrderByDescending(r => r.Date).ToList();

        // Create display items
        var displayItems = filtered.Select(receipt => new ReceiptDisplayItem
        {
            Id = receipt.Id,
            TransactionId = receipt.TransactionId,
            TransactionType = receipt.TransactionType,
            FileName = receipt.FileName,
            FileType = receipt.FileType,
            FileSize = receipt.FileSize,
            Amount = receipt.Amount,
            Date = receipt.Date,
            Vendor = receipt.Vendor,
            Source = receipt.Source,
            IsAiScanned = receipt.IsAiScanned,
            CreatedAt = receipt.CreatedAt,
            ImagePath = GetReceiptImagePath(receipt)
        }).ToList();

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination and add to collection
        var pagedReceipts = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedReceipts)
        {
            item.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ReceiptDisplayItem.IsSelected))
                {
                    UpdateSelectionState();
                }
            };
            Receipts.Add(item);
        }
    }

    private static bool IsImageFile(string fileType)
    {
        return fileType.Contains("image", StringComparison.OrdinalIgnoreCase) ||
               fileType.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) ||
               fileType.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase) ||
               fileType.EndsWith("png", StringComparison.OrdinalIgnoreCase) ||
               fileType.EndsWith("gif", StringComparison.OrdinalIgnoreCase) ||
               fileType.EndsWith("webp", StringComparison.OrdinalIgnoreCase);
    }

    private void UpdatePageNumbers()
    {
        PageNumbers.Clear();
        var startPage = Math.Max(1, CurrentPage - 2);
        var endPage = Math.Min(TotalPages, startPage + 4);
        startPage = Math.Max(1, endPage - 4);

        for (var i = startPage; i <= endPage; i++)
        {
            PageNumbers.Add(i);
        }
    }

    private void UpdatePaginationText(int totalCount)
    {
        PaginationText = PaginationTextHelper.FormatPaginationText(
            totalCount, CurrentPage, PageSize, TotalPages, "receipt");
    }

    private void UpdateSelectionState()
    {
        SelectedCount = Receipts.Count(r => r.IsSelected);
        HasSelectedReceipts = SelectedCount > 0;
    }

    private static string GetReceiptImagePath(Receipt receipt)
    {
        if (string.IsNullOrEmpty(receipt.FileData))
            return string.Empty;

        try
        {
            // Create temp file from Base64 data stored in company file
            var tempDir = Path.Combine(Path.GetTempPath(), "ArgoBooks", "Receipts");
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, receipt.FileName);
            var bytes = Convert.FromBase64String(receipt.FileData);
            File.WriteAllBytes(tempPath, bytes);
            return tempPath;
        }
        catch
        {
            return string.Empty;
        }
    }

    #endregion

    #region Filter Modal Commands

    [RelayCommand]
    private void OpenFilterModal()
    {
        IsFilterModalOpen = true;
    }

    [RelayCommand]
    private void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        CurrentPage = 1;
        FilterReceipts();
        IsFilterModalOpen = false;
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterType = "All";
        FilterSource = "All";
        FilterFileType = "All";
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterDateFrom = null;
        FilterDateTo = null;
        SearchQuery = null;
        CurrentPage = 1;
        FilterReceipts();
        IsFilterModalOpen = false;
    }

    #endregion

    #region Action Commands

    [RelayCommand]
    private void AiScanReceipt()
    {
        // TODO: Open AI scan modal/workflow
        // This would integrate with Google Cloud Vision API
    }

    [RelayCommand]
    private void ExportSelected()
    {
        var selectedReceipts = Receipts.Where(r => r.IsSelected).ToList();
        if (selectedReceipts.Count == 0) return;

        // TODO: Implement export functionality
        // Export selected receipts to ZIP or folder
    }

    [RelayCommand]
    private void DownloadReceipt(ReceiptDisplayItem? receipt)
    {
        if (receipt == null) return;
        // TODO: Implement download functionality
    }

    [RelayCommand]
    private void DeleteReceipt(ReceiptDisplayItem? receipt)
    {
        if (receipt == null) return;
        // TODO: Implement delete with confirmation
    }

    [RelayCommand]
    private void SelectAll()
    {
        IsAllSelected = true;
    }

    [RelayCommand]
    private void DeselectAll()
    {
        IsAllSelected = false;
    }

    #endregion
}

/// <summary>
/// Display model for receipts in the UI.
/// </summary>
public partial class ReceiptDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _transactionId = string.Empty;

    [ObservableProperty]
    private string _transactionType = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _fileType = string.Empty;

    [ObservableProperty]
    private long _fileSize;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private string _vendor = string.Empty;

    [ObservableProperty]
    private string _source = string.Empty;

    [ObservableProperty]
    private bool _isAiScanned;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _imagePath = string.Empty;

    // Computed properties for display
    public string DateFormatted => Date.ToString("MMM d, yyyy");
    public string AmountFormatted => $"${Amount:N2}";
    public string FileSizeFormatted => FormatFileSize(FileSize);

    public bool IsExpense => TransactionType == "Expense";
    public bool IsRevenue => TransactionType == "Revenue";

    public bool IsImage => FileType.Contains("image", StringComparison.OrdinalIgnoreCase) ||
                           FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                           FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                           FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

    public bool IsPdf => FileType.Contains("pdf", StringComparison.OrdinalIgnoreCase) ||
                         FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public bool HasImage => !string.IsNullOrEmpty(ImagePath);

    public string TypeBadgeText => TransactionType;

    public string SourceBadgeText => IsAiScanned ? "AI Scanned" : "Manual";

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
