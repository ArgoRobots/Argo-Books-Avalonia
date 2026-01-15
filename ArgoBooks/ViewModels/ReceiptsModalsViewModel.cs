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

    #endregion

    #region Filter Modal Commands

    /// <summary>
    /// Opens the filter modal.
    /// </summary>
    public void OpenFilterModal()
    {
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
        FilterType = "All";
        FilterDateFrom = null;
        FilterDateTo = null;
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterSource = "All";
        FilterFileType = "All";
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    #endregion

    #region AI Scan Review Modal State

    private IReceiptScannerService? _scannerService;
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

    [ObservableProperty]
    private string? _receiptImagePath;

    [ObservableProperty]
    private string _extractedVendor = string.Empty;

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

    public ObservableCollection<SupplierOption> SupplierOptions { get; } = [];
    public ObservableCollection<CategoryOption> CategoryOptions { get; } = [];
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
    private bool _hasVendorError;

    [ObservableProperty]
    private string _vendorErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasTotalError;

    [ObservableProperty]
    private string _totalErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasSupplierError;

    [ObservableProperty]
    private bool _hasCategoryError;

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
        LoadCategoryOptions();

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

            ScanningMessage = "Analyzing receipt with AI...".Translate();

            var result = await _scannerService.ScanReceiptAsync(_currentImageData, _currentFileName);

            if (!result.IsSuccess)
            {
                HasScanError = true;
                ScanErrorMessage = result.ErrorMessage ?? "Unknown error occurred.".Translate();
                IsScanning = false;
                return;
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

    private async void PopulateScanResults(ReceiptScanResult result)
    {
        ExtractedVendor = result.VendorName ?? string.Empty;
        ExtractedDate = result.TransactionDate.HasValue
            ? new DateTimeOffset(result.TransactionDate.Value)
            : DateTimeOffset.Now;
        ExtractedSubtotal = result.Subtotal?.ToString("F2") ?? "0.00";
        ExtractedTax = result.TaxAmount?.ToString("F2") ?? "0.00";
        ExtractedTotal = result.TotalAmount?.ToString("F2") ?? "0.00";

        // Confidence
        ConfidenceScore = result.Confidence;
        ConfidenceText = $"{result.Confidence:P0}";
        IsHighConfidence = result.Confidence >= 0.85;
        IsMediumConfidence = result.Confidence >= 0.6 && result.Confidence < 0.85;
        IsLowConfidence = result.Confidence < 0.6;

        // Line items
        LineItems.Clear();
        foreach (var item in result.LineItems)
        {
            LineItems.Add(new ScannedLineItemViewModel
            {
                Description = item.Description,
                Quantity = item.Quantity.ToString("F2"),
                UnitPrice = item.UnitPrice.ToString("F2"),
                TotalPrice = item.TotalPrice.ToString("F2"),
                Confidence = item.Confidence
            });
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

    [RelayCommand]
    private async Task RetryScan()
    {
        if (_currentImageData != null && _currentFileName != null)
        {
            IsScanning = true;
            HasScanError = false;
            HasScanResult = false;
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
    private void CreateExpense()
    {
        // Validate
        HasVendorError = false;
        HasTotalError = false;
        HasSupplierError = false;
        HasCategoryError = false;

        var hasErrors = false;

        if (string.IsNullOrWhiteSpace(ExtractedVendor))
        {
            HasVendorError = true;
            VendorErrorMessage = "Vendor name is required.".Translate();
            hasErrors = true;
        }

        if (!decimal.TryParse(ExtractedTotal, out var total) || total <= 0)
        {
            HasTotalError = true;
            TotalErrorMessage = "Please enter a valid total amount.".Translate();
            hasErrors = true;
        }

        if (SelectedSupplier == null)
        {
            HasSupplierError = true;
            hasErrors = true;
        }

        if (SelectedCategory == null)
        {
            HasCategoryError = true;
            hasErrors = true;
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

        // Create expense
        companyData.IdCounters.Purchase++;
        var expenseId = $"PUR-{DateTime.Now:yyyy}-{companyData.IdCounters.Purchase:D5}";

        // Create line items
        var lineItems = LineItems.Select(li =>
        {
            decimal.TryParse(li.Quantity, out var qty);
            decimal.TryParse(li.UnitPrice, out var unitPrice);
            return new LineItem
            {
                Description = li.Description,
                Quantity = qty > 0 ? qty : 1,
                UnitPrice = unitPrice
            };
        }).Where(li => !string.IsNullOrWhiteSpace(li.Description)).ToList();

        var expense = new Purchase
        {
            Id = expenseId,
            Date = ExtractedDate?.DateTime ?? DateTime.Now,
            SupplierId = SelectedSupplier?.Id,
            CategoryId = SelectedCategory?.Id,
            Description = lineItems.Count > 0 ? lineItems[0].Description : ExtractedVendor,
            LineItems = lineItems,
            Quantity = lineItems.Sum(li => li.Quantity),
            UnitPrice = lineItems.Count > 0 ? lineItems.Average(li => li.UnitPrice) : subtotal,
            Amount = subtotal > 0 ? subtotal : total,
            TaxRate = subtotal > 0 && taxAmount > 0 ? (taxAmount / subtotal) * 100 : 0,
            TaxAmount = taxAmount,
            Total = total,
            PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash,
            Notes = Notes,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Create receipt
        companyData.IdCounters.Receipt++;
        var receiptId = $"RCP-{DateTime.Now:yyyy}-{companyData.IdCounters.Receipt:D5}";

        string? fileData = null;
        if (_currentImageData != null)
        {
            fileData = Convert.ToBase64String(_currentImageData);
        }

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
            Vendor = ExtractedVendor,
            Source = "AI Scanned",
            OcrData = CreateOcrData(),
            CreatedAt = DateTime.Now
        };

        expense.ReceiptId = receiptId;

        // Record undo action
        var capturedReceipt = receipt;
        var capturedExpense = expense;
        var action = new DelegateAction(
            $"AI scan receipt {receiptId}",
            () =>
            {
                companyData.Purchases.Remove(capturedExpense);
                companyData.Receipts.Remove(capturedReceipt);
                companyData.IdCounters.Purchase--;
                companyData.IdCounters.Receipt--;
                ReceiptScanned?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Purchases.Add(capturedExpense);
                companyData.Receipts.Add(capturedReceipt);
                companyData.IdCounters.Purchase++;
                companyData.IdCounters.Receipt++;
                ReceiptScanned?.Invoke(this, EventArgs.Empty);
            });

        companyData.Purchases.Add(expense);
        companyData.Receipts.Add(receipt);
        App.UndoRedoManager.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        ReceiptScanned?.Invoke(this, EventArgs.Empty);
        CloseScanReviewModal();
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
        var openAiService = new OpenAiService();
        IsAiConfigured = openAiService.IsConfigured;

        if (!openAiService.IsConfigured)
        {
            // Fall back to basic matching
            TryBasicSupplierMatch(result.VendorName);
            return;
        }

        IsLoadingAiSuggestions = true;
        HasAiSuggestions = false;
        ShowCreateSupplierSuggestion = false;
        ShowCreateCategorySuggestion = false;

        try
        {
            var companyData = App.CompanyManager?.CompanyData;
            if (companyData == null)
            {
                TryBasicSupplierMatch(result.VendorName);
                return;
            }

            var request = new ReceiptAnalysisRequest
            {
                VendorName = result.VendorName ?? string.Empty,
                RawText = result.RawText,
                LineItemDescriptions = result.LineItems.Select(li => li.Description).ToList(),
                TotalAmount = result.TotalAmount ?? 0,
                ExistingSuppliers = companyData.Suppliers?.Select(s => new ExistingSupplierInfo
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList() ?? [],
                ExistingCategories = companyData.Categories?
                    .Where(c => c.Type == CategoryType.Purchase)
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
                TryBasicSupplierMatch(result.VendorName);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AI suggestion failed: {ex.Message}");
            TryBasicSupplierMatch(result.VendorName);
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

        // Apply category suggestion
        if (!string.IsNullOrEmpty(suggestion.MatchedCategoryId))
        {
            var category = CategoryOptions.FirstOrDefault(c => c.Id == suggestion.MatchedCategoryId);
            if (category != null)
            {
                SelectedCategory = category;
                CategoryMatchConfidence = suggestion.CategoryConfidence;
            }
        }
        else if (suggestion.ShouldCreateNewCategory && suggestion.NewCategory != null)
        {
            ShowCreateCategorySuggestion = true;
            SuggestedCategoryName = ToTitleCase(suggestion.NewCategory.Name);
            CategoryMatchConfidence = 0;
        }
    }

    /// <summary>
    /// Converts a string to title case (e.g., "HARBOR LANE CAFE" -> "Harbor Lane Cafe").
    /// </summary>
    private static string ToTitleCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
    }

    /// <summary>
    /// Falls back to basic string matching for supplier.
    /// </summary>
    private void TryBasicSupplierMatch(string? vendorName)
    {
        if (string.IsNullOrEmpty(vendorName))
            return;

        var matchedSupplier = SupplierOptions.FirstOrDefault(s =>
            s.Name.Contains(vendorName, StringComparison.OrdinalIgnoreCase) ||
            vendorName.Contains(s.Name, StringComparison.OrdinalIgnoreCase));

        if (matchedSupplier != null)
        {
            SelectedSupplier = matchedSupplier;
            SupplierMatchConfidence = 0.7; // Assume medium confidence for basic match
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
        App.AddNotification("Supplier Created".Translate(), "Created new supplier: {0}".TranslateFormat(newSupplier.Name), NotificationType.Success);
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
            Type = CategoryType.Purchase,
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
        App.AddNotification("Category Created".Translate(), "Created new category: {0}".TranslateFormat(newCategory.Name), NotificationType.Success);
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

    private void ResetScanModal()
    {
        IsScanning = false;
        HasScanError = false;
        HasScanResult = false;
        ScanErrorMessage = string.Empty;
        ReceiptImagePath = null;
        ExtractedVendor = string.Empty;
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
        SelectedCategory = null;
        SelectedPaymentMethod = "Cash";
        Notes = string.Empty;
        HasVendorError = false;
        HasTotalError = false;
        HasSupplierError = false;
        HasCategoryError = false;
        _currentImageData = null;
        _currentFileName = null;

        // Reset AI suggestion state
        IsLoadingAiSuggestions = false;
        HasAiSuggestions = false;
        AiSuggestion = null;
        SupplierMatchConfidence = 0;
        CategoryMatchConfidence = 0;
        ShowCreateSupplierSuggestion = false;
        ShowCreateCategorySuggestion = false;
        SuggestedSupplierName = string.Empty;
        SuggestedCategoryName = string.Empty;
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

        foreach (var category in companyData.Categories
            .Where(c => c.Type == CategoryType.Purchase)
            .OrderBy(c => c.Name))
        {
            CategoryOptions.Add(new CategoryOption { Id = category.Id, Name = category.Name });
        }
    }

    private IReceiptScannerService? CreateScannerService()
    {
        // Credentials are loaded from .env file by the service
        return new AzureReceiptScannerService();
    }

    private OcrData CreateOcrData()
    {
        decimal.TryParse(ExtractedTotal, out var total);
        decimal.TryParse(ExtractedSubtotal, out var subtotal);
        decimal.TryParse(ExtractedTax, out var tax);

        return new OcrData
        {
            ExtractedVendor = ExtractedVendor,
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
