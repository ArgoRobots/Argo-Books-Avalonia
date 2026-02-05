using System.Collections.ObjectModel;
using System.Globalization;
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
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Changes?".Translate(),
                    Message = "You have unapplied filter changes. Are you sure you want to close?".Translate(),
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                    return;
            }

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
    private byte[]? _currentImageData;
    private string? _currentFileName;

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

    /// <summary>
    /// Gets the modal width based on current state.
    /// Narrower for loading/error states, wider for results.
    /// </summary>
    public double ModalWidth => HasScanResult ? 1100 : 480;

    partial void OnHasScanResultChanged(bool value)
    {
        OnPropertyChanged(nameof(ModalWidth));
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
    private CategoryOption? _selectedCategory;

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
    public ObservableCollection<CategoryOption> CategoryOptions { get; } = [];
    public ObservableCollection<ProductOption> ProductOptions { get; } = [];
    public ObservableCollection<string> PaymentMethodOptions { get; } =
    [
        "Cash",
        "Credit Card",
        "Debit Card",
        "Bank Transfer",
        "Check",
        "PayPal",
        "Other"
    ];

    [ObservableProperty]
    private bool _hasTotalError;

    [ObservableProperty]
    private string _totalErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasSupplierError;

    [ObservableProperty]
    private string _supplierErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasCategoryError;

    [ObservableProperty]
    private string _categoryErrorMessage = string.Empty;

    partial void OnSelectedSupplierChanged(SupplierOption? value)
    {
        if (value != null)
        {
            HasSupplierError = false;
            SupplierErrorMessage = string.Empty;
        }
    }

    partial void OnSelectedCategoryChanged(CategoryOption? value)
    {
        if (value != null)
        {
            HasCategoryError = false;
            CategoryErrorMessage = string.Empty;
        }
    }

    // AI Suggestion State
    [ObservableProperty]
    private bool _isLoadingAiSuggestions;

    [ObservableProperty]
    private bool _hasAiSuggestions;

    [ObservableProperty]
    private SupplierCategorySuggestion? _aiSuggestion;

    [ObservableProperty]
    private double _supplierMatchConfidence;

    [ObservableProperty]
    private double _categoryMatchConfidence;

    [ObservableProperty]
    private bool _showCreateSupplierSuggestion;

    [ObservableProperty]
    private string _suggestedSupplierName = string.Empty;

    [ObservableProperty]
    private bool _showCreateCategorySuggestion;

    [ObservableProperty]
    private string _suggestedCategoryName = string.Empty;

    [ObservableProperty]
    private bool _isAiConfigured;

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

    #region AI Scan Commands

    /// <summary>
    /// Opens the scan review modal and starts scanning.
    /// </summary>
    public async Task OpenScanModalAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            App.AddNotification("Error".Translate(), "File not found.".Translate(), NotificationType.Error);
            return;
        }

        try
        {
            _currentImageData = await File.ReadAllBytesAsync(filePath);
            _currentFileName = Path.GetFileName(filePath);
            await OpenScanModalWithDataAsync(_currentImageData, _currentFileName, filePath);
        }
        catch (Exception ex)
        {
            App.AddNotification("Error".Translate(), "Failed to read file: {0}".TranslateFormat(ex.Message), NotificationType.Error);
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

        LoadSupplierOptions();
        LoadProductOptions();

        // Set the image path for preview
        if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
        {
            ReceiptImagePath = tempFilePath;
        }
        else
        {
            // Create temp file for preview
            var tempDir = Path.Combine(Path.GetTempPath(), "ArgoBooks", "ScanPreview");
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, fileName);
            await File.WriteAllBytesAsync(tempPath, imageData);
            ReceiptImagePath = tempPath;
        }

        IsScanReviewModalOpen = true;
        IsScanning = true;
        HasScanError = false;
        HasScanResult = false;

        // Start scanning
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
                ScanErrorMessage = "Azure Document Intelligence is not configured.\n\nPlease add your API key and endpoint in Settings > AI Receipt Scanning.".Translate();
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
                    HasScanError = true;
                    if (!string.IsNullOrEmpty(usageCheck.ErrorMessage))
                    {
                        ScanErrorMessage = usageCheck.ErrorMessage;
                    }
                    else
                    {
                        ScanErrorMessage = "Monthly scan limit reached ({0}/{1}).\n\nYour limit resets on {2}.\n\nUpgrade to Premium for more scans.".TranslateFormat(
                            usageCheck.ScanCount,
                            usageCheck.MonthlyLimit,
                            usageCheck.ResetsAt ?? "the 1st of next month");
                    }
                    IsScanning = false;
                    return;
                }
            }

            ScanningMessage = "Analyzing receipt with AI...".Translate();

            var result = await _scannerService.ScanReceiptAsync(_currentImageData, _currentFileName);

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
                    IsNearLimit = incrementResult.Remaining <= 50 && incrementResult.Remaining > 0;
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
        IsNearLimit = usageCheck.Remaining <= 50 && usageCheck.Remaining > 0;
    }

    private async void PopulateScanResults(ReceiptScanResult result)
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

        // Confidence
        ConfidenceScore = result.Confidence;
        ConfidenceText = $"{result.Confidence:P0}";
        IsHighConfidence = result.Confidence >= 0.85;
        IsMediumConfidence = result.Confidence >= 0.6 && result.Confidence < 0.85;
        IsLowConfidence = result.Confidence < 0.6;

        // Line items (filter out discounts and non-product lines)
        LineItems.Clear();
        foreach (var item in result.LineItems)
        {
            // Skip discount lines and negative-amount adjustments
            if (IsDiscountLine(item))
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

            LineItems.Add(lineItem);
        }

        // Get AI suggestions for supplier and category
        await GetAiSuggestionsAsync(result);
    }

    [RelayCommand]
    private void CloseScanReviewModal()
    {
        IsScanReviewModalOpen = false;
        ResetScanModal();
    }

    /// <summary>
    /// Requests to close the scan review modal, showing confirmation if a receipt has been scanned.
    /// </summary>
    [RelayCommand]
    private async Task RequestCloseScanReviewModalAsync()
    {
        if (HasScanResult)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Scanned Receipt?".Translate(),
                    Message = "You have a scanned receipt that hasn't been saved. Are you sure you want to close?".Translate(),
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                    return;
            }
        }

        CloseScanReviewModal();
    }

    [RelayCommand]
    private async Task RetryScan()
    {
        if (_currentImageData != null && _currentFileName != null)
        {
            // Set scanning state first to show loading spinner
            IsScanning = true;
            ScanningMessage = "Retrying...".Translate();

            // Small delay to ensure smooth transition from error to loading state
            await Task.Delay(100);

            // Now clear the error state
            HasScanError = false;
            HasScanResult = false;

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
            IsManuallyAdded = true
        });
    }

    [RelayCommand]
    private void RemoveLineItem(ScannedLineItemViewModel? item)
    {
        if (item != null)
        {
            LineItems.Remove(item);
        }
    }

    [RelayCommand]
    private void CreateTransaction()
    {
        // Validate
        HasTotalError = false;
        HasSupplierError = false;

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

        if (hasErrors)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
        {
            App.AddNotification("Error".Translate(), "No company is open.".Translate(), NotificationType.Error);
            return;
        }

        // Parse values
        decimal.TryParse(ExtractedSubtotal, out var subtotal);
        decimal.TryParse(ExtractedTax, out var taxAmount);

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
            fileData = Convert.ToBase64String(_currentImageData);
        }

        if (IsRevenue)
        {
            // Create revenue transaction
            CreateRevenueTransaction(companyData, receiptId, fileData, total, subtotal, taxAmount, lineItems);
        }
        else
        {
            // Create expense transaction
            CreateExpenseTransaction(companyData, receiptId, fileData, total, subtotal, taxAmount, lineItems);
        }

        App.CompanyManager?.MarkAsChanged();
        ReceiptScanned?.Invoke(this, EventArgs.Empty);
        CloseScanReviewModal();
    }

    private void CreateExpenseTransaction(CompanyData companyData, string receiptId, string? fileData,
        decimal total, decimal subtotal, decimal taxAmount, List<LineItem> lineItems)
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

        // Record undo action
        var capturedReceipt = receipt;
        var capturedExpense = expense;
        var action = new DelegateAction(
            $"AI scan expense {expenseId}",
            () =>
            {
                companyData.Expenses.Remove(capturedExpense);
                companyData.Receipts.Remove(capturedReceipt);
                companyData.IdCounters.Expense--;
                companyData.IdCounters.Receipt--;
                ReceiptScanned?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Expenses.Add(capturedExpense);
                companyData.Receipts.Add(capturedReceipt);
                companyData.IdCounters.Expense++;
                companyData.IdCounters.Receipt++;
                ReceiptScanned?.Invoke(this, EventArgs.Empty);
            });

        companyData.Expenses.Add(expense);
        companyData.Receipts.Add(receipt);
        App.UndoRedoManager.RecordAction(action);
    }

    private void CreateRevenueTransaction(CompanyData companyData, string receiptId, string? fileData,
        decimal total, decimal subtotal, decimal taxAmount, List<LineItem> lineItems)
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

        // Record undo action
        var capturedReceipt = receipt;
        var capturedRevenue = revenue;
        var action = new DelegateAction(
            $"AI scan revenue {revenueId}",
            () =>
            {
                companyData.Revenues.Remove(capturedRevenue);
                companyData.Receipts.Remove(capturedReceipt);
                companyData.IdCounters.Revenue--;
                companyData.IdCounters.Receipt--;
                ReceiptScanned?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Revenues.Add(capturedRevenue);
                companyData.Receipts.Add(capturedReceipt);
                companyData.IdCounters.Revenue++;
                companyData.IdCounters.Receipt++;
                ReceiptScanned?.Invoke(this, EventArgs.Empty);
            });

        companyData.Revenues.Add(revenue);
        companyData.Receipts.Add(receipt);
        App.UndoRedoManager.RecordAction(action);
    }

    [RelayCommand]
    private void NavigateToCreateSupplier()
    {
        // Close modal and navigate to suppliers page with add modal open
        CloseScanReviewModal();
        App.NavigationService?.NavigateTo("Suppliers");
        App.SupplierModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void NavigateToCreateCategory()
    {
        // Close modal and navigate to categories page with add modal open
        CloseScanReviewModal();
        App.NavigationService?.NavigateTo("Categories");
        App.CategoryModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void NavigateToCreateProduct()
    {
        // Close modal and navigate to products page with add modal open
        CloseScanReviewModal();
        App.NavigationService?.NavigateTo("Products");
        App.ProductModalsViewModel?.OpenAddModal();
    }

    /// <summary>
    /// Creates a new product from a line item suggestion.
    /// </summary>
    [RelayCommand]
    private void CreateSuggestedProduct(ScannedLineItemViewModel? lineItem)
    {
        if (lineItem == null || string.IsNullOrEmpty(lineItem.SuggestedProductName))
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Products == null) return;

        // Create new product with default purchase category
        var newId = Guid.NewGuid().ToString();
        var defaultCategory = companyData.Categories?
            .FirstOrDefault(c => c.Type == CategoryType.Expense);

        var newProduct = new Product
        {
            Id = newId,
            Name = lineItem.SuggestedProductName,
            Description = lineItem.Description,
            CostPrice = decimal.TryParse(lineItem.UnitPrice, out var price) ? price : 0,
            UnitPrice = 0,
            CategoryId = defaultCategory?.Id
        };

        companyData.Products.Add(newProduct);

        // Add to options and select
        var option = new ProductOption
        {
            Id = newId,
            Name = newProduct.Name,
            Description = newProduct.Description,
            UnitPrice = newProduct.CostPrice
        };
        ProductOptions.Add(option);
        lineItem.SelectedProduct = option;
        lineItem.ShowCreateProductSuggestion = false;

        companyData.MarkAsModified();
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
        }
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
        var openAiService = new OpenAiService(App.ErrorLogger, App.TelemetryManager);
        IsAiConfigured = openAiService.IsConfigured;

        if (!openAiService.IsConfigured)
        {
            // Fall back to basic matching
            TryBasicSupplierMatch(result.SupplierName);
            return;
        }

        IsLoadingAiSuggestions = true;
        HasAiSuggestions = false;
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
                ExistingSuppliers = companyData.Suppliers?.Select(s => new ExistingSupplierInfo
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList() ?? [],
                ExistingCategories = companyData.Categories?
                    .Where(c => c.Type == CategoryType.Expense)
                    .Select(c => new ExistingCategoryInfo
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description
                    }).ToList() ?? []
            };

            var suggestion = await openAiService.GetSupplierCategorySuggestionAsync(request);

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
            System.Diagnostics.Debug.WriteLine($"AI suggestion failed: {ex.Message}");
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
        AiSuggestion = suggestion;
        HasAiSuggestions = true;

        // Apply supplier suggestion
        if (!string.IsNullOrEmpty(suggestion.MatchedSupplierId))
        {
            var supplier = SupplierOptions.FirstOrDefault(s => s.Id == suggestion.MatchedSupplierId);
            if (supplier != null)
            {
                SelectedSupplier = supplier;
                SupplierMatchConfidence = suggestion.SupplierConfidence;
            }
        }
        else if (suggestion.ShouldCreateNewSupplier && suggestion.NewSupplier != null)
        {
            ShowCreateSupplierSuggestion = true;
            SuggestedSupplierName = ToTitleCase(suggestion.NewSupplier.Name);
            SupplierMatchConfidence = 0;
        }

    }

    /// <summary>
    /// Determines if a line item is a discount rather than a product.
    /// Only filters based on description keywords to avoid incorrectly
    /// removing legitimate line items with negative amounts.
    /// </summary>
    private static bool IsDiscountLine(ScannedLineItem item)
    {
        var desc = item.Description?.ToLowerInvariant() ?? string.Empty;
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
            return text?.Trim() ?? string.Empty;

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
        var companyName = companyData?.Settings?.Company?.Name;

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
            SupplierMatchConfidence = 0.7; // Assume medium confidence for basic match
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
    /// Creates a new supplier from the AI suggestion.
    /// </summary>
    [RelayCommand]
    private void CreateSuggestedSupplier()
    {
        if (AiSuggestion?.NewSupplier == null || string.IsNullOrEmpty(SuggestedSupplierName))
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
            Notes = AiSuggestion.NewSupplier.Notes ?? "Created from AI receipt scan".Translate()
        };

        companyData.Suppliers.Add(newSupplier);

        // Add to options and select
        var option = new SupplierOption { Id = newId, Name = newSupplier.Name };
        SupplierOptions.Add(option);
        SelectedSupplier = option;

        ShowCreateSupplierSuggestion = false;
        SupplierMatchConfidence = 1.0;

        App.CompanyManager?.MarkAsChanged();
    }

    /// <summary>
    /// Creates a new category from the AI suggestion.
    /// </summary>
    [RelayCommand]
    private void CreateSuggestedCategory()
    {
        if (AiSuggestion?.NewCategory == null || string.IsNullOrEmpty(SuggestedCategoryName))
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        // Generate ID
        companyData.IdCounters.Category++;
        var newId = $"CAT-PUR-{companyData.IdCounters.Category:D3}";

        // Create category
        var newCategory = new Category
        {
            Id = newId,
            Type = CategoryType.Expense,
            Name = SuggestedCategoryName,
            Description = AiSuggestion.NewCategory.Description,
            ItemType = AiSuggestion.NewCategory.ItemType
        };

        companyData.Categories.Add(newCategory);

        // Add to options and select
        var option = new CategoryOption { Id = newId, Name = newCategory.Name };
        CategoryOptions.Add(option);
        SelectedCategory = option;

        ShowCreateCategorySuggestion = false;
        CategoryMatchConfidence = 1.0;

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

    /// <summary>
    /// Dismisses the category suggestion without creating.
    /// </summary>
    [RelayCommand]
    private void DismissCategorySuggestion()
    {
        ShowCreateCategorySuggestion = false;
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
        ScanErrorMessage = string.Empty;
        ReceiptImagePath = null;
        ExtractedSupplier = string.Empty;
        ExtractedDate = DateTimeOffset.Now;
        ExtractedSubtotal = string.Empty;
        ExtractedTax = string.Empty;
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
        HasTotalError = false;
        HasSupplierError = false;
        _currentImageData = null;
        _currentFileName = null;

        // Reset AI suggestion state
        IsLoadingAiSuggestions = false;
        HasAiSuggestions = false;
        AiSuggestion = null;
        SupplierMatchConfidence = 0;
        ShowCreateSupplierSuggestion = false;
        SuggestedSupplierName = string.Empty;

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

    private void LoadCategoryOptions()
    {
        CategoryOptions.Clear();
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Categories == null) return;

        // Load categories based on transaction type (Revenue or Expense)
        var targetType = IsRevenue ? CategoryType.Revenue : CategoryType.Expense;
        foreach (var category in companyData.Categories
            .Where(c => c.Type == targetType)
            .OrderBy(c => c.Name))
        {
            CategoryOptions.Add(new CategoryOption { Id = category.Id, Name = category.Name });
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

    private IReceiptScannerService? CreateScannerService()
    {
        // Credentials are loaded from .env file by the service
        return new AzureReceiptScannerService(App.ErrorLogger, App.TelemetryManager);
    }

    private IReceiptUsageService? CreateUsageService()
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

    [ObservableProperty]
    private bool _isManuallyAdded;

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
