using System.Collections.ObjectModel;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Portal;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Services;
using ArgoBooks.Helpers;
using ArgoBooks.Services;
using ArgoBooks.Localization;
using ArgoBooks.Utilities;
using ArgoBooks.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Receipts page displaying receipt archive management.
/// </summary>
public partial class ReceiptsPageViewModel : ViewModelBase
{
    public ResponsiveHeaderHelper ResponsiveHeader { get; } = new();

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
        if (value) ColumnWidths.NeedsHorizontalScroll = false;
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
    private bool _isSelectionMode;

    [ObservableProperty]
    private bool _hasSelectedReceipts;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private bool _isAllSelected;

    partial void OnIsSelectionModeChanged(bool value)
    {
        // Clear selection when exiting selection mode
        if (!value)
        {
            foreach (var receipt in Receipts)
            {
                receipt.IsSelected = false;
            }
            UpdateSelectionState();
        }
    }

    partial void OnIsAllSelectedChanged(bool value)
    {
        foreach (var receipt in Receipts)
        {
            receipt.IsSelected = value;
        }
        UpdateSelectionState();
    }

    [RelayCommand]
    private void ToggleSelectionMode()
    {
        IsSelectionMode = !IsSelectionMode;
    }

    [RelayCommand]
    private void ExitSelectionMode()
    {
        IsSelectionMode = false;
    }

    [RelayCommand]
    private void ToggleReceiptSelection(ReceiptDisplayItem? receipt)
    {
        if (receipt == null) return;
        receipt.IsSelected = !receipt.IsSelected;
        UpdateSelectionState();
    }

    #endregion

    #region Receipts Collection

    private readonly List<Receipt> _allReceipts = [];

    public BatchObservableCollection<ReceiptDisplayItem> Receipts { get; } = [];

    #endregion

    #region Pagination

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _pageSize = 25; // Default to match ArgoTable's PageSizeOptions

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

    [RelayCommand]
    private void OpenPreview(ReceiptDisplayItem? receipt)
    {
        if (receipt == null) return;

        var title = $"Receipt #{receipt.Id}";
        if (!string.IsNullOrEmpty(receipt.Supplier))
            title += $"\n{receipt.Supplier}";

        App.ReceiptViewerModal?.Show(receipt.ImagePath, receipt.Id, title);
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
    private bool _showIdColumn = ColumnVisibilityHelper.Load("Receipts", "Id", true);

    [ObservableProperty]
    private bool _showSupplierColumn = ColumnVisibilityHelper.Load("Receipts", "Supplier", true);

    [ObservableProperty]
    private bool _showDateColumn = ColumnVisibilityHelper.Load("Receipts", "Date", true);

    [ObservableProperty]
    private bool _showTypeColumn = ColumnVisibilityHelper.Load("Receipts", "Type", true);

    [ObservableProperty]
    private bool _showAmountColumn = ColumnVisibilityHelper.Load("Receipts", "Amount", true);

    partial void OnShowIdColumnChanged(bool value)
    {
        ColumnWidths.SetColumnVisibility("Id", value);
        ColumnVisibilityHelper.Save("Receipts", "Id", value);
    }

    partial void OnShowSupplierColumnChanged(bool value)
    {
        ColumnWidths.SetColumnVisibility("Supplier", value);
        ColumnVisibilityHelper.Save("Receipts", "Supplier", value);
    }

    partial void OnShowDateColumnChanged(bool value)
    {
        ColumnWidths.SetColumnVisibility("Date", value);
        ColumnVisibilityHelper.Save("Receipts", "Date", value);
    }

    partial void OnShowTypeColumnChanged(bool value)
    {
        ColumnWidths.SetColumnVisibility("Type", value);
        ColumnVisibilityHelper.Save("Receipts", "Type", value);
    }

    partial void OnShowAmountColumnChanged(bool value)
    {
        ColumnWidths.SetColumnVisibility("Amount", value);
        ColumnVisibilityHelper.Save("Receipts", "Amount", value);
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

    [RelayCommand]
    private void ResetColumnVisibility()
    {
        ColumnWidths.ResetWidths();
        ColumnVisibilityHelper.ResetPage("Receipts");
        ShowIdColumn = true;
        ShowSupplierColumn = true;
        ShowDateColumn = true;
        ShowTypeColumn = true;
        ShowAmountColumn = true;
    }

    #endregion

    #region AI Scan State

    [ObservableProperty]
    private bool _isDragOver;

    [ObservableProperty]
    private bool _isScannerConfigured;

    /// <summary>
    /// Event raised when a file needs to be scanned via file picker.
    /// The view handles the file picker and passes the result.
    /// </summary>
    public event EventHandler? ScanFileRequested;

    partial void OnHasPremiumChanged(bool value)
    {
        CheckScannerConfiguration();
    }

    private void CheckScannerConfiguration()
    {
        IsScannerConfigured = PortalSettings.IsConfigured;
    }

    /// <summary>
    /// Called by the view after a file is selected or dropped.
    /// </summary>
    public async Task HandleFileSelectedAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        if (App.ReceiptsModalsViewModel == null) return;

        await App.ReceiptsModalsViewModel.OpenScanModalAsync(filePath);
    }

    /// <summary>
    /// Called by the view when files are dropped on the receipts page.
    /// </summary>
    public async Task HandleFilesDroppedAsync(IEnumerable<string> filePaths)
    {
        var validPaths = filePaths
            .Where(p => !string.IsNullOrEmpty(p))
            .Where(p =>
            {
                var ext = Path.GetExtension(p).ToLowerInvariant();
                return ext is ".jpg" or ".jpeg" or ".png" or ".pdf";
            })
            .ToList();

        if (validPaths.Count == 0)
        {
            await App.ShowWarningMessageBoxAsync(
                Loc.Tr("Invalid File"),
                Loc.Tr("Please drop JPEG, PNG, or PDF files."));
            return;
        }

        // Open bulk drop zone and add the files
        var modalsVm = App.ReceiptsModalsViewModel;
        if (modalsVm == null) return;

        if (!modalsVm.IsBulkDropZoneOpen)
            modalsVm.OpenBulkDropZone();

        modalsVm.AddFilesToQueue(validPaths);
    }

    #endregion

    #region Constructor

    public ReceiptsPageViewModel()
    {
        LoadReceipts();
        CheckScannerConfiguration();

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
                r.Supplier.ToLowerInvariant().Contains(query) ||
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
            Supplier = receipt.Supplier,
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
            .Take(PageSize)
            .ToList();

        // Unsubscribe from previous receipt items before replacing
        foreach (var oldItem in Receipts)
        {
            oldItem.PropertyChanged -= OnReceiptItemPropertyChanged;
        }

        foreach (var item in pagedReceipts)
        {
            item.PropertyChanged += OnReceiptItemPropertyChanged;
        }

        Receipts.ReplaceAll(pagedReceipts);
    }

    private void OnReceiptItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReceiptDisplayItem.IsSelected))
        {
            UpdateSelectionState();
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

        // Auto-enter selection mode when items are selected (e.g., via checkbox)
        if (HasSelectedReceipts && !IsSelectionMode)
        {
            IsSelectionMode = true;
        }
    }

    private static string GetReceiptImagePath(Receipt receipt)
    {
        if (string.IsNullOrEmpty(receipt.FileData))
            return string.Empty;

        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ArgoBooks", "Receipts");
            Directory.CreateDirectory(tempDir);
            var bytes = Convert.FromBase64String(receipt.FileData);

            var isPdf = receipt.FileType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true
                        || receipt.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

            if (isPdf)
            {
                // Render first page of the PDF as a JPEG for preview
                #pragma warning disable CA1416 // RenderPdfFirstPage uses PDFium which supports Windows/macOS/Linux (not browser)
                var rendered = ReceiptImageHelper.RenderPdfFirstPage(bytes);
                #pragma warning restore CA1416
                if (rendered == null) return string.Empty;
                var pdfPreviewPath = Path.Combine(tempDir, Path.ChangeExtension(receipt.FileName, ".jpg"));
                File.WriteAllBytes(pdfPreviewPath, rendered);
                return pdfPreviewPath;
            }

            var tempPath = Path.Combine(tempDir, receipt.FileName);
            var output = ReceiptImageHelper.FixOrientation(bytes);
            File.WriteAllBytes(tempPath, output);
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
    private async Task AiScanReceipt()
    {
        if (App.ReceiptsModalsViewModel == null) return;

        // Trigger file picker in the view — usage limit is checked after modal opens
        ScanFileRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task ExportSelected()
    {
        var selectedReceipts = Receipts.Where(r => r.IsSelected).ToList();
        if (selectedReceipts.Count == 0) return;

        try
        {
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow as MainWindow
                : null;

            if (mainWindow?.StorageProvider == null) return;

            // Let user pick a folder to export to
            var folders = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Export Folder",
                AllowMultiple = false
            });

            if (folders.Count == 0) return;

            var baseFolder = folders[0].Path.LocalPath;

            // Create subfolder with company name and date
            var companyName = App.CompanyManager?.CurrentCompanyName ?? "Receipts";
            var safeName = string.Join("_", companyName.Split(Path.GetInvalidFileNameChars()));
            var exportFolderName = $"{safeName}_{DateTime.Now:yyyy-MM-dd}";
            var exportFolder = Path.Combine(baseFolder, exportFolderName);

            Directory.CreateDirectory(exportFolder);

            var exportedCount = 0;

            foreach (var receipt in selectedReceipts)
            {
                if (string.IsNullOrEmpty(receipt.ImagePath) || !File.Exists(receipt.ImagePath))
                    continue;

                var extension = Path.GetExtension(receipt.FileName);
                if (string.IsNullOrEmpty(extension))
                    extension = Path.GetExtension(receipt.ImagePath);

                var fileName = $"Receipt_{receipt.Id}_{receipt.DateFormatted.Replace(",", "").Replace(" ", "_")}{extension}";
                var destinationPath = Path.Combine(exportFolder, fileName);

                File.Copy(receipt.ImagePath, destinationPath, overwrite: true);
                exportedCount++;
            }

            if (exportedCount > 0)
            {
                // Exit selection mode after successful export
                IsSelectionMode = false;
            }
            else
            {
                // Show error message box
                if (mainWindow.MessageBoxService != null)
                {
                    await mainWindow.MessageBoxService.ShowWarningAsync(
                        "Export Failed",
                        "No receipts could be exported. Files may be missing.");
                }
            }
        }
        catch (Exception ex)
        {
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow as MainWindow
                : null;

            if (mainWindow?.MessageBoxService != null)
            {
                await mainWindow.MessageBoxService.ShowErrorAsync(
                    "Export Error",
                    $"Failed to export receipts: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task DownloadReceipt(ReceiptDisplayItem? receipt)
    {
        if (receipt == null || string.IsNullOrEmpty(receipt.ImagePath)) return;

        try
        {
            var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel?.StorageProvider == null) return;

            // Determine file extension from source
            var sourceExtension = Path.GetExtension(receipt.ImagePath);
            if (string.IsNullOrEmpty(sourceExtension))
                sourceExtension = ".png";

            var filters = new[]
            {
                new FilePickerFileType("Image files") { Patterns = [$"*{sourceExtension}"] }
            };

            var suggestedName = $"Receipt_{receipt.Id}{sourceExtension}";

            var result = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Receipt Image",
                SuggestedFileName = suggestedName,
                FileTypeChoices = filters,
                DefaultExtension = sourceExtension.TrimStart('.')
            });

            if (result != null)
            {
                var destinationPath = result.Path.LocalPath;

                // Copy the file
                if (File.Exists(receipt.ImagePath))
                {
                    File.Copy(receipt.ImagePath, destinationPath, overwrite: true);
                    App.AddNotification("Success", "Receipt saved successfully", NotificationType.Success);
                }
            }
        }
        catch (Exception ex)
        {
            await (App.ConfirmationDialog?.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Error",
                Message = $"Failed to save receipt: {ex.Message}",
                PrimaryButtonText = "OK",
                CancelButtonText = null
            }) ?? Task.CompletedTask);
        }
    }

    [RelayCommand]
    private async Task DeleteReceipt(ReceiptDisplayItem? item)
    {
        if (item == null) return;

        try
        {
            var companyData = App.CompanyManager?.CompanyData;
            if (companyData == null) return;

            var receipt = companyData.Receipts.FirstOrDefault(r => r.Id == item.Id);
            if (receipt == null) return;

            var dialog = App.ConfirmationDialog;
            if (dialog == null) return;

            var isLinked = !string.IsNullOrEmpty(receipt.TransactionId);
            var message = "Are you sure you want to delete this receipt?\n\nID: {0}\nSupplier: {1}".TranslateFormat(item.Id, item.Supplier);
            if (isLinked)
            {
                message += "\n\n" + "This receipt is linked to a {0} transaction ({1}). The receipt will be removed from the transaction.".TranslateFormat(
                    receipt.TransactionType, receipt.TransactionId);
            }

            var result = await dialog.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Delete Receipt".Translate(),
                Message = message,
                PrimaryButtonText = "Delete".Translate(),
                CancelButtonText = "Cancel".Translate(),
                IsPrimaryDestructive = true
            });

            if (result != ConfirmationResult.Primary) return;

            App.EventLogService?.CapturePreDeletionSnapshot("Receipt", receipt.Id);

            string? linkedTransactionId = null;
            string? linkedTransactionType = null;
            if (isLinked)
            {
                linkedTransactionId = receipt.TransactionId;
                linkedTransactionType = receipt.TransactionType;

                if (receipt.TransactionType == "Expense")
                {
                    var expense = companyData.Expenses.FirstOrDefault(e => e.Id == receipt.TransactionId);
                    if (expense != null) expense.ReceiptId = null;
                }
                else if (receipt.TransactionType == "Revenue")
                {
                    var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == receipt.TransactionId);
                    if (revenue != null) revenue.ReceiptId = null;
                }
            }

            companyData.Receipts.Remove(receipt);

            var deletedReceipt = receipt;
            var action = new DelegateAction(
                $"Delete receipt {deletedReceipt.Id}",
                () =>
                {
                    companyData.Receipts.Add(deletedReceipt);
                    if (linkedTransactionType == "Expense")
                    {
                        var expense = companyData.Expenses.FirstOrDefault(e => e.Id == linkedTransactionId);
                        if (expense != null) expense.ReceiptId = deletedReceipt.Id;
                    }
                    else if (linkedTransactionType == "Revenue")
                    {
                        var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == linkedTransactionId);
                        if (revenue != null) revenue.ReceiptId = deletedReceipt.Id;
                    }
                },
                () =>
                {
                    companyData.Receipts.Remove(deletedReceipt);
                    if (linkedTransactionType == "Expense")
                    {
                        var expense = companyData.Expenses.FirstOrDefault(e => e.Id == linkedTransactionId);
                        if (expense != null) expense.ReceiptId = null;
                    }
                    else if (linkedTransactionType == "Revenue")
                    {
                        var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == linkedTransactionId);
                        if (revenue != null) revenue.ReceiptId = null;
                    }
                });

            App.UndoRedoManager.RecordAction(action);
            App.CompanyManager?.MarkAsChanged();
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.Validation, "Receipt.DeleteReceipt");
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var receipt in Receipts)
        {
            receipt.IsSelected = true;
        }
        UpdateSelectionState();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var receipt in Receipts)
        {
            receipt.IsSelected = false;
        }
        UpdateSelectionState();
    }

    [RelayCommand]
    private async Task DeleteSelected()
    {
        var selectedReceipts = Receipts.Where(r => r.IsSelected).ToList();
        if (selectedReceipts.Count == 0) return;

        try
        {
            var dialog = App.ConfirmationDialog;
            if (dialog == null) return;

            var companyData = App.CompanyManager?.CompanyData;
            if (companyData == null) return;

            // Check how many are linked to transactions
            var linkedCount = selectedReceipts.Count(r => !string.IsNullOrEmpty(r.TransactionId));

            var message = selectedReceipts.Count == 1
                ? "Are you sure you want to delete this receipt?\n\nID: {0}\nSupplier: {1}".TranslateFormat(selectedReceipts[0].Id, selectedReceipts[0].Supplier)
                : "Are you sure you want to delete {0} receipts?".TranslateFormat(selectedReceipts.Count);

            if (linkedCount > 0)
            {
                message += "\n\n" + "{0} of the selected receipts are linked to transactions. The receipts will be removed from those transactions.".TranslateFormat(linkedCount);
            }

            var result = await dialog.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Delete Receipts".Translate(),
                Message = message,
                PrimaryButtonText = "Delete".Translate(),
                CancelButtonText = "Cancel".Translate(),
                IsPrimaryDestructive = true
            });

            if (result != ConfirmationResult.Primary) return;

            // Find actual Receipt objects to delete and track transaction links for undo
            var receiptsToDelete = new List<Receipt>();
            var transactionLinks = new List<(string TransactionId, string TransactionType, string ReceiptId)>();

            foreach (var displayItem in selectedReceipts)
            {
                var receipt = companyData.Receipts.FirstOrDefault(r => r.Id == displayItem.Id);
                if (receipt == null) continue;

                App.EventLogService?.CapturePreDeletionSnapshot("Receipt", receipt.Id);
                receiptsToDelete.Add(receipt);

                // Unlink from transaction
                if (!string.IsNullOrEmpty(receipt.TransactionId))
                {
                    transactionLinks.Add((receipt.TransactionId, receipt.TransactionType, receipt.Id));

                    if (receipt.TransactionType == "Expense")
                    {
                        var expense = companyData.Expenses.FirstOrDefault(e => e.Id == receipt.TransactionId);
                        if (expense != null) expense.ReceiptId = null;
                    }
                    else if (receipt.TransactionType == "Revenue")
                    {
                        var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == receipt.TransactionId);
                        if (revenue != null) revenue.ReceiptId = null;
                    }
                }
            }

            if (receiptsToDelete.Count == 0) return;

            // Remove all receipts
            foreach (var receipt in receiptsToDelete)
            {
                companyData.Receipts.Remove(receipt);
            }

            // Record undo/redo action
            var capturedReceipts = receiptsToDelete.ToList();
            var capturedLinks = transactionLinks.ToList();
            var action = new DelegateAction(
                $"Delete {capturedReceipts.Count} receipt(s)",
                () =>
                {
                    foreach (var r in capturedReceipts)
                        companyData.Receipts.Add(r);
                    foreach (var link in capturedLinks)
                    {
                        if (link.TransactionType == "Expense")
                        {
                            var expense = companyData.Expenses.FirstOrDefault(e => e.Id == link.TransactionId);
                            if (expense != null) expense.ReceiptId = link.ReceiptId;
                        }
                        else if (link.TransactionType == "Revenue")
                        {
                            var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == link.TransactionId);
                            if (revenue != null) revenue.ReceiptId = link.ReceiptId;
                        }
                    }
                },
                () =>
                {
                    foreach (var r in capturedReceipts)
                        companyData.Receipts.Remove(r);
                    foreach (var link in capturedLinks)
                    {
                        if (link.TransactionType == "Expense")
                        {
                            var expense = companyData.Expenses.FirstOrDefault(e => e.Id == link.TransactionId);
                            if (expense != null) expense.ReceiptId = null;
                        }
                        else if (link.TransactionType == "Revenue")
                        {
                            var revenue = companyData.Revenues.FirstOrDefault(r => r.Id == link.TransactionId);
                            if (revenue != null) revenue.ReceiptId = null;
                        }
                    }
                });

            App.UndoRedoManager.RecordAction(action);
            App.CompanyManager?.MarkAsChanged();

            // Exit selection mode and reload
            IsSelectionMode = false;
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.Validation, "Receipt.DeleteSelected");
        }
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
    private string _supplier = string.Empty;

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
    public string AmountFormatted => CurrencyService.Format(Amount);
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
