using System.Collections.ObjectModel;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
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

    [ObservableProperty]
    private double _columnMenuX;

    [ObservableProperty]
    private double _columnMenuY;

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

    #region AI Scan State

    [ObservableProperty]
    private bool _isDragOver;

    [ObservableProperty]
    private bool _isAzureConfigured;

    /// <summary>
    /// Event raised when a file needs to be scanned via file picker.
    /// The view handles the file picker and passes the result.
    /// </summary>
    public event EventHandler? ScanFileRequested;

    partial void OnHasPremiumChanged(bool value)
    {
        CheckAzureConfiguration();
    }

    private void CheckAzureConfiguration()
    {
        // Check if Azure credentials are configured in .env file
        IsAzureConfigured = DotEnv.HasValue("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT") &&
                           DotEnv.HasValue("AZURE_DOCUMENT_INTELLIGENCE_API_KEY");
    }

    /// <summary>
    /// Called by the view after a file is selected or dropped.
    /// </summary>
    public async Task HandleFileSelectedAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        if (!HasPremium)
        {
            App.AddNotification(
                Localization.TranslationExtensions.Translate("Premium Feature"),
                Localization.TranslationExtensions.Translate("AI Receipt Scanning requires a Premium subscription."),
                NotificationType.Warning);
            return;
        }

        if (!IsAzureConfigured)
        {
            App.AddNotification(
                Localization.TranslationExtensions.Translate("Configuration Required"),
                Localization.TranslationExtensions.Translate("Please add AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT and AZURE_DOCUMENT_INTELLIGENCE_API_KEY to your .env file."),
                NotificationType.Warning);
            return;
        }

        await App.ReceiptsModalsViewModel!.OpenScanModalAsync(filePath);
    }

    /// <summary>
    /// Called by the view when files are dropped.
    /// </summary>
    public async Task HandleFilesDroppedAsync(IEnumerable<string> filePaths)
    {
        var filePath = filePaths.FirstOrDefault();
        if (string.IsNullOrEmpty(filePath)) return;

        // Validate file type
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".pdf")
        {
            App.AddNotification(
                Localization.TranslationExtensions.Translate("Invalid File"),
                Localization.TranslationExtensions.Translate("Please drop a JPEG, PNG, or PDF file."),
                NotificationType.Warning);
            return;
        }

        await HandleFileSelectedAsync(filePath);
    }

    #endregion

    #region Constructor

    public ReceiptsPageViewModel()
    {
        LoadReceipts();
        CheckAzureConfiguration();

        // Subscribe to undo/redo state changes to refresh UI
        App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;

        // Subscribe to filter modal events
        if (App.ReceiptsModalsViewModel != null)
        {
            App.ReceiptsModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.ReceiptsModalsViewModel.FiltersCleared += OnFiltersCleared;
            App.ReceiptsModalsViewModel.ReceiptScanned += OnReceiptScanned;
        }
    }

    private void OnReceiptScanned(object? sender, EventArgs e)
    {
        // Refresh the receipts list after a new scan
        LoadReceipts();
    }

    private void OnFiltersApplied(object? sender, EventArgs e)
    {
        CurrentPage = 1;
        FilterReceipts();
    }

    private void OnFiltersCleared(object? sender, EventArgs e)
    {
        SearchQuery = null;
        CurrentPage = 1;
        FilterReceipts();
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

        // Get filter values from modals view model
        var modals = App.ReceiptsModalsViewModel;
        var filterType = modals?.FilterType ?? "All";
        var filterSource = modals?.FilterSource ?? "All";
        var filterFileType = modals?.FilterFileType ?? "All";
        var filterAmountMin = modals?.FilterAmountMin;
        var filterAmountMax = modals?.FilterAmountMax;
        var filterDateFrom = modals?.FilterDateFrom;
        var filterDateTo = modals?.FilterDateTo;

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
        if (filterType != "All")
        {
            filtered = filtered.Where(r => r.TransactionType == filterType).ToList();
        }

        // Apply source filter
        if (filterSource != "All")
        {
            filtered = filterSource switch
            {
                "AI Scanned" => filtered.Where(r => r.IsAiScanned).ToList(),
                "Manual" => filtered.Where(r => !r.IsAiScanned).ToList(),
                _ => filtered
            };
        }

        // Apply file type filter
        if (filterFileType != "All")
        {
            filtered = filterFileType switch
            {
                "Image" => filtered.Where(r => IsImageFile(r.FileType)).ToList(),
                "PDF" => filtered.Where(r => r.FileType.Contains("pdf", StringComparison.OrdinalIgnoreCase)).ToList(),
                _ => filtered
            };
        }

        // Apply amount filter
        if (decimal.TryParse(filterAmountMin, out var minAmount))
        {
            filtered = filtered.Where(r => r.Amount >= minAmount).ToList();
        }
        if (decimal.TryParse(filterAmountMax, out var maxAmount))
        {
            filtered = filtered.Where(r => r.Amount <= maxAmount).ToList();
        }

        // Apply date filter
        if (filterDateFrom.HasValue)
        {
            filtered = filtered.Where(r => r.Date >= filterDateFrom.Value.DateTime).ToList();
        }
        if (filterDateTo.HasValue)
        {
            filtered = filtered.Where(r => r.Date <= filterDateTo.Value.DateTime).ToList();
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
        App.ReceiptsModalsViewModel?.OpenFilterModal();
    }

    #endregion

    #region Action Commands

    [RelayCommand]
    private void AiScanReceipt()
    {
        // Trigger file picker in the view
        ScanFileRequested?.Invoke(this, EventArgs.Empty);
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
