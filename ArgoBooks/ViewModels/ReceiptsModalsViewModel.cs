using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.AI;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for Receipts modals (Filter, AI Scan Review).
/// </summary>
public partial class ReceiptsModalsViewModel : ViewModelBase
{
    /// <summary>
    /// Design-time instance for Avalonia previewer in Rider/VS.
    /// Provides sample data so the scan review modal is visible in the preview.
    /// </summary>
    public static ReceiptsModalsViewModel DesignInstance { get; } = CreateDesignInstance();

    private static ReceiptsModalsViewModel CreateDesignInstance()
    {
        var sampleProduct = new ProductOption { Id = "1", Name = "Classico Traditional Pizza Sauce", UnitPrice = 11.88m };
        var sampleSupplier = new SupplierOption { Id = "1", Name = "Independent Grocer" };

        var vm = new ReceiptsModalsViewModel
        {
            IsScanReviewModalOpen = true,
            HasScanResult = true,
            IsHighConfidence = true,
            ConfidenceText = "95%",
            ExtractedTotal = "25.74",
            ExtractedSubtotal = "22.86",
            ExtractedTax = "2.88",
            ExtractedDiscount = "0.00",
            ExtractedSupplier = "Independent Grocer",
            SelectedPaymentMethod = "Debit Card"
        };
        vm.ProductOptions.Add(sampleProduct);
        vm.SupplierOptions.Add(sampleSupplier);
        vm.SelectedSupplier = sampleSupplier;
        vm.LineItems.Add(new ScannedLineItemViewModel { Description = "CLSO TRAD PZA S MRJ", Quantity = "1", UnitPrice = "11.88", TotalPrice = "11.88", SelectedProduct = sampleProduct });
        vm.LineItems.Add(new ScannedLineItemViewModel { Description = "CLSO SORNT ONION MRJ", Quantity = "1", UnitPrice = "2.97", TotalPrice = "2.97", ShowCreateProductSuggestion = true, SuggestedProductName = "Clso Sornt Onion" });
        vm.LineItems.Add(new ScannedLineItemViewModel { Description = "UH SOYA SCE MRJ", Quantity = "1", UnitPrice = "4.79", TotalPrice = "4.79" });
        return vm;
    }

    #region Events

    /// <summary>
    /// Raised when filters are applied.
    /// </summary>
    public event EventHandler? FiltersApplied;

    /// <summary>
    /// Raised when filters are cleared.
    /// </summary>
    public event EventHandler? FiltersCleared;

    /// <summary>
    /// Raised when a receipt scan creates a new expense.
    /// </summary>
    public event EventHandler? ReceiptScanned;

    #endregion

    #region Filter Modal State

    [ObservableProperty]
    private bool _isFilterModalOpen;

    [ObservableProperty]
    private string _filterType = "All";

    [ObservableProperty]
    private DateTimeOffset? _filterDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterDateTo;

    [ObservableProperty]
    private string? _filterAmountMin;

    [ObservableProperty]
    private string? _filterAmountMax;

    [ObservableProperty]
    private string _filterSource = "All";

    [ObservableProperty]
    private string _filterFileType = "All";

    /// <summary>
    /// Receipt type filter options.
    /// </summary>
    public ObservableCollection<string> TypeOptions { get; } = ["All", "Expense", "Revenue"];

    /// <summary>
    /// Source filter options.
    /// </summary>
    public ObservableCollection<string> SourceOptions { get; } = ["All", "Manual", "AI Scanned"];

    /// <summary>
    /// File type filter options.
    /// </summary>
    public ObservableCollection<string> FileTypeOptions { get; } = ["All", "Image", "PDF"];

    // Original filter values for change detection
    private string _originalFilterType = "All";
    private DateTimeOffset? _originalFilterDateFrom;
    private DateTimeOffset? _originalFilterDateTo;
    private string? _originalFilterAmountMin;
    private string? _originalFilterAmountMax;
    private string _originalFilterSource = "All";
    private string _originalFilterFileType = "All";

    /// <summary>
    /// Returns true if any filter has been changed from its original value when the modal was opened.
    /// </summary>
    public bool HasFilterModalChanges =>
        FilterType != _originalFilterType ||
        FilterDateFrom != _originalFilterDateFrom ||
        FilterDateTo != _originalFilterDateTo ||
        FilterAmountMin != _originalFilterAmountMin ||
        FilterAmountMax != _originalFilterAmountMax ||
        FilterSource != _originalFilterSource ||
        FilterFileType != _originalFilterFileType;

    /// <summary>
    /// Captures the current filter values as the original values for change detection.
    /// </summary>
    private void CaptureOriginalFilterValues()
    {
        _originalFilterType = FilterType;
        _originalFilterDateFrom = FilterDateFrom;
        _originalFilterDateTo = FilterDateTo;
        _originalFilterAmountMin = FilterAmountMin;
        _originalFilterAmountMax = FilterAmountMax;
        _originalFilterSource = FilterSource;
        _originalFilterFileType = FilterFileType;
    }

    /// <summary>
    /// Restores filter values to their original values when the modal was opened.
    /// </summary>
    private void RestoreOriginalFilterValues()
    {
        FilterType = _originalFilterType;
        FilterDateFrom = _originalFilterDateFrom;
        FilterDateTo = _originalFilterDateTo;
        FilterAmountMin = _originalFilterAmountMin;
        FilterAmountMax = _originalFilterAmountMax;
        FilterSource = _originalFilterSource;
        FilterFileType = _originalFilterFileType;
    }

    #endregion

    #region Filter Modal Commands

    /// <summary>
    /// Opens the filter modal.
    /// </summary>
    public void OpenFilterModal()
    {
        CaptureOriginalFilterValues();
        IsFilterModalOpen = true;
    }

    /// <summary>
    /// Closes the filter modal.
    /// </summary>
    [RelayCommand]
    private void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    /// <summary>
    /// Requests to close the filter modal, showing confirmation if there are unapplied changes.
    /// </summary>
    [RelayCommand]
    public async Task RequestCloseFilterModalAsync()
    {
        if (HasFilterModalChanges)
        {
            if (!await ConfirmDiscardFiltersAsync())
                return;

            // Restore filter values to original values
            RestoreOriginalFilterValues();
        }

        CloseFilterModal();
    }

    /// <summary>
    /// Applies the current filters.
    /// </summary>
    [RelayCommand]
    private void ApplyFilters()
    {
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        ResetFilterDefaults();
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    #endregion

    #region AI Scan Review Modal State

    private IReceiptScannerService? _scannerService;
    private IReceiptUsageService? _usageService;

    /// <summary>
    /// Invalidates cached scan services so the next scan attempt picks up
    /// current license/usage state. Call after plan status changes (upgrade/downgrade).
    /// </summary>
    public void InvalidateScanServices()
    {
        _usageService?.InvalidateCache();
        _usageService = null;
        _scannerService = null;
    }
    private byte[]? _currentImageData;
    private string? _currentFileName;

    // Track entities created during receipt flow for undo
    private Supplier? _createdSupplierForUndo;
    private Category? _createdCategoryForUndo;
    private readonly List<Product> _createdProductsForUndo = new();

    [ObservableProperty]
    private bool _isScanReviewModalOpen;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _scanningMessage = "Analyzing receipt...";

    [ObservableProperty]
    private bool _hasScanError;

    [ObservableProperty]
    private string _scanErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasScanResult;

    [ObservableProperty]
    private bool _isFullscreen;

    /// <summary>
    /// Gets the modal width based on current state.
    /// Narrower for loading/error states, wider for results.
    /// NaN when fullscreen (stretches to fill).
    /// </summary>
    public double ModalWidth => IsFullscreen ? double.NaN : (HasScanResult ? 1100 : 520);

    /// <summary>
    /// Gets the modal height. NaN when fullscreen (stretches to fill).
    /// </summary>
    public double ModalHeight => IsFullscreen ? double.NaN : (HasScanResult ? 850 : 400);

    /// <summary>
    /// Gets the modal margin. Zero when fullscreen, auto-centered otherwise.
    /// </summary>
    public Avalonia.Thickness ModalMargin => IsFullscreen ? new Avalonia.Thickness(8) : new Avalonia.Thickness(0);

    /// <summary>
    /// Gets modal horizontal alignment. Stretch when fullscreen.
    /// </summary>
    public Avalonia.Layout.HorizontalAlignment ModalHorizontalAlignment =>
        IsFullscreen ? Avalonia.Layout.HorizontalAlignment.Stretch : Avalonia.Layout.HorizontalAlignment.Center;

    /// <summary>
    /// Gets modal vertical alignment. Stretch when fullscreen.
    /// </summary>
    public Avalonia.Layout.VerticalAlignment ModalVerticalAlignment =>
        IsFullscreen ? Avalonia.Layout.VerticalAlignment.Stretch : Avalonia.Layout.VerticalAlignment.Center;

    partial void OnHasScanResultChanged(bool value)
    {
        OnPropertyChanged(nameof(ModalWidth));
        OnPropertyChanged(nameof(ModalHeight));
    }

    partial void OnIsFullscreenChanged(bool value)
    {
        OnPropertyChanged(nameof(ModalWidth));
        OnPropertyChanged(nameof(ModalHeight));
        OnPropertyChanged(nameof(ModalMargin));
        OnPropertyChanged(nameof(ModalHorizontalAlignment));
        OnPropertyChanged(nameof(ModalVerticalAlignment));
    }

    [ObservableProperty]
    private string? _receiptImagePath;

    [ObservableProperty]
    private string _extractedSupplier = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _extractedDate;

    [ObservableProperty]
    private string _extractedSubtotal = string.Empty;

    [ObservableProperty]
    private string _extractedTax = string.Empty;

    [ObservableProperty]
    private string _extractedDiscount = string.Empty;

    [ObservableProperty]
    private string _extractedTotal = string.Empty;

    [ObservableProperty]
    private double _confidenceScore;

    [ObservableProperty]
    private string _confidenceText = string.Empty;

    [ObservableProperty]
    private bool _isHighConfidence;

    [ObservableProperty]
    private bool _isMediumConfidence;

    [ObservableProperty]
    private bool _isLowConfidence;

    public ObservableCollection<ScannedLineItemViewModel> LineItems { get; } = [];

    [ObservableProperty]
    private SupplierOption? _selectedSupplier;

    [ObservableProperty]
    private string _selectedPaymentMethod = "Cash";

    [ObservableProperty]
    private string _notes = string.Empty;

    /// <summary>
    /// Whether this is a revenue transaction (true) or expense transaction (false).
    /// Determined by comparing extracted merchant name against the user's company name.
    /// </summary>
    [ObservableProperty]
    private bool _isRevenue;

    /// <summary>
    /// The detected transaction type label for UI display.
    /// </summary>
    public string TransactionTypeLabel => IsRevenue ? "Revenue".Translate() : "Expense".Translate();

    partial void OnIsRevenueChanged(bool value)
    {
        OnPropertyChanged(nameof(TransactionTypeLabel));
    }

    /// <summary>
    /// Sets the transaction type (expense or revenue).
    /// </summary>
    [RelayCommand]
    private void SetTransactionType(string type)
    {
        IsRevenue = type == "Revenue";
    }

    public ObservableCollection<SupplierOption> SupplierOptions { get; } = [];
    public ObservableCollection<ProductOption> ProductOptions { get; } = [];
    public ObservableCollection<string> PaymentMethodOptions { get; } = new(PaymentMethodExtensions.GetCommonOptions());

    [ObservableProperty]
    private bool _hasTotalError;

    [ObservableProperty]
    private string _totalErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasUnmatchedProducts;

    [ObservableProperty]
    private bool _hasTotalMismatchWarning;

    [ObservableProperty]
    private string _totalMismatchWarningMessage = string.Empty;

    [ObservableProperty]
    private bool _hasLineItemsError;

    [ObservableProperty]
    private string _lineItemsErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasSupplierError;

    [ObservableProperty]
    private string _supplierErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasValidationMessage;

    partial void OnSelectedSupplierChanged(SupplierOption? value)
    {
        if (value != null)
        {
            HasSupplierError = false;
            SupplierErrorMessage = string.Empty;
            ClearValidationMessageIfNoErrors();
        }
    }

    partial void OnExtractedTotalChanged(string value) => ValidateTotals();
    partial void OnExtractedSubtotalChanged(string value) => ValidateTotals();
    partial void OnExtractedTaxChanged(string value) => ValidateTotals();
    partial void OnExtractedDiscountChanged(string value) => ValidateTotals();

    partial void OnHasTotalErrorChanged(bool value)
    {
        if (!value) ClearValidationMessageIfNoErrors();
    }

    partial void OnHasLineItemsErrorChanged(bool value)
    {
        if (!value) ClearValidationMessageIfNoErrors();
    }

    private void ClearValidationMessageIfNoErrors()
    {
        if (!HasTotalError && !HasSupplierError && !HasLineItemsError &&
            LineItems.All(li => !li.HasProductError))
        {
            HasValidationMessage = false;
        }
    }

    // AI Suggestion State
    [ObservableProperty]
    private bool _isLoadingAiSuggestions;

    private SupplierCategorySuggestion? _aiSuggestion;

    [ObservableProperty]
    private bool _showCreateSupplierSuggestion;

    [ObservableProperty]
    private string _suggestedSupplierName = string.Empty;

    // Usage tracking state
    [ObservableProperty]
    private int _scansUsed;

    [ObservableProperty]
    private int _scansLimit;

    [ObservableProperty]
    private int _scansRemaining;

    [ObservableProperty]
    private bool _hasUsageInfo;

    [ObservableProperty]
    private bool _isNearLimit;

    [ObservableProperty]
    private string? _usageTier;

    [ObservableProperty]
    private string? _resetsAt;

    #endregion

    #region Bulk Scan State

    [ObservableProperty]
    private bool _isBulkDropZoneOpen;

    [ObservableProperty]
    private bool _isBulkScanning;

    [ObservableProperty]
    private bool _isBulkScanComplete;

    [ObservableProperty]
    private bool _isBulkReviewOpen;

    public ObservableCollection<BulkScanItem> BulkItems { get; } = [];

    [ObservableProperty]
    private BulkScanItem? _currentBulkItem;

    [ObservableProperty]
    private int _currentBulkIndex;

    [ObservableProperty]
    private int _bulkScansCompleted;

    [ObservableProperty]
    private int _bulkScansSucceeded;

    [ObservableProperty]
    private int _bulkScansFailed;

    public int BulkApprovedCount => BulkItems.Count(i => i.IsApproved);

    public IReadOnlyList<BulkScanItem> BulkSucceededItems =>
        BulkItems.Where(i => i.Status == BulkScanStatus.Succeeded).ToList();

    public int BulkProgressPercent => BulkItems.Count > 0
        ? (int)(BulkScansCompleted / (double)BulkItems.Count * 100)
        : 0;

    partial void OnBulkScansCompletedChanged(int value)
    {
        OnPropertyChanged(nameof(BulkProgressPercent));
        OnPropertyChanged(nameof(BulkSucceededItems));
    }

    private CancellationTokenSource? _bulkCancellationSource;

    #endregion

    #region Bulk Scan Commands

    public void OpenBulkDropZone()
    {
        BulkItems.Clear();
        BulkScansCompleted = 0;
        BulkScansSucceeded = 0;
        BulkScansFailed = 0;
        IsBulkDropZoneOpen = true;
        IsBulkScanning = false;
        IsBulkScanComplete = false;
        IsBulkReviewOpen = false;
    }

    public void AddFilesToQueue(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension is not (".jpg" or ".jpeg" or ".png" or ".pdf"))
                continue;

            if (BulkItems.Any(i => i.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
                continue;

            BulkItems.Add(new BulkScanItem
            {
                FilePath = path,
                FileName = Path.GetFileName(path)
            });
        }

        OnPropertyChanged(nameof(BulkApprovedCount));
    }

    [RelayCommand]
    private void RemoveFromQueue(BulkScanItem? item)
    {
        if (item != null)
            BulkItems.Remove(item);
    }

    [RelayCommand]
    private void CloseBulkDropZone()
    {
        IsBulkDropZoneOpen = false;
        BulkItems.Clear();
    }

    [RelayCommand]
    private async Task StartBulkScan()
    {
        if (BulkItems.Count == 0) return;

        IsBulkDropZoneOpen = false;
        IsBulkScanning = true;
        IsBulkScanComplete = false;
        BulkScansCompleted = 0;
        BulkScansSucceeded = 0;
        BulkScansFailed = 0;

        _scannerService ??= CreateScannerService();
        _usageService ??= CreateUsageService();
        _bulkCancellationSource = new CancellationTokenSource();
        var token = _bulkCancellationSource.Token;

        if (_usageService != null)
        {
            var usageCheck = await _usageService.CheckUsageAsync();
            UpdateUsageDisplay(usageCheck);

            if (!usageCheck.CanScan)
            {
                IsBulkScanning = false;
                await UpgradePromptHelper.ShowReceiptScanLimitPromptAsync(
                    usageCheck.ScanCount, usageCheck.MonthlyLimit, usageCheck.ResetsAt);
                return;
            }

            if (usageCheck.Remaining < BulkItems.Count)
            {
                ScansRemaining = usageCheck.Remaining;
            }
        }

        // Read and preprocess all files first
        foreach (var item in BulkItems)
        {
            if (token.IsCancellationRequested) break;
            try
            {
                item.FileData = await File.ReadAllBytesAsync(item.FilePath, token);
                var isPdf = item.IsPdf;
                var fileData = item.FileData;
                var fileName = item.FileName;

                item.PreprocessedData = await Task.Run(() =>
                    ReceiptImageHelper.PreprocessForOcr(fileData, fileName), token);
                item.ScanFileName = isPdf ? fileName : Path.ChangeExtension(fileName, ".jpg");

                _ = Task.Run(async () =>
                {
                    var tempDir = Path.Combine(Path.GetTempPath(), "ArgoBooks", "BulkScanPreview");
                    Directory.CreateDirectory(tempDir);
                    var previewPath = Path.Combine(tempDir, $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid():N}.jpg");

                    #pragma warning disable CA1416 // RenderPdfFirstPage uses PDFium which supports Windows/macOS/Linux (not browser)
                    var previewBytes = isPdf
                        ? ReceiptImageHelper.RenderPdfFirstPage(fileData)
                        : item.PreprocessedData;
                    #pragma warning restore CA1416

                    if (previewBytes != null)
                    {
                        await File.WriteAllBytesAsync(previewPath, previewBytes);
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            item.PreviewImagePath = previewPath);
                    }
                }, token);
            }
            catch (Exception ex)
            {
                item.Status = BulkScanStatus.Failed;
                item.ErrorMessage = ex.Message;
                BulkScansCompleted++;
                BulkScansFailed++;
            }
        }

        var semaphore = new SemaphoreSlim(3);
        var scanTasks = BulkItems
            .Where(i => i.Status == BulkScanStatus.Queued && i.PreprocessedData != null)
            .Select(item => ScanSingleItemAsync(item, semaphore, token))
            .ToList();

        await Task.WhenAll(scanTasks);

        IsBulkScanning = false;
        IsBulkScanComplete = true;
    }

    private async Task ScanSingleItemAsync(BulkScanItem item, SemaphoreSlim semaphore, CancellationToken token)
    {
        await semaphore.WaitAsync(token);
        try
        {
            if (token.IsCancellationRequested) return;

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                item.Status = BulkScanStatus.Scanning);

            if (_usageService != null)
            {
                var usageCheck = await _usageService.CheckUsageAsync();
                if (!usageCheck.CanScan)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        item.Status = BulkScanStatus.Failed;
                        item.ErrorMessage = "Monthly scan limit reached";
                        BulkScansCompleted++;
                        BulkScansFailed++;
                    });
                    return;
                }
            }

            var result = await Task.Run(() =>
                _scannerService!.ScanReceiptAsync(item.PreprocessedData!, item.ScanFileName, skipPreprocessing: true, token), token);

            if (result.IsSuccess)
            {
                if (_usageService != null)
                    await _usageService.IncrementUsageAsync();

                var supplier = result.SupplierName ?? "Unknown";
                var total = result.TotalAmount?.ToString("F2") ?? "0.00";

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    item.ScanResult = result;
                    item.Status = BulkScanStatus.Succeeded;
                    item.SummaryText = $"{supplier} · ${total}";
                    item.ConfidenceText = result.Confidence >= 0.85 ? "High" : result.Confidence >= 0.6 ? "Medium" : "Low";
                    BulkScansCompleted++;
                    BulkScansSucceeded++;
                });
            }
            else
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    item.Status = BulkScanStatus.Failed;
                    item.ErrorMessage = result.ErrorMessage ?? "Unknown error";
                    BulkScansCompleted++;
                    BulkScansFailed++;
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                item.Status = BulkScanStatus.Failed;
                item.ErrorMessage = ex.Message;
                BulkScansCompleted++;
                BulkScansFailed++;
            });
        }
        finally
        {
            semaphore.Release();
        }
    }

    [RelayCommand]
    private async Task RetryBulkItem(BulkScanItem? item)
    {
        if (item == null || item.PreprocessedData == null) return;

        // Don't allow retry while scanning is still in progress (would cause duplicate concurrent scan)
        if (IsBulkScanning) return;

        // Reset cancelled token if user cancelled a previous batch
        if (_bulkCancellationSource?.IsCancellationRequested == true)
            _bulkCancellationSource = new CancellationTokenSource();

        item.Status = BulkScanStatus.Queued;
        item.ErrorMessage = string.Empty;
        BulkScansFailed--;
        BulkScansCompleted--;

        var semaphore = new SemaphoreSlim(1);
        await ScanSingleItemAsync(item, semaphore, _bulkCancellationSource?.Token ?? CancellationToken.None);
    }

    [RelayCommand]
    private void CancelBulkScan()
    {
        _bulkCancellationSource?.Cancel();
        IsBulkScanning = false;
        IsBulkScanComplete = true;
    }

    [RelayCommand]
    private void OpenBulkReview()
    {
        var succeeded = BulkSucceededItems;
        if (succeeded.Count == 0) return;

        IsBulkScanComplete = false;
        IsBulkReviewOpen = true;

        LoadSupplierOptions();
        LoadProductOptions();

        NavigateToBulkItem(0);
    }

    private void NavigateToBulkItem(int index)
    {
        var succeeded = BulkSucceededItems;
        if (index < 0 || index >= succeeded.Count) return;

        // Save current form state before navigating away
        if (CurrentBulkItem != null)
        {
            SaveCurrentFormToItem(CurrentBulkItem);
            CurrentBulkItem.IsActive = false;
        }

        CurrentBulkIndex = index;
        CurrentBulkItem = succeeded[index];
        CurrentBulkItem.IsActive = true;

        PopulateScanResultsForBulkItem(CurrentBulkItem);
    }

    [RelayCommand]
    private void NavigateToBulkItemByRef(BulkScanItem? item)
    {
        if (item == null) return;
        var succeeded = BulkSucceededItems;
        var index = ((IList<BulkScanItem>)succeeded).IndexOf(item);
        if (index >= 0)
            NavigateToBulkItem(index);
    }

    private void PopulateScanResultsForBulkItem(BulkScanItem item)
    {
        if (item.ScanResult == null) return;

        _currentImageData = item.PreprocessedData;
        _currentFileName = item.ScanFileName;

        ReceiptImagePath = item.PreviewImagePath;

        PopulateScanResults(item.ScanResult);
    }

    [RelayCommand]
    private void ApproveBulkItem()
    {
        if (CurrentBulkItem == null) return;

        SaveCurrentFormToItem(CurrentBulkItem);
        CurrentBulkItem.IsApproved = true;
        OnPropertyChanged(nameof(BulkApprovedCount));

        var succeeded = BulkSucceededItems;
        var nextIndex = -1;

        for (var i = CurrentBulkIndex + 1; i < succeeded.Count; i++)
        {
            if (!succeeded[i].IsApproved)
            {
                nextIndex = i;
                break;
            }
        }

        if (nextIndex == -1)
        {
            for (var i = 0; i < CurrentBulkIndex; i++)
            {
                if (!succeeded[i].IsApproved)
                {
                    nextIndex = i;
                    break;
                }
            }
        }

        if (nextIndex >= 0)
            NavigateToBulkItem(nextIndex);
    }

    [RelayCommand]
    private void UnapproveBulkItem()
    {
        if (CurrentBulkItem == null) return;
        CurrentBulkItem.IsApproved = false;
        OnPropertyChanged(nameof(BulkApprovedCount));
    }

    [RelayCommand]
    private void SkipBulkItem()
    {
        var succeeded = BulkSucceededItems;
        var nextIndex = CurrentBulkIndex + 1;
        if (nextIndex < succeeded.Count)
            NavigateToBulkItem(nextIndex);
    }

    private void SaveCurrentFormToItem(BulkScanItem item)
    {
        if (item.ScanResult == null) return;

        item.ScanResult.SupplierName = ExtractedSupplier;
        item.ScanResult.TransactionDate = ExtractedDate?.DateTime;

        if (decimal.TryParse(ExtractedSubtotal, out var sub)) item.ScanResult.Subtotal = sub;
        if (decimal.TryParse(ExtractedTax, out var tax)) item.ScanResult.TaxAmount = tax;
        if (decimal.TryParse(ExtractedTotal, out var tot)) item.ScanResult.TotalAmount = tot;
        if (decimal.TryParse(ExtractedDiscount, out var disc)) item.ScanResult.Discount = disc;

        item.ScanResult.PaymentMethod = SelectedPaymentMethod;

        item.ScanResult.LineItems = LineItems.Select(li =>
        {
            decimal.TryParse(li.Quantity, out var qty);
            decimal.TryParse(li.UnitPrice, out var up);
            decimal.TryParse(li.TotalPrice, out var tp);
            return new ScannedLineItem
            {
                Description = li.Description,
                Quantity = qty,
                UnitPrice = up,
                TotalPrice = tp,
                Confidence = li.Confidence
            };
        }).ToList();

        var supplier = ExtractedSupplier;
        if (string.IsNullOrEmpty(supplier) && SelectedSupplier != null)
            supplier = SelectedSupplier.Name;
        item.SummaryText = $"{supplier} · ${ExtractedTotal}";
    }

    [RelayCommand]
    private async Task CreateAllApprovedTransactions()
    {
        var approvedItems = BulkItems.Where(i => i.IsApproved && i.ScanResult != null).ToList();
        if (approvedItems.Count == 0) return;

        if (CurrentBulkItem != null && CurrentBulkItem.IsApproved)
            SaveCurrentFormToItem(CurrentBulkItem);

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var createdExpenses = new List<Expense>();
        var createdRevenues = new List<Revenue>();
        var createdReceipts = new List<Receipt>();
        var createdSuppliers = new List<Supplier>();
        var createdCategories = new List<Category>();
        var createdProducts = new List<Product>();

        foreach (var item in approvedItems)
        {
            PopulateScanResultsForBulkItem(item);

            _createdSupplierForUndo = null;
            _createdCategoryForUndo = null;
            _createdProductsForUndo.Clear();

            decimal.TryParse(ExtractedTotal, out var total);
            decimal.TryParse(ExtractedSubtotal, out var subtotal);
            decimal.TryParse(ExtractedTax, out var taxAmount);
            decimal.TryParse(ExtractedDiscount, out var discount);

            if (total <= 0) continue;

            var lineItems = LineItems.Select(li =>
            {
                decimal.TryParse(li.Quantity, out var qty);
                decimal.TryParse(li.UnitPrice, out var unitPrice);
                return new LineItem
                {
                    ProductId = li.SelectedProduct?.Id,
                    Description = li.SelectedProduct?.Name ?? li.Description,
                    Quantity = qty > 0 ? qty : 1,
                    UnitPrice = unitPrice
                };
            }).Where(li => !string.IsNullOrWhiteSpace(li.Description) || li.ProductId != null).ToList();

            companyData.IdCounters.Receipt++;
            var receiptId = $"RCP-{DateTime.Now:yyyy}-{companyData.IdCounters.Receipt:D5}";

            string? fileData = null;
            if (item.PreprocessedData != null)
            {
                var imageBytes = item.PreprocessedData;
                fileData = await Task.Run(() =>
                {
                    var orientedData = ReceiptImageHelper.FixOrientation(imageBytes);
                    return Convert.ToBase64String(orientedData);
                });
            }

            var receipt = new Receipt
            {
                Id = receiptId,
                TransactionType = IsRevenue ? "Revenue" : "Expense",
                FileName = item.ScanFileName,
                FileType = GetMimeType(item.ScanFileName),
                FileSize = item.PreprocessedData?.Length ?? 0,
                FileData = fileData,
                Amount = total,
                Date = ExtractedDate?.DateTime ?? DateTime.Now,
                Supplier = ExtractedSupplier,
                Source = "AI Scanned",
                OcrData = CreateOcrData(),
                CreatedAt = DateTime.Now
            };

            if (IsRevenue)
            {
                companyData.IdCounters.Revenue++;
                var revenueId = $"REV-{DateTime.Now:yyyy}-{companyData.IdCounters.Revenue:D5}";

                var revenue = new Revenue
                {
                    Id = revenueId,
                    Date = ExtractedDate?.DateTime ?? DateTime.Now,
                    CustomerId = null,
                    Description = lineItems.Count > 0 ? lineItems[0].Description : ExtractedSupplier,
                    LineItems = lineItems,
                    Quantity = lineItems.Sum(li => li.Quantity),
                    UnitPrice = lineItems.Count > 0 ? lineItems.Average(li => li.UnitPrice) : subtotal,
                    Amount = subtotal > 0 ? subtotal : total,
                    Subtotal = subtotal > 0 ? subtotal : total,
                    TaxRate = subtotal > 0 && taxAmount > 0 ? (taxAmount / subtotal) * 100 : 0,
                    TaxAmount = taxAmount,
                    Discount = discount,
                    Total = total,
                    PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var rpm) ? rpm : PaymentMethod.Cash,
                    PaymentStatus = "Paid",
                    Notes = Notes,
                    ReceiptId = receiptId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                receipt.TransactionId = revenueId;
                companyData.Revenues.Add(revenue);
                createdRevenues.Add(revenue);
            }
            else
            {
                companyData.IdCounters.Expense++;
                var expenseId = $"PUR-{DateTime.Now:yyyy}-{companyData.IdCounters.Expense:D5}";

                var expense = new Expense
                {
                    Id = expenseId,
                    Date = ExtractedDate?.DateTime ?? DateTime.Now,
                    SupplierId = SelectedSupplier?.Id,
                    Description = lineItems.Count > 0 ? lineItems[0].Description : ExtractedSupplier,
                    LineItems = lineItems,
                    Quantity = lineItems.Sum(li => li.Quantity),
                    UnitPrice = lineItems.Count > 0 ? lineItems.Average(li => li.UnitPrice) : subtotal,
                    Amount = subtotal > 0 ? subtotal : total,
                    TaxRate = subtotal > 0 && taxAmount > 0 ? (taxAmount / subtotal) * 100 : 0,
                    TaxAmount = taxAmount,
                    Discount = discount,
                    Total = total,
                    PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var epm) ? epm : PaymentMethod.Cash,
                    Notes = Notes,
                    ReceiptId = receiptId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                receipt.TransactionId = expenseId;
                companyData.Expenses.Add(expense);
                createdExpenses.Add(expense);
            }

            companyData.Receipts.Add(receipt);
            createdReceipts.Add(receipt);

            if (_createdSupplierForUndo != null)
                createdSuppliers.Add(_createdSupplierForUndo);
            if (_createdCategoryForUndo != null)
                createdCategories.Add(_createdCategoryForUndo);
            createdProducts.AddRange(_createdProductsForUndo);
        }

        var action = new DelegateAction(
            $"Bulk scan {approvedItems.Count} receipts",
            () =>
            {
                foreach (var e in createdExpenses) companyData.Expenses.Remove(e);
                foreach (var r in createdRevenues) companyData.Revenues.Remove(r);
                foreach (var r in createdReceipts) companyData.Receipts.Remove(r);
                foreach (var p in createdProducts) companyData.Products?.Remove(p);
                foreach (var c in createdCategories) companyData.Categories.Remove(c);
                foreach (var s in createdSuppliers) companyData.Suppliers.Remove(s);
                ReceiptScanned?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                foreach (var s in createdSuppliers) companyData.Suppliers.Add(s);
                foreach (var c in createdCategories) companyData.Categories.Add(c);
                foreach (var p in createdProducts) companyData.Products?.Add(p);
                foreach (var e in createdExpenses) companyData.Expenses.Add(e);
                foreach (var r in createdRevenues) companyData.Revenues.Add(r);
                foreach (var r in createdReceipts) companyData.Receipts.Add(r);
                ReceiptScanned?.Invoke(this, EventArgs.Empty);
            });

        App.UndoRedoManager.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();
        ReceiptScanned?.Invoke(this, EventArgs.Empty);
        CloseBulkReview();
    }

    [RelayCommand]
    private void CloseBulkReview()
    {
        IsBulkReviewOpen = false;
        IsBulkScanComplete = false;
        IsBulkScanning = false;
        BulkItems.Clear();
        CurrentBulkItem = null;
        ResetScanModal();
    }

    #endregion

    #region AI Scan Commands

    /// <summary>
    /// Checks whether the user can scan a receipt. If the limit is reached,
    /// shows the upgrade prompt and returns false.
    /// Call this before opening a file picker.
    /// </summary>
    public async Task<bool> CanScanOrShowLimitAsync()
    {
        _usageService ??= CreateUsageService();
        var usageCheck = await _usageService.CheckUsageAsync();
        if (!usageCheck.CanScan)
        {
            await UpgradePromptHelper.ShowReceiptScanLimitPromptAsync(
                usageCheck.ScanCount,
                usageCheck.MonthlyLimit,
                usageCheck.ResetsAt);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Opens the scan review modal and starts scanning.
    /// </summary>
    public async Task OpenScanModalAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            await (App.ConfirmationDialog?.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Error".Translate(),
                Message = "File not found.".Translate(),
                PrimaryButtonText = "OK".Translate(),
                CancelButtonText = null
            }) ?? Task.CompletedTask);
            return;
        }

        // Show modal immediately in loading state before reading the file
        ResetScanModal();
        IsScanReviewModalOpen = true;
        IsScanning = true;
        HasScanError = false;
        HasScanResult = false;

        try
        {
            _currentImageData = await File.ReadAllBytesAsync(filePath);
            _currentFileName = Path.GetFileName(filePath);
            await OpenScanModalWithDataAsync(_currentImageData, _currentFileName, filePath);
        }
        catch (Exception ex)
        {
            IsScanning = false;
            HasScanError = true;
            ScanErrorMessage = "Failed to read file: {0}".TranslateFormat(ex.Message);
        }
    }

    /// <summary>
    /// Opens the scan review modal with image data and starts scanning.
    /// </summary>
    public async Task OpenScanModalWithDataAsync(byte[] imageData, string fileName, string? tempFilePath = null)
    {
        // Reset state first, before setting new data
        ResetScanModal();

        _currentImageData = imageData;
        _currentFileName = fileName;

        // Show modal immediately with loading state
        IsScanReviewModalOpen = true;
        IsScanning = true;
        HasScanError = false;
        HasScanResult = false;

        // Yield to let the UI render the modal and start the spinner before doing sync work
        await Task.Delay(1);

        LoadSupplierOptions();
        LoadProductOptions();

        // Preprocess the image once on a background thread (EXIF fix + contrast + sharpen).
        // The result is used for both the preview image and the API call, avoiding
        // a redundant FixOrientation decode/encode cycle. PDFs are returned unchanged.
        var isPdf = Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        var preprocessedData = await Task.Run(() =>
            ReceiptImageHelper.PreprocessForOcr(imageData, fileName));
        _currentImageData = preprocessedData;
        _currentFileName = isPdf ? fileName : Path.ChangeExtension(fileName, ".jpg");

        // Write preview image to disk off the UI thread
        _ = Task.Run(async () =>
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ArgoBooks", "ScanPreview");
            Directory.CreateDirectory(tempDir);
            var previewPath = Path.Combine(tempDir, Path.ChangeExtension(fileName, ".jpg"));

            // For PDFs, render the first page as JPEG for preview
            #pragma warning disable CA1416 // RenderPdfFirstPage uses PDFium which supports Windows/macOS/Linux (not browser)
            var previewBytes = isPdf
                ? ReceiptImageHelper.RenderPdfFirstPage(preprocessedData)
                : preprocessedData;
            #pragma warning restore CA1416

            if (previewBytes != null)
            {
                await File.WriteAllBytesAsync(previewPath, previewBytes);
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ReceiptImagePath = previewPath);
            }
        });

        // Start scanning (image is already preprocessed)
        await ScanReceiptAsync();
    }

    private async Task ScanReceiptAsync()
    {
        try
        {
            // Get or create scanner service
            _scannerService ??= CreateScannerService();
            _usageService ??= CreateUsageService();

            if (_scannerService == null || !_scannerService.IsConfigured)
            {
                HasScanError = true;
                ScanErrorMessage = "AI Receipt Scanning requires an active premium license.".Translate();
                IsScanning = false;
                return;
            }

            if (_currentImageData == null || _currentFileName == null)
            {
                HasScanError = true;
                ScanErrorMessage = "No image data to scan.".Translate();
                IsScanning = false;
                return;
            }

            // Check usage limit before scanning
            ScanningMessage = "Checking usage limits...".Translate();
            if (_usageService != null)
            {
                var usageCheck = await _usageService.CheckUsageAsync();
                UpdateUsageDisplay(usageCheck);

                if (!usageCheck.CanScan)
                {
                    IsScanning = false;
                    await UpgradePromptHelper.ShowReceiptScanLimitPromptAsync(
                        usageCheck.ScanCount,
                        usageCheck.MonthlyLimit,
                        usageCheck.ResetsAt);
                    return;
                }
            }

            ScanningMessage = "Analyzing receipt with AI...".Translate();

            // Run API call off the UI thread to keep the spinner smooth.
            // Image is already preprocessed in OpenScanModalWithDataAsync, so skip it here.
            var imageData = _currentImageData;
            var fileName = _currentFileName;
            var result = await Task.Run(() => _scannerService.ScanReceiptAsync(imageData, fileName, skipPreprocessing: true));

            if (!result.IsSuccess)
            {
                HasScanError = true;
                ScanErrorMessage = result.ErrorMessage ?? "Unknown error occurred.".Translate();
                IsScanning = false;
                return;
            }

            // Increment usage after successful scan
            if (_usageService != null)
            {
                var incrementResult = await _usageService.IncrementUsageAsync();
                if (incrementResult.Success)
                {
                    ScansUsed = incrementResult.ScanCount;
                    ScansRemaining = incrementResult.Remaining;
                    IsNearLimit = incrementResult.MonthlyLimit > 0 && incrementResult.Remaining > 0 && incrementResult.Remaining <= incrementResult.MonthlyLimit / 10;
                }
            }

            // Populate fields with extracted data
            PopulateScanResults(result);

            HasScanResult = true;
            IsScanning = false;
        }
        catch (Exception ex)
        {
            HasScanError = true;
            ScanErrorMessage = "Failed to scan receipt: {0}".TranslateFormat(ex.Message);
            IsScanning = false;
        }
    }

    private void UpdateUsageDisplay(UsageCheckResult usageCheck)
    {
        HasUsageInfo = true;
        ScansUsed = usageCheck.ScanCount;
        ScansLimit = usageCheck.MonthlyLimit;
        ScansRemaining = usageCheck.Remaining;
        UsageTier = usageCheck.Tier;
        ResetsAt = usageCheck.ResetsAt;
        IsNearLimit = usageCheck.MonthlyLimit > 0 && usageCheck.Remaining > 0 && usageCheck.Remaining <= usageCheck.MonthlyLimit / 10;
    }

    private async void PopulateScanResults(ReceiptScanResult result)
    {
        try
        {
            ExtractedSupplier = result.SupplierName ?? string.Empty;
            ExtractedDate = result.TransactionDate.HasValue
                ? new DateTimeOffset(result.TransactionDate.Value)
                : DateTimeOffset.Now;
            ExtractedSubtotal = result.Subtotal?.ToString("F2") ?? "0.00";
            ExtractedTax = result.TaxAmount?.ToString("F2") ?? "0.00";
            ExtractedTotal = result.TotalAmount?.ToString("F2") ?? "0.00";

            // Detect if this is a revenue or expense based on merchant name matching company name
            DetectTransactionType(result.SupplierName);

            // Set payment method if detected
            if (!string.IsNullOrEmpty(result.PaymentMethod) && PaymentMethodOptions.Contains(result.PaymentMethod))
            {
                SelectedPaymentMethod = result.PaymentMethod;
            }

            // Confidence
            ConfidenceScore = result.Confidence;
            ConfidenceText = $"{result.Confidence:P0}";
            IsHighConfidence = result.Confidence >= 0.85;
            IsMediumConfidence = result.Confidence >= 0.6 && result.Confidence < 0.85;
            IsLowConfidence = result.Confidence < 0.6;

            // Use discount from scanner if available, fall back to line-item heuristic
            var discountTotal = result.Discount ?? result.LineItems
                .Where(IsDiscountLine)
                .Sum(item => Math.Abs(item.TotalPrice));
            ExtractedDiscount = discountTotal > 0 ? discountTotal.ToString("F2") : "0.00";

            // Line items (filter out discounts and non-product lines)
            LineItems.Clear();
            foreach (var item in result.LineItems)
            {
                // Skip discount lines, negative-amount adjustments, and $0 items
                if (IsDiscountLine(item) || item.TotalPrice == 0)
                    continue;

                var lineItem = new ScannedLineItemViewModel
                {
                    Description = CleanOcrText(item.Description),
                    Quantity = ((int)item.Quantity).ToString(),
                    UnitPrice = item.UnitPrice.ToString("F2"),
                    TotalPrice = item.TotalPrice.ToString("F2"),
                    Confidence = item.Confidence
                };

                // Try to match to existing product
                TryMatchProduct(lineItem, item.Description);

                lineItem.OnProductErrorCleared = ClearValidationMessageIfNoErrors;
                lineItem.OnTotalPriceEdited = ValidateTotals;
                LineItems.Add(lineItem);
            }

            UpdateHasUnmatchedProducts();
            ValidateTotals();

            // Fire AI suggestions in the background — don't block showing scan results
            _ = GetAiSuggestionsAsync(result);
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.FileSystem, "PopulateScanResults");
        }
    }

    [RelayCommand]
    private void CloseScanReviewModal()
    {
        IsScanReviewModalOpen = false;
        IsFullscreen = false;
        ResetScanModal();
    }

    [RelayCommand]
    private void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
    }

    /// <summary>
    /// Requests to close the scan review modal, showing confirmation if scanning or data is present.
    /// </summary>
    [RelayCommand]
    private async Task RequestCloseScanReviewModalAsync()
    {
        if (HasScanResult || IsScanning)
        {
            if (!await ConfirmDiscardNewAsync()) return;
        }

        CloseScanReviewModal();
    }

    [RelayCommand]
    private async Task RetryScan()
    {
        if (_currentImageData != null && _currentFileName != null)
        {
            // Clear error/result state first to avoid UI overlap
            HasScanError = false;
            HasScanResult = false;

            // Then show scanning state
            IsScanning = true;
            ScanningMessage = "Retrying...".Translate();

            // Start the scan
            await ScanReceiptAsync();
        }
    }

    [RelayCommand]
    private void AddLineItem()
    {
        LineItems.Add(new ScannedLineItemViewModel
        {
            Description = string.Empty,
            Quantity = "1",
            UnitPrice = "0.00",
            TotalPrice = "0.00",
            Confidence = 1.0,
            IsManuallyAdded = true,
            OnProductErrorCleared = ClearValidationMessageIfNoErrors,
            OnTotalPriceEdited = ValidateTotals
        });
    }

    [RelayCommand]
    private void RemoveLineItem(ScannedLineItemViewModel? item)
    {
        if (item != null)
        {
            LineItems.Remove(item);
            ValidateTotals();
        }
    }

    [RelayCommand]
    private async Task CreateTransactionAsync()
    {
        // Validate
        HasValidationMessage = false;
        HasTotalError = false;
        HasSupplierError = false;
        HasLineItemsError = false;

        // Clear product errors on all line items
        foreach (var lineItem in LineItems)
        {
            lineItem.HasProductError = false;
            lineItem.ProductErrorMessage = string.Empty;
        }

        var hasErrors = false;

        if (!decimal.TryParse(ExtractedTotal, out var total) || total <= 0)
        {
            HasTotalError = true;
            TotalErrorMessage = "Please enter a valid total amount.".Translate();
            hasErrors = true;
        }

        // Supplier is required for expenses, optional for revenue
        if (!IsRevenue && SelectedSupplier == null)
        {
            HasSupplierError = true;
            SupplierErrorMessage = "Please select a supplier.".Translate();
            hasErrors = true;
        }

        // At least one line item is required
        if (LineItems.Count == 0)
        {
            HasLineItemsError = true;
            LineItemsErrorMessage = "Please add at least one line item.".Translate();
            hasErrors = true;
        }

        // Validate line items have products selected
        foreach (var lineItem in LineItems)
        {
            if (lineItem.SelectedProduct == null)
            {
                lineItem.HasProductError = true;
                lineItem.ProductErrorMessage = "Please select a product.".Translate();
                hasErrors = true;
            }
        }

        HasValidationMessage = hasErrors;
        if (hasErrors)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
        {
            await (App.ConfirmationDialog?.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Error".Translate(),
                Message = "No company is open.".Translate(),
                PrimaryButtonText = "OK".Translate(),
                CancelButtonText = null
            }) ?? Task.CompletedTask);
            return;
        }

        // Parse values
        decimal.TryParse(ExtractedSubtotal, out var subtotal);
        decimal.TryParse(ExtractedTax, out var taxAmount);
        decimal.TryParse(ExtractedDiscount, out var discount);

        // Create line items
        var lineItems = LineItems.Select(li =>
        {
            decimal.TryParse(li.Quantity, out var qty);
            decimal.TryParse(li.UnitPrice, out var unitPrice);
            return new LineItem
            {
                ProductId = li.SelectedProduct?.Id,
                Description = li.SelectedProduct?.Name ?? li.Description,
                Quantity = qty > 0 ? qty : 1,
                UnitPrice = unitPrice
            };
        }).Where(li => !string.IsNullOrWhiteSpace(li.Description) || li.ProductId != null).ToList();

        // Create receipt first (common for both transaction types)
        companyData.IdCounters.Receipt++;
        var receiptId = $"RCP-{DateTime.Now:yyyy}-{companyData.IdCounters.Receipt:D5}";

        string? fileData = null;
        if (_currentImageData != null)
        {
            var imageBytes = _currentImageData;
            fileData = await Task.Run(() =>
            {
                var orientedData = ReceiptImageHelper.FixOrientation(imageBytes);
                return Convert.ToBase64String(orientedData);
            });
        }

        if (IsRevenue)
        {
            // Create revenue transaction
            CreateRevenueTransaction(companyData, receiptId, fileData, total, subtotal, taxAmount, discount, lineItems);
        }
        else
        {
            // Create expense transaction
            CreateExpenseTransaction(companyData, receiptId, fileData, total, subtotal, taxAmount, discount, lineItems);
        }

        App.CompanyManager?.MarkAsChanged();
        ReceiptScanned?.Invoke(this, EventArgs.Empty);
        CloseScanReviewModal();
    }

    private void CreateExpenseTransaction(CompanyData companyData, string receiptId, string? fileData,
        decimal total, decimal subtotal, decimal taxAmount, decimal discount, List<LineItem> lineItems)
    {
        companyData.IdCounters.Expense++;
        var expenseId = $"PUR-{DateTime.Now:yyyy}-{companyData.IdCounters.Expense:D5}";

        var expense = new Expense
        {
            Id = expenseId,
            Date = ExtractedDate?.DateTime ?? DateTime.Now,
            SupplierId = SelectedSupplier?.Id,
            Description = lineItems.Count > 0 ? lineItems[0].Description : ExtractedSupplier,
            LineItems = lineItems,
            Quantity = lineItems.Sum(li => li.Quantity),
            UnitPrice = lineItems.Count > 0 ? lineItems.Average(li => li.UnitPrice) : subtotal,
            Amount = subtotal > 0 ? subtotal : total,
            TaxRate = subtotal > 0 && taxAmount > 0 ? (taxAmount / subtotal) * 100 : 0,
            TaxAmount = taxAmount,
            Discount = discount,
            Total = total,
            PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash,
            Notes = Notes,
            ReceiptId = receiptId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var receipt = new Receipt
        {
            Id = receiptId,
            TransactionId = expenseId,
            TransactionType = "Expense",
            FileName = _currentFileName ?? "receipt.jpg",
            FileType = GetMimeType(_currentFileName ?? "receipt.jpg"),
            FileSize = _currentImageData?.Length ?? 0,
            FileData = fileData,
            Amount = total,
            Date = ExtractedDate?.DateTime ?? DateTime.Now,
            Supplier = ExtractedSupplier,
            Source = "AI Scanned",
            OcrData = CreateOcrData(),
            CreatedAt = DateTime.Now
        };

        // Capture auto-created entities for undo
        var capturedReceipt = receipt;
        var capturedExpense = expense;
        var capturedSupplier = _createdSupplierForUndo;
        var capturedCategory = _createdCategoryForUndo;
        var capturedProducts = _createdProductsForUndo.ToList();

        var action = new DelegateAction(
            $"AI scan expense {expenseId}",
            () =>
            {
                companyData.Expenses.Remove(capturedExpense);
                companyData.Receipts.Remove(capturedReceipt);

                // Also undo auto-created entities
                foreach (var product in capturedProducts)
                    companyData.Products?.Remove(product);
                if (capturedCategory != null)
                    companyData.Categories.Remove(capturedCategory);
                if (capturedSupplier != null)
                    companyData.Suppliers.Remove(capturedSupplier);

                ReceiptScanned?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                // Re-add auto-created entities
                if (capturedSupplier != null)
                    companyData.Suppliers.Add(capturedSupplier);
                if (capturedCategory != null)
                    companyData.Categories.Add(capturedCategory);
                foreach (var product in capturedProducts)
                    companyData.Products?.Add(product);

                companyData.Expenses.Add(capturedExpense);
                companyData.Receipts.Add(capturedReceipt);
                ReceiptScanned?.Invoke(this, EventArgs.Empty);
            });

        companyData.Expenses.Add(expense);
        companyData.Receipts.Add(receipt);
        App.UndoRedoManager.RecordAction(action);
    }

    private void CreateRevenueTransaction(CompanyData companyData, string receiptId, string? fileData,
        decimal total, decimal subtotal, decimal taxAmount, decimal discount, List<LineItem> lineItems)
    {
        companyData.IdCounters.Revenue++;
        var revenueId = $"REV-{DateTime.Now:yyyy}-{companyData.IdCounters.Revenue:D5}";

        var revenue = new Revenue
        {
            Id = revenueId,
            Date = ExtractedDate?.DateTime ?? DateTime.Now,
            CustomerId = null, // Could be linked to customer if we add customer selection later
            Description = lineItems.Count > 0 ? lineItems[0].Description : ExtractedSupplier,
            LineItems = lineItems,
            Quantity = lineItems.Sum(li => li.Quantity),
            UnitPrice = lineItems.Count > 0 ? lineItems.Average(li => li.UnitPrice) : subtotal,
            Amount = subtotal > 0 ? subtotal : total,
            Subtotal = subtotal > 0 ? subtotal : total,
            TaxRate = subtotal > 0 && taxAmount > 0 ? (taxAmount / subtotal) * 100 : 0,
            TaxAmount = taxAmount,
            Discount = discount,
            Total = total,
            PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash,
            PaymentStatus = "Paid",
            Notes = Notes,
            ReceiptId = receiptId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var receipt = new Receipt
        {
            Id = receiptId,
            TransactionId = revenueId,
            TransactionType = "Revenue",
            FileName = _currentFileName ?? "receipt.jpg",
            FileType = GetMimeType(_currentFileName ?? "receipt.jpg"),
            FileSize = _currentImageData?.Length ?? 0,
            FileData = fileData,
            Amount = total,
            Date = ExtractedDate?.DateTime ?? DateTime.Now,
            Supplier = ExtractedSupplier, // Still store the merchant name for reference
            Source = "AI Scanned",
            OcrData = CreateOcrData(),
            CreatedAt = DateTime.Now
        };

        // Capture auto-created entities for undo
        var capturedReceipt = receipt;
        var capturedRevenue = revenue;
        var capturedSupplier = _createdSupplierForUndo;
        var capturedCategory = _createdCategoryForUndo;
        var capturedProducts = _createdProductsForUndo.ToList();

        var action = new DelegateAction(
            $"AI scan revenue {revenueId}",
            () =>
            {
                companyData.Revenues.Remove(capturedRevenue);
                companyData.Receipts.Remove(capturedReceipt);

                // Also undo auto-created entities
                foreach (var product in capturedProducts)
                    companyData.Products?.Remove(product);
                if (capturedCategory != null)
                    companyData.Categories.Remove(capturedCategory);
                if (capturedSupplier != null)
                    companyData.Suppliers.Remove(capturedSupplier);

                ReceiptScanned?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                // Re-add auto-created entities
                if (capturedSupplier != null)
                    companyData.Suppliers.Add(capturedSupplier);
                if (capturedCategory != null)
                    companyData.Categories.Add(capturedCategory);
                foreach (var product in capturedProducts)
                    companyData.Products?.Add(product);

                companyData.Revenues.Add(capturedRevenue);
                companyData.Receipts.Add(capturedReceipt);
                ReceiptScanned?.Invoke(this, EventArgs.Empty);
            });

        companyData.Revenues.Add(revenue);
        companyData.Receipts.Add(receipt);
        App.UndoRedoManager.RecordAction(action);
    }

    [RelayCommand]
    private void OpenCreateSupplier()
    {
        var supplierModals = App.SupplierModalsViewModel;
        if (supplierModals == null) return;

        void OnSaved(object? s, EventArgs e)
        {
            supplierModals.SupplierSaved -= OnSaved;
            LoadSupplierOptions();
        }
        supplierModals.SupplierSaved += OnSaved;
        supplierModals.OpenAddModal();
    }

    [RelayCommand]
    private void OpenCreateProduct()
    {
        var productModals = App.ProductModalsViewModel;
        if (productModals == null) return;

        void OnSaved(object? s, EventArgs e)
        {
            productModals.ProductSaved -= OnSaved;
            LoadProductOptions();
        }
        productModals.ProductSaved += OnSaved;
        productModals.OpenAddModal();
    }

    /// <summary>
    /// Creates a new product from a line item suggestion.
    /// </summary>
    [RelayCommand]
    private void CreateSuggestedProduct(ScannedLineItemViewModel? lineItem)
    {
        CreateSuggestedProductCore(lineItem);
        UpdateHasUnmatchedProducts();
    }

    private void CreateSuggestedProductCore(ScannedLineItemViewModel? lineItem)
    {
        if (lineItem == null || string.IsNullOrEmpty(lineItem.SuggestedProductName))
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Products == null) return;

        // Find or create an expense category for the product.
        // Prefer the AI-suggested category, then fall back to any existing expense category.
        var category = _createdCategoryForUndo
            ?? companyData.Categories.FirstOrDefault(c => c.Type == CategoryType.Expense);

        if (category == null)
        {
            // No expense category exists — create one from AI suggestion if available
            var aiCategory = _aiSuggestion?.NewCategory;
            var categoryName = aiCategory?.Name ?? "General Expenses";

            companyData.IdCounters.Category++;
            category = new Category
            {
                Id = $"CAT-PUR-{companyData.IdCounters.Category:D3}",
                Name = categoryName,
                Type = CategoryType.Expense,
                ItemType = aiCategory?.ItemType ?? "Product",
                Description = aiCategory?.Description
            };
            companyData.Categories.Add(category);
            _createdCategoryForUndo = category;
        }

        // Generate proper product ID
        companyData.IdCounters.Product++;
        var newId = $"PRD-{companyData.IdCounters.Product:D3}";

        var newProduct = new Product
        {
            Id = newId,
            Name = lineItem.SuggestedProductName,
            Description = string.Empty,
            CostPrice = decimal.TryParse(lineItem.UnitPrice, out var price) ? price : 0,
            UnitPrice = 0,
            CategoryId = category.Id,
            Type = CategoryType.Expense
        };

        companyData.Products.Add(newProduct);
        _createdProductsForUndo.Add(newProduct);

        // Add to options and select
        var option = new ProductOption
        {
            Id = newId,
            Name = newProduct.Name,
            Description = string.Empty,
            UnitPrice = newProduct.CostPrice
        };
        ProductOptions.Add(option);
        lineItem.SelectedProduct = option;
        lineItem.ShowCreateProductSuggestion = false;

        companyData.MarkAsModified();
    }

    /// <summary>
    /// Creates products for all unmatched line items at once.
    /// </summary>
    [RelayCommand]
    private void CreateAllSuggestedProducts()
    {
        var unmatched = LineItems.Where(li => li.ShowCreateProductSuggestion).ToList();
        foreach (var lineItem in unmatched)
        {
            CreateSuggestedProductCore(lineItem);
        }
        UpdateHasUnmatchedProducts();
    }

    /// <summary>
    /// Dismisses the create product suggestion for a line item.
    /// </summary>
    [RelayCommand]
    private void DismissProductSuggestion(ScannedLineItemViewModel? lineItem)
    {
        if (lineItem != null)
        {
            lineItem.ShowCreateProductSuggestion = false;
            UpdateHasUnmatchedProducts();
        }
    }

    private void UpdateHasUnmatchedProducts()
    {
        HasUnmatchedProducts = LineItems.Any(li => li.ShowCreateProductSuggestion);
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        // Close modal and open settings
        CloseScanReviewModal();
        // The settings modal should be opened from the header
    }

    /// <summary>
    /// Gets AI suggestions for supplier and category based on receipt data.
    /// </summary>
    private async Task GetAiSuggestionsAsync(ReceiptScanResult result)
    {
        var geminiService = new GeminiService(App.ErrorLogger, App.TelemetryManager);

        if (!geminiService.IsConfigured)
        {
            // Fall back to basic matching
            TryBasicSupplierMatch(result.SupplierName);
            return;
        }

        IsLoadingAiSuggestions = true;
        ShowCreateSupplierSuggestion = false;

        try
        {
            var companyData = App.CompanyManager?.CompanyData;
            if (companyData == null)
            {
                TryBasicSupplierMatch(result.SupplierName);
                return;
            }

            var request = new ReceiptAnalysisRequest
            {
                SupplierName = result.SupplierName ?? string.Empty,
                RawText = result.RawText,
                LineItemDescriptions = result.LineItems.Select(li => li.Description).ToList(),
                TotalAmount = result.TotalAmount ?? 0,
                ExistingSuppliers = companyData.Suppliers.Select(s => new ExistingSupplierInfo
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList(),
                ExistingCategories = companyData.Categories
                    .Where(c => c.Type == CategoryType.Expense)
                    .Select(c => new ExistingCategoryInfo
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description
                    }).ToList()
            };

            var suggestion = await geminiService.GetSupplierCategorySuggestionAsync(request);

            if (suggestion != null)
            {
                ApplyAiSuggestion(suggestion);
            }
            else
            {
                // AI failed, fall back to basic matching
                TryBasicSupplierMatch(result.SupplierName);
            }
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.Api, "AI suggestion failed");
            TryBasicSupplierMatch(result.SupplierName);
        }
        finally
        {
            IsLoadingAiSuggestions = false;
        }
    }

    /// <summary>
    /// Applies AI suggestions to the form.
    /// </summary>
    private void ApplyAiSuggestion(SupplierCategorySuggestion suggestion)
    {
        _aiSuggestion = suggestion;

        // Apply supplier suggestion
        if (!string.IsNullOrEmpty(suggestion.MatchedSupplierId))
        {
            var supplier = SupplierOptions.FirstOrDefault(s => s.Id == suggestion.MatchedSupplierId);
            if (supplier != null)
            {
                SelectedSupplier = supplier;
            }
        }
        else if (suggestion.ShouldCreateNewSupplier && suggestion.NewSupplier != null)
        {
            ShowCreateSupplierSuggestion = true;
            SuggestedSupplierName = ToTitleCase(suggestion.NewSupplier.Name);
        }
    }

    private void ValidateTotals()
    {
        HasTotalMismatchWarning = false;
        TotalMismatchWarningMessage = string.Empty;

        decimal.TryParse(ExtractedTotal, out var total);
        if (total == 0) return;

        decimal.TryParse(ExtractedSubtotal, out var subtotal);
        decimal.TryParse(ExtractedTax, out var tax);
        decimal.TryParse(ExtractedDiscount, out var discount);

        var lineItemSum = LineItems
            .Sum(li => decimal.TryParse(li.TotalPrice, out var p) ? p : 0);

        // Check line items + tax - discount against total
        var expectedTotal = lineItemSum + tax - discount;
        var diff = Math.Abs(expectedTotal - total);

        if (diff > 0.02m)
        {
            HasTotalMismatchWarning = true;
            TotalMismatchWarningMessage = string.Format(
                "Line items ({0:C}) + tax ({1:C}) - discount ({2:C}) = {3:C}, but total is {4:C}. Some items may be incorrect.".Translate(),
                lineItemSum, tax, discount, expectedTotal, total);
        }
    }

    /// <summary>
    /// Determines if a line item is a discount rather than a product.
    /// Only filters based on description keywords to avoid incorrectly
    /// removing legitimate line items with negative amounts.
    /// </summary>
    private static bool IsDiscountLine(ScannedLineItem item)
    {
        var desc = item.Description.ToLowerInvariant();
        return desc.Contains("discount") || desc.Contains("% off") || desc.Contains("coupon")
            || desc.Contains("promo");
    }

    /// <summary>
    /// Cleans OCR artifacts from a description string.
    /// Removes *, normalizes newlines to spaces, and collapses multiple spaces.
    /// </summary>
    private static string CleanOcrText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var cleaned = text
            .Replace("*", "")
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ");

        // Collapse multiple spaces into one
        while (cleaned.Contains("  "))
            cleaned = cleaned.Replace("  ", " ");

        return cleaned.Trim();
    }

    /// <summary>
    /// Converts a string to title case and cleans up OCR artifacts
    /// (e.g., "LARGE DRINK*" -> "Large Drink", "MD HOOK\nwheat" -> "Md Hook Wheat").
    /// </summary>
    private static string ToTitleCase(string text)
    {
        var cleaned = CleanOcrText(text);
        if (string.IsNullOrEmpty(cleaned))
            return text.Trim();

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleaned.ToLower());
    }

    /// <summary>
    /// Detects whether the transaction should be revenue or expense based on
    /// comparing the extracted merchant name against the user's company name.
    /// If the merchant matches (or closely matches) the company name → Revenue.
    /// If it doesn't match → Expense.
    /// </summary>
    private void DetectTransactionType(string? merchantName)
    {
        var companyData = App.CompanyManager?.CompanyData;
        var companyName = companyData?.Settings.Company.Name;

        if (string.IsNullOrWhiteSpace(merchantName) || string.IsNullOrWhiteSpace(companyName))
        {
            // Default to expense if we can't compare
            IsRevenue = false;
            return;
        }

        // Normalize both strings for comparison
        var normalizedMerchant = NormalizeForComparison(merchantName);
        var normalizedCompany = NormalizeForComparison(companyName);

        // Check for exact match
        if (normalizedMerchant.Equals(normalizedCompany, StringComparison.OrdinalIgnoreCase))
        {
            IsRevenue = true;
            return;
        }

        // Check if one contains the other (handles cases like "ACME Inc" vs "ACME")
        if (normalizedMerchant.Contains(normalizedCompany, StringComparison.OrdinalIgnoreCase) ||
            normalizedCompany.Contains(normalizedMerchant, StringComparison.OrdinalIgnoreCase))
        {
            IsRevenue = true;
            return;
        }

        // Check word-level similarity (handles word order differences)
        var merchantWords = normalizedMerchant.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var companyWords = normalizedCompany.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // If most significant words match, consider it a match
        var matchingWords = merchantWords.Count(mw =>
            companyWords.Any(cw => mw.Equals(cw, StringComparison.OrdinalIgnoreCase) ||
                                   (mw.Length > 3 && cw.Length > 3 &&
                                    (mw.Contains(cw, StringComparison.OrdinalIgnoreCase) ||
                                     cw.Contains(mw, StringComparison.OrdinalIgnoreCase)))));

        var totalWords = Math.Max(merchantWords.Length, companyWords.Length);
        if (totalWords > 0 && (double)matchingWords / totalWords >= 0.5)
        {
            IsRevenue = true;
            return;
        }

        // Default to expense if no match found
        IsRevenue = false;
    }

    /// <summary>
    /// Normalizes a string for comparison by removing common business suffixes,
    /// punctuation, and extra whitespace.
    /// </summary>
    private static string NormalizeForComparison(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Convert to lowercase
        var normalized = text.ToLowerInvariant();

        // Remove common business suffixes
        string[] suffixes = ["inc", "inc.", "llc", "llc.", "ltd", "ltd.", "corp", "corp.",
                            "corporation", "company", "co", "co.", "limited", "gmbh",
                            "plc", "pty", "pvt", "s.a.", "sa", "ag"];

        foreach (var suffix in suffixes)
        {
            if (normalized.EndsWith(" " + suffix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[..^(suffix.Length + 1)];
            }
        }

        // Remove punctuation and extra whitespace
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^\w\s]", " ");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");

        return normalized.Trim();
    }

    /// <summary>
    /// Falls back to basic string matching for supplier.
    /// </summary>
    private void TryBasicSupplierMatch(string? supplierName)
    {
        if (string.IsNullOrEmpty(supplierName))
            return;

        var matchedSupplier = SupplierOptions.FirstOrDefault(s =>
            s.Name.Contains(supplierName, StringComparison.OrdinalIgnoreCase) ||
            supplierName.Contains(s.Name, StringComparison.OrdinalIgnoreCase));

        if (matchedSupplier != null)
        {
            SelectedSupplier = matchedSupplier;
        }
        else
        {
            // No match found — suggest creating a new supplier
            ShowCreateSupplierSuggestion = true;
            SuggestedSupplierName = ToTitleCase(supplierName);
        }
    }

    /// <summary>
    /// Tries to match a scanned line item description to an existing product.
    /// </summary>
    private void TryMatchProduct(ScannedLineItemViewModel lineItem, string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            lineItem.SuggestedProductName = string.Empty;
            lineItem.ShowCreateProductSuggestion = false;
            return;
        }

        // Clean OCR artifacts before matching
        var cleanedDescription = CleanOcrText(description);

        // Try to find exact or close match
        var matchedProduct = ProductOptions.FirstOrDefault(p =>
            p.Name.Equals(cleanedDescription, StringComparison.OrdinalIgnoreCase));

        if (matchedProduct == null)
        {
            // Try partial match
            matchedProduct = ProductOptions.FirstOrDefault(p =>
                p.Name.Contains(cleanedDescription, StringComparison.OrdinalIgnoreCase) ||
                cleanedDescription.Contains(p.Name, StringComparison.OrdinalIgnoreCase));
        }

        if (matchedProduct != null)
        {
            lineItem.SelectedProduct = matchedProduct;
            lineItem.ShowCreateProductSuggestion = false;
        }
        else
        {
            // No match found - suggest creating new product
            lineItem.SuggestedProductName = ToTitleCase(description);
            lineItem.ShowCreateProductSuggestion = true;
        }
    }

    /// <summary>
    /// Creates a new supplier from the AI suggestion or basic match suggestion.
    /// </summary>
    [RelayCommand]
    private void CreateSuggestedSupplier()
    {
        if (string.IsNullOrEmpty(SuggestedSupplierName))
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        // Generate ID
        companyData.IdCounters.Supplier++;
        var newId = $"SUP-{companyData.IdCounters.Supplier:D4}";

        // Create supplier
        var newSupplier = new Supplier
        {
            Id = newId,
            Name = SuggestedSupplierName,
            Notes = _aiSuggestion?.NewSupplier?.Notes ?? "Created from receipt scan".Translate()
        };

        companyData.Suppliers.Add(newSupplier);
        _createdSupplierForUndo = newSupplier;

        // Add to options and select
        var option = new SupplierOption { Id = newId, Name = newSupplier.Name };
        SupplierOptions.Add(option);
        SelectedSupplier = option;

        ShowCreateSupplierSuggestion = false;

        App.CompanyManager?.MarkAsChanged();
    }

    /// <summary>
    /// Dismisses the supplier suggestion without creating.
    /// </summary>
    [RelayCommand]
    private void DismissSupplierSuggestion()
    {
        ShowCreateSupplierSuggestion = false;
    }

    #endregion

    #region Helper Methods

    private void ResetFilterDefaults()
    {
        FilterType = "All";
        FilterDateFrom = null;
        FilterDateTo = null;
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterSource = "All";
        FilterFileType = "All";
    }

    private void ResetScanModal()
    {
        IsScanning = false;
        HasScanError = false;
        HasScanResult = false;
        IsFullscreen = false;
        ScanErrorMessage = string.Empty;
        ReceiptImagePath = null;
        ExtractedSupplier = string.Empty;
        ExtractedDate = DateTimeOffset.Now;
        ExtractedSubtotal = string.Empty;
        ExtractedTax = string.Empty;
        ExtractedDiscount = string.Empty;
        ExtractedTotal = string.Empty;
        ConfidenceScore = 0;
        ConfidenceText = string.Empty;
        IsHighConfidence = false;
        IsMediumConfidence = false;
        IsLowConfidence = false;
        LineItems.Clear();
        SelectedSupplier = null;
        SelectedPaymentMethod = "Cash";
        Notes = string.Empty;
        IsRevenue = false;
        HasValidationMessage = false;
        HasTotalError = false;
        HasSupplierError = false;
        HasLineItemsError = false;
        LineItemsErrorMessage = string.Empty;
        HasTotalMismatchWarning = false;
        TotalMismatchWarningMessage = string.Empty;
        _currentImageData = null;
        _currentFileName = null;

        // Reset AI suggestion state
        IsLoadingAiSuggestions = false;
        _aiSuggestion = null;
        ShowCreateSupplierSuggestion = false;
        SuggestedSupplierName = string.Empty;

        // Reset undo tracking for auto-created entities
        _createdSupplierForUndo = null;
        _createdCategoryForUndo = null;
        _createdProductsForUndo.Clear();

        // Reset usage state (keep cached values for display)
        IsNearLimit = false;
    }

    private void LoadSupplierOptions()
    {
        SupplierOptions.Clear();
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Suppliers == null) return;

        foreach (var supplier in companyData.Suppliers.OrderBy(s => s.Name))
        {
            SupplierOptions.Add(new SupplierOption { Id = supplier.Id, Name = supplier.Name });
        }
    }

    private void LoadProductOptions()
    {
        ProductOptions.Clear();
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Products == null) return;

        foreach (var product in companyData.Products.OrderBy(p => p.Name))
        {
            // Filter to purchase-type products (for expense receipts)
            var category = companyData.Categories?.FirstOrDefault(c => c.Id == product.CategoryId);
            if (category?.Type == CategoryType.Revenue)
                continue;

            ProductOptions.Add(new ProductOption
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                UnitPrice = product.CostPrice // Use cost price for expenses
            });
        }
    }

    private IReceiptScannerService CreateScannerService()
    {
        return new GeminiReceiptScannerService(App.LicenseService, App.ErrorLogger, App.TelemetryManager);
    }

    private IReceiptUsageService CreateUsageService()
    {
        return new ReceiptUsageService(App.LicenseService);
    }

    private OcrData CreateOcrData()
    {
        decimal.TryParse(ExtractedTotal, out var total);
        decimal.TryParse(ExtractedSubtotal, out var subtotal);
        decimal.TryParse(ExtractedTax, out var tax);

        return new OcrData
        {
            ExtractedSupplier = ExtractedSupplier,
            ExtractedDate = ExtractedDate?.DateTime,
            ExtractedAmount = total,
            ExtractedSubtotal = subtotal,
            ExtractedTaxAmount = tax,
            Confidence = ConfidenceScore,
            LineItems = LineItems.Select(li =>
            {
                decimal.TryParse(li.Quantity, out var qty);
                decimal.TryParse(li.UnitPrice, out var unitPrice);
                decimal.TryParse(li.TotalPrice, out var totalPrice);
                return new OcrLineItem
                {
                    Description = li.Description,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    Confidence = li.Confidence
                };
            }).ToList(),
            ExtractedItems = LineItems.Select(li => li.Description).ToList()
        };
    }

    private static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".bmp" => "image/bmp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    #endregion
}

/// <summary>
/// View model for a scanned line item.
/// </summary>
public partial class ScannedLineItemViewModel : ObservableObject
{
    /// <summary>
    /// Callback invoked when a product error is cleared.
    /// </summary>
    public Action? OnProductErrorCleared { get; set; }

    /// <summary>
    /// Callback invoked when the total price changes so the parent can revalidate totals.
    /// </summary>
    public Action? OnTotalPriceEdited { get; set; }

    partial void OnTotalPriceChanged(string value) => OnTotalPriceEdited?.Invoke();

    [ObservableProperty]
    private ProductOption? _selectedProduct;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _quantity = "1";

    [ObservableProperty]
    private string _unitPrice = "0.00";

    [ObservableProperty]
    private string _totalPrice = "0.00";

    [ObservableProperty]
    private double _confidence = 1.0;

    /// <summary>
    /// Whether this line item has low confidence and should show a warning icon.
    /// </summary>
    public bool IsLowConfidence => Confidence < 0.85 && !IsManuallyAdded;

    partial void OnConfidenceChanged(double value) => OnPropertyChanged(nameof(IsLowConfidence));

    [ObservableProperty]
    private bool _isManuallyAdded;

    partial void OnIsManuallyAddedChanged(bool value) => OnPropertyChanged(nameof(IsLowConfidence));

    [ObservableProperty]
    private bool _hasProductError;

    [ObservableProperty]
    private string _productErrorMessage = string.Empty;

    /// <summary>
    /// Suggested product name when no match found (for creating new product).
    /// </summary>
    [ObservableProperty]
    private string _suggestedProductName = string.Empty;

    /// <summary>
    /// Whether to show the create product suggestion.
    /// </summary>
    [ObservableProperty]
    private bool _showCreateProductSuggestion;

    partial void OnSelectedProductChanged(ProductOption? value)
    {
        if (value != null)
        {
            Description = value.Name;
            // Only update unit price if it's currently 0 or not set
            if (UnitPrice == "0.00" || string.IsNullOrEmpty(UnitPrice))
            {
                UnitPrice = value.UnitPrice.ToString("F2");
            }
            HasProductError = false;
            ProductErrorMessage = string.Empty;
            ShowCreateProductSuggestion = false;
            OnProductErrorCleared?.Invoke();
        }
        RecalculateTotal();
    }

    partial void OnQuantityChanged(string value)
    {
        RecalculateTotal();
    }

    partial void OnUnitPriceChanged(string value)
    {
        RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        if (decimal.TryParse(Quantity, out var qty) && decimal.TryParse(UnitPrice, out var price))
        {
            TotalPrice = (qty * price).ToString("F2");
        }
    }
}
