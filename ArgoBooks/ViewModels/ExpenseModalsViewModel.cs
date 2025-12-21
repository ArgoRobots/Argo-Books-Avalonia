using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for expense modals (Add, Edit, Delete, Filter).
/// </summary>
public partial class ExpenseModalsViewModel : ViewModelBase
{
    #region Events

    public event EventHandler? ExpenseSaved;
    public event EventHandler? ExpenseDeleted;
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
    private string _modalTitle = "Add Expense";

    [ObservableProperty]
    private string _saveButtonText = "Add Expense";

    #endregion

    #region Add/Edit Modal Fields

    private string _editingExpenseId = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _modalDate = DateTimeOffset.Now;

    [ObservableProperty]
    private SupplierOption? _selectedSupplier;

    [ObservableProperty]
    private bool _hasSupplierError;

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

    public ObservableCollection<SupplierOption> SupplierOptions { get; } = [];
    public ObservableCollection<CategoryOption> CategoryOptions { get; } = [];
    public ObservableCollection<ProductOption> ProductOptions { get; } = [];
    public ObservableCollection<string> PaymentMethodOptions { get; } = ["Cash", "Credit Card", "Debit Card", "Bank Transfer", "Check", "PayPal", "Other"];
    public ObservableCollection<ExpenseLineItem> LineItems { get; } = [];

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

    private string _deleteExpenseIdInternal = string.Empty;

    [ObservableProperty]
    private string _deleteExpenseId = string.Empty;

    [ObservableProperty]
    private string _deleteExpenseDescription = string.Empty;

    [ObservableProperty]
    private string _deleteExpenseAmount = string.Empty;

    #endregion

    #region Filter Modal Fields

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private SupplierOption? _filterSelectedSupplier;

    [ObservableProperty]
    private string? _filterSupplierId;

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

    [ObservableProperty]
    private string _filterReceiptStatus = "All";

    public ObservableCollection<string> StatusFilterOptions { get; } = ["All", "Completed", "Pending", "Partial Return", "Returned", "Cancelled"];
    public ObservableCollection<string> ReceiptFilterOptions { get; } = ["All", "With Receipt", "No Receipt"];

    #endregion

    #region Constructor

    public ExpenseModalsViewModel()
    {
        LoadSupplierOptions();
        LoadCategoryOptions();
        LoadProductOptions();
    }

    private void LoadSupplierOptions()
    {
        SupplierOptions.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Suppliers == null)
            return;

        foreach (var supplier in companyData.Suppliers.OrderBy(s => s.Name))
        {
            SupplierOptions.Add(new SupplierOption { Id = supplier.Id, Name = supplier.Name });
        }
    }

    private void LoadCategoryOptions()
    {
        CategoryOptions.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Categories == null)
            return;

        var purchaseCategories = companyData.Categories
            .Where(c => c.Type == CategoryType.Purchase)
            .OrderBy(c => c.Name);

        foreach (var category in purchaseCategories)
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
                UnitPrice = product.CostPrice  // Use cost price for expenses
            });
        }
    }

    #endregion

    #region Add Modal

    [RelayCommand]
    public void OpenAddModal()
    {
        LoadSupplierOptions();
        LoadCategoryOptions();
        LoadProductOptions();
        ResetForm();
        IsEditMode = false;
        ModalTitle = "Add Expense";
        SaveButtonText = "Add Expense";
        IsAddEditModalOpen = true;
    }

    #endregion

    #region Edit Modal

    public void OpenEditModal(ExpenseDisplayItem? item)
    {
        if (item == null) return;

        var expense = App.CompanyManager?.CompanyData?.Purchases?.FirstOrDefault(p => p.Id == item.Id);
        if (expense == null) return;

        LoadSupplierOptions();
        LoadCategoryOptions();

        _editingExpenseId = expense.Id;
        IsEditMode = true;
        ModalTitle = $"Edit Expense {expense.Id}";
        SaveButtonText = "Save Changes";

        // Populate form
        ModalDate = new DateTimeOffset(expense.Date);
        SelectedSupplier = SupplierOptions.FirstOrDefault(s => s.Id == expense.SupplierId);
        SelectedCategory = CategoryOptions.FirstOrDefault(c => c.Id == expense.CategoryId);
        ModalDescription = expense.Description;
        ModalQuantity = expense.Quantity;
        ModalUnitPrice = expense.UnitPrice;
        ModalTaxRate = expense.TaxRate;
        ModalShipping = expense.ShippingCost;
        ModalDiscount = expense.Discount;
        SelectedPaymentMethod = expense.PaymentMethod.ToString();
        ModalNotes = expense.Notes;
        ModalReferenceNumber = expense.ReferenceNumber;

        ClearValidationErrors();
        IsAddEditModalOpen = true;
    }

    #endregion

    #region Delete Confirmation

    public void OpenDeleteConfirm(ExpenseDisplayItem? item)
    {
        if (item == null) return;

        _deleteExpenseIdInternal = item.Id;
        DeleteExpenseId = item.Id;
        DeleteExpenseDescription = item.ProductDescription;
        DeleteExpenseAmount = item.TotalFormatted;
        IsDeleteConfirmOpen = true;
    }

    [RelayCommand]
    private void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deleteExpenseIdInternal = string.Empty;
        DeleteExpenseId = string.Empty;
        DeleteExpenseDescription = string.Empty;
        DeleteExpenseAmount = string.Empty;
    }

    [RelayCommand]
    private void DeleteExpense()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Purchases == null) return;

        var expense = companyData.Purchases.FirstOrDefault(p => p.Id == _deleteExpenseIdInternal);
        if (expense == null) return;

        // Create undo action
        var deletedExpense = expense;
        var action = new ExpenseDeleteAction(
            $"Delete expense {expense.Id}",
            deletedExpense,
            () =>
            {
                companyData.Purchases.Add(deletedExpense);
                ExpenseDeleted?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Purchases.Remove(deletedExpense);
                ExpenseDeleted?.Invoke(this, EventArgs.Empty);
            });

        // Remove the expense
        companyData.Purchases.Remove(expense);

        // Record undo action and mark as changed
        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        ExpenseDeleted?.Invoke(this, EventArgs.Empty);
        CloseDeleteConfirm();
    }

    #endregion

    #region Filter Modal

    public void OpenFilterModal()
    {
        // Load options for filter dropdowns with "All" option
        LoadSupplierOptionsForFilter();
        LoadCategoryOptionsForFilter();
        IsFilterModalOpen = true;
    }

    private void LoadSupplierOptionsForFilter()
    {
        SupplierOptions.Clear();
        SupplierOptions.Add(new SupplierOption { Id = null, Name = "All Suppliers" });

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Suppliers == null)
            return;

        foreach (var supplier in companyData.Suppliers.OrderBy(s => s.Name))
        {
            SupplierOptions.Add(new SupplierOption { Id = supplier.Id, Name = supplier.Name });
        }
    }

    private void LoadCategoryOptionsForFilter()
    {
        CategoryOptions.Clear();
        CategoryOptions.Add(new CategoryOption { Id = null, Name = "All Categories" });

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Categories == null)
            return;

        var purchaseCategories = companyData.Categories
            .Where(c => c.Type == CategoryType.Purchase)
            .OrderBy(c => c.Name);

        foreach (var category in purchaseCategories)
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
        FilterSupplierId = FilterSelectedSupplier?.Id;
        FilterCategoryId = FilterSelectedCategory?.Id;
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterStatus = "All";
        FilterSelectedSupplier = null;
        FilterSupplierId = null;
        FilterSelectedCategory = null;
        FilterCategoryId = null;
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterDateFrom = null;
        FilterDateTo = null;
        FilterReceiptStatus = "All";
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    #endregion

    #region Save Expense

    [RelayCommand]
    private void CloseAddEditModal()
    {
        IsAddEditModalOpen = false;
        ResetForm();
    }

    [RelayCommand]
    private void SaveExpense()
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
            SaveEditedExpense(companyData);
        }
        else
        {
            SaveNewExpense(companyData);
        }

        CloseAddEditModal();
    }

    private void SaveNewExpense(CompanyData companyData)
    {
        // Generate expense ID
        companyData.IdCounters.Purchase++;
        var expenseId = $"PUR-{DateTime.Now:yyyy}-{companyData.IdCounters.Purchase:D5}";

        // Combine line items into description
        var description = LineItems.Count == 1
            ? LineItems[0].Description
            : string.Join(", ", LineItems.Select(li => li.Description).Where(d => !string.IsNullOrEmpty(d)));
        var totalQuantity = LineItems.Sum(li => li.Quantity);
        var averageUnitPrice = LineItems.Count > 0 ? LineItems.Average(li => li.UnitPrice) : 0;

        var expense = new Purchase
        {
            Id = expenseId,
            Date = ModalDate?.DateTime ?? DateTime.Now,
            SupplierId = SelectedSupplier?.Id,
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
        var action = new ExpenseAddAction(
            $"Add expense {expenseId}",
            expense,
            () =>
            {
                companyData.Purchases.Remove(expense);
                companyData.IdCounters.Purchase--;
                ExpenseSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Purchases.Add(expense);
                companyData.IdCounters.Purchase++;
                ExpenseSaved?.Invoke(this, EventArgs.Empty);
            });

        // Add the expense
        companyData.Purchases.Add(expense);

        // Record undo action and mark as changed
        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        ExpenseSaved?.Invoke(this, EventArgs.Empty);
    }

    private void SaveEditedExpense(CompanyData companyData)
    {
        var expense = companyData.Purchases.FirstOrDefault(p => p.Id == _editingExpenseId);
        if (expense == null) return;

        // Store original values for undo
        var originalDate = expense.Date;
        var originalSupplierId = expense.SupplierId;
        var originalCategoryId = expense.CategoryId;
        var originalDescription = expense.Description;
        var originalQuantity = expense.Quantity;
        var originalUnitPrice = expense.UnitPrice;
        var originalAmount = expense.Amount;
        var originalTaxRate = expense.TaxRate;
        var originalTaxAmount = expense.TaxAmount;
        var originalShippingCost = expense.ShippingCost;
        var originalDiscount = expense.Discount;
        var originalTotal = expense.Total;
        var originalPaymentMethod = expense.PaymentMethod;
        var originalNotes = expense.Notes;
        var originalReferenceNumber = expense.ReferenceNumber;

        // Apply changes
        expense.Date = ModalDate?.DateTime ?? DateTime.Now;
        expense.SupplierId = SelectedSupplier?.Id;
        expense.CategoryId = SelectedCategory?.Id;
        expense.Description = ModalDescription;
        expense.Quantity = ModalQuantity;
        expense.UnitPrice = ModalUnitPrice;
        expense.Amount = Subtotal;
        expense.TaxRate = ModalTaxRate;
        expense.TaxAmount = TaxAmount;
        expense.ShippingCost = ModalShipping;
        expense.Discount = ModalDiscount;
        expense.Total = Total;
        expense.PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash;
        expense.Notes = ModalNotes;
        expense.ReferenceNumber = ModalReferenceNumber;
        expense.UpdatedAt = DateTime.Now;

        // Create undo action
        var action = new ExpenseEditAction(
            $"Edit expense {_editingExpenseId}",
            expense,
            () =>
            {
                expense.Date = originalDate;
                expense.SupplierId = originalSupplierId;
                expense.CategoryId = originalCategoryId;
                expense.Description = originalDescription;
                expense.Quantity = originalQuantity;
                expense.UnitPrice = originalUnitPrice;
                expense.Amount = originalAmount;
                expense.TaxRate = originalTaxRate;
                expense.TaxAmount = originalTaxAmount;
                expense.ShippingCost = originalShippingCost;
                expense.Discount = originalDiscount;
                expense.Total = originalTotal;
                expense.PaymentMethod = originalPaymentMethod;
                expense.Notes = originalNotes;
                expense.ReferenceNumber = originalReferenceNumber;
                ExpenseSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                expense.Date = ModalDate?.DateTime ?? DateTime.Now;
                expense.SupplierId = SelectedSupplier?.Id;
                expense.CategoryId = SelectedCategory?.Id;
                expense.Description = ModalDescription;
                expense.Quantity = ModalQuantity;
                expense.UnitPrice = ModalUnitPrice;
                expense.Amount = Subtotal;
                expense.TaxRate = ModalTaxRate;
                expense.TaxAmount = TaxAmount;
                expense.ShippingCost = ModalShipping;
                expense.Discount = ModalDiscount;
                expense.Total = Total;
                expense.PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash;
                expense.Notes = ModalNotes;
                expense.ReferenceNumber = ModalReferenceNumber;
                ExpenseSaved?.Invoke(this, EventArgs.Empty);
            });

        // Record undo action and mark as changed
        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        ExpenseSaved?.Invoke(this, EventArgs.Empty);
    }

    private void ResetForm()
    {
        _editingExpenseId = string.Empty;
        ModalDate = DateTimeOffset.Now;
        SelectedSupplier = null;
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
        ReceiptFileName = "No receipt attached";
        _receiptFilePath = null;
        ClearValidationErrors();
    }

    private void ClearValidationErrors()
    {
        HasSupplierError = false;
        HasCategoryError = false;
        HasDescriptionError = false;
        HasUnitPriceError = false;
        ValidationMessage = string.Empty;
        HasValidationMessage = false;
    }

    #endregion

    #region Navigation Commands

    [RelayCommand]
    private void NavigateToCreateSupplier()
    {
        IsAddEditModalOpen = false;
        App.NavigationService?.NavigateTo("Suppliers", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    [RelayCommand]
    private void NavigateToCreateCategory()
    {
        IsAddEditModalOpen = false;
        App.NavigationService?.NavigateTo("Categories", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    #endregion

    #region Line Items

    [RelayCommand]
    private void AddLineItem()
    {
        var lineItem = new ExpenseLineItem();
        lineItem.PropertyChanged += (_, _) => UpdateTotals();
        LineItems.Add(lineItem);
        UpdateTotals();
    }

    [RelayCommand]
    private void RemoveLineItem(ExpenseLineItem? item)
    {
        if (item != null)
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
/// Line item for expense form.
/// </summary>
public partial class ExpenseLineItem : ObservableObject
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

/// <summary>
/// Option for product selection.
/// </summary>
public class ProductOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}
