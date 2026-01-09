using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;
using Avalonia;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Base class for transaction modals (Expense and Revenue).
/// Provides common modal state, form fields, and operations.
/// </summary>
/// <typeparam name="TDisplayItem">The display item type (ExpenseDisplayItem or RevenueDisplayItem)</typeparam>
/// <typeparam name="TLineItem">The line item type (ExpenseLineItem or RevenueLineItem)</typeparam>
public abstract partial class TransactionModalsViewModelBase<TDisplayItem, TLineItem> : ViewModelBase
    where TDisplayItem : class
    where TLineItem : TransactionLineItemBase, new()
{
    #region Abstract Properties

    /// <summary>
    /// The transaction type name (e.g., "Expense" or "Revenue").
    /// </summary>
    protected abstract string TransactionTypeName { get; }

    /// <summary>
    /// The entity name for the counterparty (e.g., "Supplier" or "Customer").
    /// </summary>
    protected abstract string CounterpartyName { get; }

    /// <summary>
    /// The category type to filter by.
    /// </summary>
    protected abstract CategoryType CategoryTypeFilter { get; }

    /// <summary>
    /// Whether to use cost price (expenses) or selling price (revenue) for products.
    /// </summary>
    protected abstract bool UseCostPrice { get; }

    #endregion

    #region Events

    public event EventHandler? TransactionSaved;
    public event EventHandler? TransactionDeleted;
    public event EventHandler? FiltersApplied;
    public event EventHandler? FiltersCleared;
    public event EventHandler? ScrollToLineItemsRequested;

    protected void RaiseTransactionSaved() => TransactionSaved?.Invoke(this, EventArgs.Empty);
    protected void RaiseTransactionDeleted() => TransactionDeleted?.Invoke(this, EventArgs.Empty);

    #endregion

    #region Modal State

    [ObservableProperty]
    private bool _isAddEditModalOpen;

    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    [ObservableProperty]
    private bool _isFilterModalOpen;

    [ObservableProperty]
    private bool _isItemStatusModalOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _modalTitle = string.Empty;

    [ObservableProperty]
    private string _saveButtonText = string.Empty;

    #endregion

    #region Item Status Modal Fields

    protected TDisplayItem? ItemStatusItem;

    [ObservableProperty]
    private string _itemStatusModalTitle = "Update Item Status";

    [ObservableProperty]
    private string _itemStatusAction = string.Empty;

    [ObservableProperty]
    private string _itemStatusItemDescription = string.Empty;

    [ObservableProperty]
    private string? _selectedItemStatusReason;

    [ObservableProperty]
    private string _itemStatusNotes = string.Empty;

    [ObservableProperty]
    private bool _isUndoAction;

    [ObservableProperty]
    private string _itemStatusSaveButtonText = "Confirm";

    [ObservableProperty]
    private bool _hasItemStatusReasonError;

    [ObservableProperty]
    private string _itemStatusReasonErrorMessage = string.Empty;

    public abstract ObservableCollection<string> LostDamagedReasonOptions { get; }
    public abstract ObservableCollection<string> ReturnReasonOptions { get; }
    public abstract ObservableCollection<string> UndoReasonOptions { get; }

    [ObservableProperty]
    private ObservableCollection<string> _currentReasonOptions = [];

    #endregion

    #region Add/Edit Modal Fields

    protected string EditingTransactionId = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _modalDate = DateTimeOffset.Now;

    [ObservableProperty]
    private CounterpartyOption? _selectedCounterparty;

    // Called when SelectedCounterparty changes - allows derived classes to notify alias properties
    partial void OnSelectedCounterpartyChanged(CounterpartyOption? value)
    {
        OnCounterpartyChanged(value);
    }

    /// <summary>
    /// Virtual method called when SelectedCounterparty changes.
    /// Override in derived classes to notify alias property changes (e.g., SelectedCustomer, SelectedSupplier).
    /// </summary>
    protected virtual void OnCounterpartyChanged(CounterpartyOption? value) { }

    [ObservableProperty]
    private bool _hasCounterpartyError;

    [ObservableProperty]
    private CategoryOption? _selectedCategory;

    [ObservableProperty]
    private bool _hasCategoryError;

    [ObservableProperty]
    private string _modalDescription = string.Empty;

    [ObservableProperty]
    private bool _hasDescriptionError;

    [ObservableProperty]
    private decimal _modalQuantity = 1;

    [ObservableProperty]
    private decimal _modalUnitPrice;

    [ObservableProperty]
    private bool _hasUnitPriceError;

    [ObservableProperty]
    private decimal _modalTaxRate;

    [ObservableProperty]
    private decimal _modalShipping;

    [ObservableProperty]
    private decimal _modalDiscount;

    [ObservableProperty]
    private string _selectedPaymentMethod = "Cash";

    [ObservableProperty]
    private string _modalNotes = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _hasValidationMessage;

    [ObservableProperty]
    private string _receiptFileName = "No receipt attached";

    protected string? ReceiptFilePath;

    public ObservableCollection<CounterpartyOption> CounterpartyOptions { get; } = [];
    public ObservableCollection<CategoryOption> CategoryOptions { get; } = [];
    public ObservableCollection<ProductOption> ProductOptions { get; } = [];
    public ObservableCollection<string> PaymentMethodOptions { get; } = ["Cash", "Bank Card", "Bank Transfer", "Check", "PayPal", "Other"];
    public ObservableCollection<TLineItem> LineItems { get; } = [];

    // Computed totals
    public decimal Subtotal => LineItems.Count > 0
        ? LineItems.Sum(li => li.Amount)
        : ModalQuantity * ModalUnitPrice;
    public decimal TaxAmount => ModalTaxRate;
    public decimal DiscountAmount => ModalDiscount;
    public decimal ShippingAmount => ModalShipping;
    public decimal Total => Subtotal + TaxAmount + ShippingAmount - DiscountAmount;

    public string SubtotalFormatted => $"${Subtotal:N2}";
    public string TaxAmountFormatted => $"${TaxAmount:N2}";
    public string DiscountAmountFormatted => $"-${DiscountAmount:N2}";
    public string ShippingAmountFormatted => $"${ShippingAmount:N2}";
    public string TotalFormatted => $"${Total:N2}";

    partial void OnModalQuantityChanged(decimal value) => UpdateTotals();
    partial void OnModalUnitPriceChanged(decimal value) => UpdateTotals();
    partial void OnModalTaxRateChanged(decimal value) => UpdateTotals();
    partial void OnModalShippingChanged(decimal value) => UpdateTotals();
    partial void OnModalDiscountChanged(decimal value) => UpdateTotals();

    protected void UpdateTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(DiscountAmount));
        OnPropertyChanged(nameof(ShippingAmount));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(SubtotalFormatted));
        OnPropertyChanged(nameof(TaxAmountFormatted));
        OnPropertyChanged(nameof(DiscountAmountFormatted));
        OnPropertyChanged(nameof(ShippingAmountFormatted));
        OnPropertyChanged(nameof(TotalFormatted));
    }

    #endregion

    #region Delete Confirmation

    protected string DeleteTransactionIdInternal = string.Empty;

    [ObservableProperty]
    private string _deleteTransactionId = string.Empty;

    [ObservableProperty]
    private string _deleteTransactionDescription = string.Empty;

    [ObservableProperty]
    private string _deleteTransactionAmount = string.Empty;

    #endregion

    #region Filter Modal Fields

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private CounterpartyOption? _filterSelectedCounterparty;

    [ObservableProperty]
    private string? _filterCounterpartyId;

    [ObservableProperty]
    private CategoryOption? _filterSelectedCategory;

    [ObservableProperty]
    private string? _filterCategoryId;

    [ObservableProperty]
    private string? _filterAmountMin;

    [ObservableProperty]
    private string? _filterAmountMax;

    [ObservableProperty]
    private DateTimeOffset? _filterDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterDateTo;

    public ObservableCollection<string> StatusFilterOptions { get; } = ["All", "Completed", "Pending", "Partial Return", "Returned", "Cancelled"];

    #endregion

    #region Constructor

    protected TransactionModalsViewModelBase()
    {
        LoadCounterpartyOptions();
        LoadCategoryOptions();
        LoadProductOptions();
    }

    #endregion

    #region Data Loading

    protected abstract void LoadCounterpartyOptions();

    protected void LoadCategoryOptions()
    {
        CategoryOptions.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Categories == null)
            return;

        var categories = companyData.Categories
            .Where(c => c.Type == CategoryTypeFilter)
            .OrderBy(c => c.Name);

        foreach (var category in categories)
        {
            CategoryOptions.Add(new CategoryOption { Id = category.Id, Name = category.Name });
        }
    }

    protected void LoadProductOptions()
    {
        ProductOptions.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Products == null)
            return;

        // Filter products based on category type
        foreach (var product in companyData.Products.OrderBy(p => p.Name))
        {
            var category = companyData.Categories?.FirstOrDefault(c => c.Id == product.CategoryId);

            // Skip products that don't match our category type
            if (CategoryTypeFilter == CategoryType.Purchase && category?.Type == CategoryType.Sales)
                continue;
            if (CategoryTypeFilter == CategoryType.Sales && category?.Type == CategoryType.Purchase)
                continue;

            ProductOptions.Add(new ProductOption
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                UnitPrice = UseCostPrice ? product.CostPrice : product.UnitPrice
            });
        }
    }

    protected void LoadCounterpartyOptionsForFilter()
    {
        CounterpartyOptions.Clear();
        CounterpartyOptions.Add(new CounterpartyOption { Id = null, Name = $"All {CounterpartyName}s" });

        LoadCounterpartyOptionsInternal();
    }

    protected abstract void LoadCounterpartyOptionsInternal();

    protected void LoadCategoryOptionsForFilter()
    {
        CategoryOptions.Clear();
        CategoryOptions.Add(new CategoryOption { Id = null, Name = "All Categories" });

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Categories == null)
            return;

        var categories = companyData.Categories
            .Where(c => c.Type == CategoryTypeFilter)
            .OrderBy(c => c.Name);

        foreach (var category in categories)
        {
            CategoryOptions.Add(new CategoryOption { Id = category.Id, Name = category.Name });
        }
    }

    #endregion

    #region Add Modal

    [RelayCommand]
    public void OpenAddModal()
    {
        LoadCounterpartyOptions();
        LoadCategoryOptions();
        LoadProductOptions();
        ResetForm();
        IsEditMode = false;
        ModalTitle = $"Add {TransactionTypeName}";
        SaveButtonText = $"Add {TransactionTypeName}";
        IsAddEditModalOpen = true;
    }

    #endregion

    #region Edit Modal

    public abstract void OpenEditModal(TDisplayItem? item);

    protected void PopulateFormFromTransaction(Transaction transaction)
    {
        ModalDate = new DateTimeOffset(transaction.Date);
        SelectedCategory = CategoryOptions.FirstOrDefault(c => c.Id == transaction.CategoryId);
        ModalTaxRate = transaction.TaxAmount;
        ModalShipping = transaction.ShippingCost;
        ModalDiscount = transaction.Discount;
        SelectedPaymentMethod = transaction.PaymentMethod.ToString();
        ModalNotes = transaction.Notes;

        // Load line items
        LineItems.Clear();
        if (transaction.LineItems.Count > 0)
        {
            foreach (var li in transaction.LineItems)
            {
                var lineItem = new TLineItem
                {
                    SelectedProduct = ProductOptions.FirstOrDefault(p => p.Id == li.ProductId),
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice
                };
                lineItem.PropertyChanged += (_, _) => UpdateTotals();
                LineItems.Add(lineItem);
            }
        }
        else
        {
            // Fallback for old data without line items
            var lineItem = new TLineItem
            {
                Description = transaction.Description,
                Quantity = transaction.Quantity,
                UnitPrice = transaction.UnitPrice
            };
            lineItem.PropertyChanged += (_, _) => UpdateTotals();
            LineItems.Add(lineItem);
        }
        UpdateTotals();

        // Load receipt info
        ReceiptFilePath = transaction.ReferenceNumber;
        ReceiptFileName = string.IsNullOrEmpty(transaction.ReferenceNumber)
            ? "No receipt attached"
            : Path.GetFileName(transaction.ReferenceNumber);

        ClearValidationErrors();
    }

    #endregion

    #region Delete Confirmation

    [RelayCommand]
    protected void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        DeleteTransactionIdInternal = string.Empty;
        DeleteTransactionId = string.Empty;
        DeleteTransactionDescription = string.Empty;
        DeleteTransactionAmount = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteTransaction))]
    protected abstract void DeleteTransaction();

    /// <summary>
    /// Determines whether the delete transaction command can execute.
    /// Override in derived classes to customize behavior.
    /// </summary>
    protected virtual bool CanDeleteTransaction() => true;

    #endregion

    #region Filter Modal

    public void OpenFilterModal()
    {
        LoadCounterpartyOptionsForFilter();
        LoadCategoryOptionsForFilter();
        IsFilterModalOpen = true;
    }

    [RelayCommand]
    protected void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    [RelayCommand]
    protected void ApplyFilters()
    {
        FilterCounterpartyId = FilterSelectedCounterparty?.Id;
        FilterCategoryId = FilterSelectedCategory?.Id;
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    [RelayCommand]
    protected virtual void ClearFilters()
    {
        FilterStatus = "All";
        FilterSelectedCounterparty = null;
        FilterCounterpartyId = null;
        FilterSelectedCategory = null;
        FilterCategoryId = null;
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterDateFrom = null;
        FilterDateTo = null;
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    #endregion

    #region Item Status Modal

    protected void OpenItemStatusModal(TDisplayItem? item, string action, string title, string buttonText, bool isUndo, ObservableCollection<string> reasonOptions)
    {
        if (item == null) return;

        ItemStatusItem = item;
        ItemStatusAction = action;
        ItemStatusModalTitle = title;
        ItemStatusItemDescription = GetItemStatusDescription(item);
        ItemStatusSaveButtonText = buttonText;
        IsUndoAction = isUndo;
        CurrentReasonOptions = new ObservableCollection<string>(reasonOptions);
        SelectedItemStatusReason = null;
        ItemStatusNotes = string.Empty;
        IsItemStatusModalOpen = true;
    }

    protected abstract string GetItemStatusDescription(TDisplayItem item);

    [RelayCommand]
    protected void CloseItemStatusModal()
    {
        IsItemStatusModalOpen = false;
        ItemStatusItem = null;
        ItemStatusAction = string.Empty;
        SelectedItemStatusReason = null;
        ItemStatusNotes = string.Empty;
        HasItemStatusReasonError = false;
        ItemStatusReasonErrorMessage = string.Empty;
    }

    [RelayCommand]
    protected abstract void ConfirmItemStatus();

    protected static LostDamagedReason MapToLostDamagedReason(string reason)
    {
        return reason.ToLowerInvariant() switch
        {
            "damaged in transit" or "damaged during storage" or "defective product" or "customer damaged" => LostDamagedReason.Damaged,
            "lost in warehouse" => LostDamagedReason.Lost,
            "expired" => LostDamagedReason.Expired,
            _ => LostDamagedReason.Other
        };
    }

    #endregion

    #region Save Transaction

    [RelayCommand]
    protected void CloseAddEditModal()
    {
        IsAddEditModalOpen = false;
        ResetForm();
    }

    [RelayCommand]
    protected void SaveTransaction()
    {
        ClearValidationErrors();

        if (LineItems.Count == 0)
        {
            ValidationMessage = "Please add at least one line item";
            HasValidationMessage = true;
            return;
        }

        // Validate that all line items have a product selected
        var hasProductErrors = false;
        foreach (var lineItem in LineItems)
        {
            if (lineItem.SelectedProduct == null)
            {
                lineItem.HasProductError = true;
                hasProductErrors = true;
            }
        }

        if (hasProductErrors)
        {
            ValidationMessage = "Please select a product for all line items";
            HasValidationMessage = true;
            ScrollToLineItemsRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        if (IsEditMode)
        {
            SaveEditedTransaction(companyData);
        }
        else
        {
            SaveNewTransaction(companyData);
        }

        CloseAddEditModal();
    }

    protected abstract void SaveNewTransaction(CompanyData companyData);
    protected abstract void SaveEditedTransaction(CompanyData companyData);

    protected List<LineItem> CreateModelLineItems()
    {
        return LineItems.Select(li => new LineItem
        {
            ProductId = li.SelectedProduct?.Id,
            Description = li.Description,
            Quantity = li.Quantity,
            UnitPrice = li.UnitPrice,
            TaxRate = 0,
            Discount = 0
        }).ToList();
    }

    protected (string description, decimal totalQuantity, decimal averageUnitPrice) GetLineItemSummary()
    {
        var description = LineItems.Count == 1
            ? LineItems[0].Description
            : string.Join(", ", LineItems.Select(li => li.Description).Where(d => !string.IsNullOrEmpty(d)));
        var totalQuantity = LineItems.Sum(li => li.Quantity);
        var averageUnitPrice = LineItems.Count > 0 ? LineItems.Average(li => li.UnitPrice) : 0;
        return (description, totalQuantity, averageUnitPrice);
    }

    protected void ResetForm()
    {
        EditingTransactionId = string.Empty;
        ModalDate = DateTimeOffset.Now;
        SelectedCounterparty = null;
        SelectedCategory = null;
        ModalDescription = string.Empty;
        ModalQuantity = 1;
        ModalUnitPrice = 0;
        ModalTaxRate = 0;
        ModalShipping = 0;
        ModalDiscount = 0;
        SelectedPaymentMethod = "Cash";
        ModalNotes = string.Empty;
        LineItems.Clear();
        AddLineItem();
        ReceiptFileName = "No receipt attached";
        ReceiptFilePath = null;
        ClearValidationErrors();
    }

    protected void ClearValidationErrors()
    {
        HasCounterpartyError = false;
        HasCategoryError = false;
        HasDescriptionError = false;
        HasUnitPriceError = false;
        ValidationMessage = string.Empty;
        HasValidationMessage = false;
    }

    #endregion

    #region Navigation Commands

    [RelayCommand]
    protected void NavigateToCreateCounterparty()
    {
        IsAddEditModalOpen = false;
        var pageName = CounterpartyName == "Supplier" ? "Suppliers" : "Customers";
        App.NavigationService?.NavigateTo(pageName, new Dictionary<string, object?> { { "openAddModal", true } });
    }

    [RelayCommand]
    protected void NavigateToCreateCategory()
    {
        IsAddEditModalOpen = false;
        var tabIndex = CategoryTypeFilter == CategoryType.Purchase ? 0 : 1;
        App.NavigationService?.NavigateTo("Categories", new Dictionary<string, object?> { { "openAddModal", true }, { "selectedTabIndex", tabIndex } });
    }

    [RelayCommand]
    protected void NavigateToCreateProduct()
    {
        IsAddEditModalOpen = false;
        var tabIndex = CategoryTypeFilter == CategoryType.Purchase ? 0 : 1;
        App.NavigationService?.NavigateTo("Products", new Dictionary<string, object?> { { "openAddModal", true }, { "selectedTabIndex", tabIndex } });
    }

    #endregion

    #region Line Items

    [RelayCommand]
    protected void AddLineItem()
    {
        var lineItem = new TLineItem();
        lineItem.PropertyChanged += (_, _) => UpdateTotals();
        LineItems.Add(lineItem);
        UpdateTotals();
    }

    [RelayCommand]
    protected void RemoveLineItem(TLineItem? item)
    {
        if (item != null && LineItems.Count > 1)
        {
            LineItems.Remove(item);
            UpdateTotals();
        }
    }

    #endregion

    #region Receipt

    [RelayCommand]
    protected async Task AttachReceipt()
    {
        var topLevel = Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Receipt",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.pdf"
                    ]
                },
                new FilePickerFileType("All Files") { Patterns = ["*.*"] }
            ]
        });

        if (files.Count > 0)
        {
            var file = files[0];
            ReceiptFilePath = file.Path.LocalPath;
            ReceiptFileName = file.Name;
        }
    }

    protected static string GetFileType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    #endregion
}

/// <summary>
/// Base class for transaction line items.
/// </summary>
public abstract partial class TransactionLineItemBase : ObservableObject
{
    [ObservableProperty]
    private ProductOption? _selectedProduct;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private bool _hasProductError;

    public decimal Amount => Quantity * UnitPrice;
    public string AmountFormatted => $"${Amount:N2}";

    partial void OnSelectedProductChanged(ProductOption? value)
    {
        if (value != null)
        {
            Description = value.Name;
            UnitPrice = value.UnitPrice;
            HasProductError = false;
        }
    }

    partial void OnQuantityChanged(decimal value)
    {
        OnPropertyChanged(nameof(Amount));
        OnPropertyChanged(nameof(AmountFormatted));
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(Amount));
        OnPropertyChanged(nameof(AmountFormatted));
    }
}

/// <summary>
/// Generic option class for counterparty (Supplier or Customer) selection.
/// </summary>
public class CounterpartyOption
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public override string ToString() => Name;
}

/// <summary>
/// Option class for product selection in line items.
/// </summary>
public class ProductOption
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public override string ToString() => Name;
}
