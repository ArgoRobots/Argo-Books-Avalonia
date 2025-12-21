using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for revenue modals (Add, Edit, Delete, Filter).
/// </summary>
public partial class RevenueModalsViewModel : ViewModelBase
{
    #region Events

    public event EventHandler? RevenueSaved;
    public event EventHandler? RevenueDeleted;
    public event EventHandler? FiltersApplied;
    public event EventHandler? FiltersCleared;

    #endregion

    #region Modal State

    [ObservableProperty]
    private bool _isAddEditModalOpen;

    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    [ObservableProperty]
    private bool _isFilterModalOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _modalTitle = "Add Revenue";

    [ObservableProperty]
    private string _saveButtonText = "Add Revenue";

    #endregion

    #region Add/Edit Modal Fields

    private string _editingRevenueId = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _modalDate = DateTimeOffset.Now;

    [ObservableProperty]
    private CustomerOption? _selectedCustomer;

    [ObservableProperty]
    private bool _hasCustomerError;

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

    private string? _receiptFilePath;

    public ObservableCollection<CustomerOption> CustomerOptions { get; } = [];
    public ObservableCollection<CategoryOption> CategoryOptions { get; } = [];
    public ObservableCollection<ProductOption> ProductOptions { get; } = [];
    public ObservableCollection<string> PaymentMethodOptions { get; } = ["Cash", "Bank Card", "Bank Transfer", "Check", "PayPal", "Other"];
    public ObservableCollection<RevenueLineItem> LineItems { get; } = [];

    // Computed totals from line items
    public decimal Subtotal => LineItems.Sum(li => li.Amount);
    public decimal TaxAmount => Subtotal * (ModalTaxRate / 100m);
    public decimal DiscountAmount => ModalDiscount;
    public decimal ShippingAmount => ModalShipping;
    public decimal Total => Subtotal + TaxAmount + ShippingAmount - DiscountAmount;

    public string SubtotalFormatted => $"${Subtotal:N2}";
    public string TaxAmountFormatted => $"${TaxAmount:N2}";
    public string DiscountAmountFormatted => $"-${DiscountAmount:N2}";
    public string ShippingAmountFormatted => $"${ShippingAmount:N2}";
    public string TotalFormatted => $"${Total:N2}";

    partial void OnModalTaxRateChanged(decimal value) => UpdateTotals();
    partial void OnModalShippingChanged(decimal value) => UpdateTotals();
    partial void OnModalDiscountChanged(decimal value) => UpdateTotals();

    private void UpdateTotals()
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

    private string _deleteRevenueIdInternal = string.Empty;

    [ObservableProperty]
    private string _deleteRevenueId = string.Empty;

    [ObservableProperty]
    private string _deleteRevenueDescription = string.Empty;

    [ObservableProperty]
    private string _deleteRevenueAmount = string.Empty;

    #endregion

    #region Filter Modal Fields

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private CustomerOption? _filterSelectedCustomer;

    [ObservableProperty]
    private string? _filterCustomerId;

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

    public RevenueModalsViewModel()
    {
        LoadCustomerOptions();
        LoadCategoryOptions();
        LoadProductOptions();
    }

    private void LoadCustomerOptions()
    {
        CustomerOptions.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Customers == null)
            return;

        foreach (var customer in companyData.Customers.OrderBy(c => c.Name))
        {
            CustomerOptions.Add(new CustomerOption { Id = customer.Id, Name = customer.Name });
        }
    }

    private void LoadCategoryOptions()
    {
        CategoryOptions.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Categories == null)
            return;

        var salesCategories = companyData.Categories
            .Where(c => c.Type == CategoryType.Sales)
            .OrderBy(c => c.Name);

        foreach (var category in salesCategories)
        {
            CategoryOptions.Add(new CategoryOption { Id = category.Id, Name = category.Name });
        }
    }

    private void LoadProductOptions()
    {
        ProductOptions.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Products == null)
            return;

        foreach (var product in companyData.Products.OrderBy(p => p.Name))
        {
            ProductOptions.Add(new ProductOption
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                UnitPrice = product.UnitPrice  // Use selling price for revenue
            });
        }
    }

    #endregion

    #region Add Modal

    [RelayCommand]
    public void OpenAddModal()
    {
        LoadCustomerOptions();
        LoadCategoryOptions();
        LoadProductOptions();
        ResetForm();
        IsEditMode = false;
        ModalTitle = "Add Revenue";
        SaveButtonText = "Add Revenue";
        IsAddEditModalOpen = true;
    }

    #endregion

    #region Edit Modal

    public void OpenEditModal(RevenueDisplayItem? item)
    {
        if (item == null) return;

        var sale = App.CompanyManager?.CompanyData?.Sales?.FirstOrDefault(s => s.Id == item.Id);
        if (sale == null) return;

        LoadCustomerOptions();
        LoadCategoryOptions();

        _editingRevenueId = sale.Id;
        IsEditMode = true;
        ModalTitle = $"Edit Sale {sale.Id}";
        SaveButtonText = "Save Changes";

        // Populate form
        ModalDate = new DateTimeOffset(sale.Date);
        SelectedCustomer = CustomerOptions.FirstOrDefault(c => c.Id == sale.CustomerId);
        SelectedCategory = CategoryOptions.FirstOrDefault(c => c.Id == sale.CategoryId);
        ModalDescription = sale.Description;
        ModalQuantity = sale.Quantity;
        ModalUnitPrice = sale.UnitPrice;
        ModalTaxRate = sale.TaxRate;
        ModalShipping = sale.ShippingCost;
        ModalDiscount = sale.Discount;
        SelectedPaymentMethod = sale.PaymentMethod.ToString();
        ModalNotes = sale.Notes;

        // Load receipt info if available
        _receiptFilePath = sale.ReferenceNumber;
        ReceiptFileName = string.IsNullOrEmpty(sale.ReferenceNumber)
            ? "No receipt attached"
            : System.IO.Path.GetFileName(sale.ReferenceNumber);

        ClearValidationErrors();
        IsAddEditModalOpen = true;
    }

    #endregion

    #region Delete Confirmation

    public void OpenDeleteConfirm(RevenueDisplayItem? item)
    {
        if (item == null) return;

        _deleteRevenueIdInternal = item.Id;
        DeleteRevenueId = item.Id;
        DeleteRevenueDescription = item.ProductDescription;
        DeleteRevenueAmount = item.TotalFormatted;
        IsDeleteConfirmOpen = true;
    }

    [RelayCommand]
    private void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deleteRevenueIdInternal = string.Empty;
        DeleteRevenueId = string.Empty;
        DeleteRevenueDescription = string.Empty;
        DeleteRevenueAmount = string.Empty;
    }

    [RelayCommand]
    private void DeleteRevenue()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Sales == null) return;

        var sale = companyData.Sales.FirstOrDefault(s => s.Id == _deleteRevenueIdInternal);
        if (sale == null) return;

        // Create undo action
        var deletedSale = sale;
        var action = new RevenueDeleteAction(
            $"Delete sale {sale.Id}",
            deletedSale,
            () =>
            {
                companyData.Sales.Add(deletedSale);
                RevenueDeleted?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Sales.Remove(deletedSale);
                RevenueDeleted?.Invoke(this, EventArgs.Empty);
            });

        // Remove the sale
        companyData.Sales.Remove(sale);

        // Record undo action and mark as changed
        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        RevenueDeleted?.Invoke(this, EventArgs.Empty);
        CloseDeleteConfirm();
    }

    #endregion

    #region Filter Modal

    public void OpenFilterModal()
    {
        // Load options for filter dropdowns with "All" option
        LoadCustomerOptionsForFilter();
        LoadCategoryOptionsForFilter();
        IsFilterModalOpen = true;
    }

    private void LoadCustomerOptionsForFilter()
    {
        CustomerOptions.Clear();
        CustomerOptions.Add(new CustomerOption { Id = null, Name = "All Customers" });

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Customers == null)
            return;

        foreach (var customer in companyData.Customers.OrderBy(c => c.Name))
        {
            CustomerOptions.Add(new CustomerOption { Id = customer.Id, Name = customer.Name });
        }
    }

    private void LoadCategoryOptionsForFilter()
    {
        CategoryOptions.Clear();
        CategoryOptions.Add(new CategoryOption { Id = null, Name = "All Categories" });

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Categories == null)
            return;

        var salesCategories = companyData.Categories
            .Where(c => c.Type == CategoryType.Sales)
            .OrderBy(c => c.Name);

        foreach (var category in salesCategories)
        {
            CategoryOptions.Add(new CategoryOption { Id = category.Id, Name = category.Name });
        }
    }

    [RelayCommand]
    private void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        FilterCustomerId = FilterSelectedCustomer?.Id;
        FilterCategoryId = FilterSelectedCategory?.Id;
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterStatus = "All";
        FilterSelectedCustomer = null;
        FilterCustomerId = null;
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

    #region Save Revenue

    [RelayCommand]
    private void CloseAddEditModal()
    {
        IsAddEditModalOpen = false;
        ResetForm();
    }

    [RelayCommand]
    private void SaveRevenue()
    {
        // Validation
        ClearValidationErrors();

        if (LineItems.Count == 0)
        {
            ValidationMessage = "Please add at least one line item";
            HasValidationMessage = true;
            return;
        }

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        if (IsEditMode)
        {
            SaveEditedRevenue(companyData);
        }
        else
        {
            SaveNewRevenue(companyData);
        }

        CloseAddEditModal();
    }

    private void SaveNewRevenue(CompanyData companyData)
    {
        // Generate sale ID
        companyData.IdCounters.Sale++;
        var saleId = $"SAL-{DateTime.Now:yyyy}-{companyData.IdCounters.Sale:D5}";

        // Combine line items into description
        var description = LineItems.Count == 1
            ? LineItems[0].Description
            : string.Join(", ", LineItems.Select(li => li.Description).Where(d => !string.IsNullOrEmpty(d)));
        var totalQuantity = LineItems.Sum(li => li.Quantity);
        var averageUnitPrice = LineItems.Count > 0 ? LineItems.Average(li => li.UnitPrice) : 0;

        var sale = new Sale
        {
            Id = saleId,
            Date = ModalDate?.DateTime ?? DateTime.Now,
            CustomerId = SelectedCustomer?.Id,
            CategoryId = SelectedCategory?.Id,
            Description = description,
            Quantity = totalQuantity,
            UnitPrice = averageUnitPrice,
            Amount = Subtotal,
            TaxRate = ModalTaxRate,
            TaxAmount = TaxAmount,
            ShippingCost = ModalShipping,
            Discount = ModalDiscount,
            Total = Total,
            PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash,
            Notes = ModalNotes,
            ReferenceNumber = _receiptFilePath ?? string.Empty,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Create undo action
        var action = new RevenueAddAction(
            $"Add sale {saleId}",
            sale,
            () =>
            {
                companyData.Sales.Remove(sale);
                companyData.IdCounters.Sale--;
                RevenueSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Sales.Add(sale);
                companyData.IdCounters.Sale++;
                RevenueSaved?.Invoke(this, EventArgs.Empty);
            });

        // Add the sale
        companyData.Sales.Add(sale);

        // Record undo action and mark as changed
        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        RevenueSaved?.Invoke(this, EventArgs.Empty);
    }

    private void SaveEditedRevenue(CompanyData companyData)
    {
        var sale = companyData.Sales.FirstOrDefault(s => s.Id == _editingRevenueId);
        if (sale == null) return;

        // Store original values for undo
        var originalDate = sale.Date;
        var originalCustomerId = sale.CustomerId;
        var originalCategoryId = sale.CategoryId;
        var originalDescription = sale.Description;
        var originalQuantity = sale.Quantity;
        var originalUnitPrice = sale.UnitPrice;
        var originalAmount = sale.Amount;
        var originalTaxRate = sale.TaxRate;
        var originalTaxAmount = sale.TaxAmount;
        var originalShippingCost = sale.ShippingCost;
        var originalDiscount = sale.Discount;
        var originalTotal = sale.Total;
        var originalPaymentMethod = sale.PaymentMethod;
        var originalNotes = sale.Notes;
        var originalReferenceNumber = sale.ReferenceNumber;

        // Apply changes
        sale.Date = ModalDate?.DateTime ?? DateTime.Now;
        sale.CustomerId = SelectedCustomer?.Id;
        sale.CategoryId = SelectedCategory?.Id;
        sale.Description = ModalDescription;
        sale.Quantity = ModalQuantity;
        sale.UnitPrice = ModalUnitPrice;
        sale.Amount = Subtotal;
        sale.TaxRate = ModalTaxRate;
        sale.TaxAmount = TaxAmount;
        sale.ShippingCost = ModalShipping;
        sale.Discount = ModalDiscount;
        sale.Total = Total;
        sale.PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash;
        sale.Notes = ModalNotes;
        sale.ReferenceNumber = _receiptFilePath ?? string.Empty;
        sale.UpdatedAt = DateTime.Now;

        // Create undo action
        var action = new RevenueEditAction(
            $"Edit sale {_editingRevenueId}",
            sale,
            () =>
            {
                sale.Date = originalDate;
                sale.CustomerId = originalCustomerId;
                sale.CategoryId = originalCategoryId;
                sale.Description = originalDescription;
                sale.Quantity = originalQuantity;
                sale.UnitPrice = originalUnitPrice;
                sale.Amount = originalAmount;
                sale.TaxRate = originalTaxRate;
                sale.TaxAmount = originalTaxAmount;
                sale.ShippingCost = originalShippingCost;
                sale.Discount = originalDiscount;
                sale.Total = originalTotal;
                sale.PaymentMethod = originalPaymentMethod;
                sale.Notes = originalNotes;
                sale.ReferenceNumber = originalReferenceNumber;
                RevenueSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                sale.Date = ModalDate?.DateTime ?? DateTime.Now;
                sale.CustomerId = SelectedCustomer?.Id;
                sale.CategoryId = SelectedCategory?.Id;
                sale.Description = ModalDescription;
                sale.Quantity = ModalQuantity;
                sale.UnitPrice = ModalUnitPrice;
                sale.Amount = Subtotal;
                sale.TaxRate = ModalTaxRate;
                sale.TaxAmount = TaxAmount;
                sale.ShippingCost = ModalShipping;
                sale.Discount = ModalDiscount;
                sale.Total = Total;
                sale.PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash;
                sale.Notes = ModalNotes;
                sale.ReferenceNumber = _receiptFilePath ?? string.Empty;
                RevenueSaved?.Invoke(this, EventArgs.Empty);
            });

        // Record undo action and mark as changed
        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        RevenueSaved?.Invoke(this, EventArgs.Empty);
    }

    private void ResetForm()
    {
        _editingRevenueId = string.Empty;
        ModalDate = DateTimeOffset.Now;
        SelectedCustomer = null;
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
        // Add a default line item
        AddLineItem();
        ReceiptFileName = "No receipt attached";
        _receiptFilePath = null;
        ClearValidationErrors();
    }

    private void ClearValidationErrors()
    {
        HasCustomerError = false;
        HasCategoryError = false;
        HasDescriptionError = false;
        HasUnitPriceError = false;
        ValidationMessage = string.Empty;
        HasValidationMessage = false;
    }

    #endregion

    #region Navigation Commands

    [RelayCommand]
    private void NavigateToCreateCustomer()
    {
        IsAddEditModalOpen = false;
        App.NavigationService?.NavigateTo("Customers", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    [RelayCommand]
    private void NavigateToCreateCategory()
    {
        IsAddEditModalOpen = false;
        App.NavigationService?.NavigateTo("Categories", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    [RelayCommand]
    private void NavigateToCreateProduct()
    {
        IsAddEditModalOpen = false;
        App.NavigationService?.NavigateTo("Products", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    #endregion

    #region Line Items

    [RelayCommand]
    private void AddLineItem()
    {
        var lineItem = new RevenueLineItem();
        lineItem.PropertyChanged += (_, _) => UpdateTotals();
        LineItems.Add(lineItem);
        UpdateTotals();
    }

    [RelayCommand]
    private void RemoveLineItem(RevenueLineItem? item)
    {
        // Don't allow removing the last item
        if (item != null && LineItems.Count > 1)
        {
            LineItems.Remove(item);
            UpdateTotals();
        }
    }

    #endregion

    #region Receipt

    [RelayCommand]
    private async Task AttachReceipt()
    {
        var topLevel = App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select Receipt",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.pdf" } },
                new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            _receiptFilePath = file.Path.LocalPath;
            ReceiptFileName = file.Name;
        }
    }

    #endregion
}

/// <summary>
/// Line item for revenue/sale form.
/// </summary>
public partial class RevenueLineItem : ObservableObject
{
    [ObservableProperty]
    private ProductOption? _selectedProduct;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice;

    public decimal Amount => Quantity * UnitPrice;
    public string AmountFormatted => $"${Amount:N2}";

    partial void OnSelectedProductChanged(ProductOption? value)
    {
        if (value != null)
        {
            Description = value.Name;
            UnitPrice = value.UnitPrice;
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
