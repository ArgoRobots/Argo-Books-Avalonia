using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
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
    public event EventHandler? ScrollToLineItemsRequested;

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
    private string _modalTitle = "Add Expense";

    [ObservableProperty]
    private string _saveButtonText = "Add Expense";

    #endregion

    #region Item Status Modal Fields

    private ExpenseDisplayItem? _itemStatusItem;

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

    public ObservableCollection<string> LostDamagedReasonOptions { get; } =
    [
        "Damaged in transit",
        "Defective product",
        "Lost in warehouse",
        "Damaged during storage",
        "Expired",
        "Other"
    ];

    public ObservableCollection<string> ReturnReasonOptions { get; } =
    [
        "Wrong item received",
        "Quality issues",
        "Not as described",
        "Duplicate order",
        "Changed mind",
        "Better price elsewhere",
        "Other"
    ];

    public ObservableCollection<string> UndoReasonOptions { get; } =
    [
        "Item found",
        "Damage was repairable",
        "Incorrect status",
        "Administrative error",
        "Other"
    ];

    [ObservableProperty]
    private ObservableCollection<string> _currentReasonOptions = [];

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
    public ObservableCollection<string> PaymentMethodOptions { get; } = ["Cash", "Bank Card", "Bank Transfer", "Check", "PayPal", "Other"];
    public ObservableCollection<ExpenseLineItem> LineItems { get; } = [];

    // Computed totals from line items
    public decimal Subtotal => LineItems.Sum(li => li.Amount);
    public decimal TaxAmount => ModalTaxRate;  // Tax is entered as dollar amount
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
        LoadProductOptions();

        _editingExpenseId = expense.Id;
        IsEditMode = true;
        ModalTitle = $"Edit Expense {expense.Id}";
        SaveButtonText = "Save Changes";

        // Populate form
        ModalDate = new DateTimeOffset(expense.Date);
        SelectedSupplier = SupplierOptions.FirstOrDefault(s => s.Id == expense.SupplierId);
        SelectedCategory = CategoryOptions.FirstOrDefault(c => c.Id == expense.CategoryId);
        ModalTaxRate = expense.TaxAmount;  // Load tax as dollar amount
        ModalShipping = expense.ShippingCost;
        ModalDiscount = expense.Discount;
        SelectedPaymentMethod = expense.PaymentMethod.ToString();
        ModalNotes = expense.Notes;

        // Load line items from expense
        LineItems.Clear();
        if (expense.LineItems.Count > 0)
        {
            foreach (var li in expense.LineItems)
            {
                // Set SelectedProduct first so OnSelectedProductChanged fires before we set saved values
                var lineItem = new ExpenseLineItem
                {
                    SelectedProduct = ProductOptions.FirstOrDefault(p => p.Id == li.ProductId)
                };
                // Now override with saved values (after product handler has fired)
                lineItem.Description = li.Description;
                lineItem.Quantity = li.Quantity;
                lineItem.UnitPrice = li.UnitPrice;
                lineItem.PropertyChanged += (_, _) => UpdateTotals();
                LineItems.Add(lineItem);
            }
        }
        else
        {
            // Fallback for old data without line items
            var lineItem = new ExpenseLineItem
            {
                Description = expense.Description,
                Quantity = expense.Quantity,
                UnitPrice = expense.UnitPrice
            };
            lineItem.PropertyChanged += (_, _) => UpdateTotals();
            LineItems.Add(lineItem);
        }
        UpdateTotals();

        // Load receipt info if available
        _receiptFilePath = expense.ReferenceNumber;
        ReceiptFileName = string.IsNullOrEmpty(expense.ReferenceNumber)
            ? "No receipt attached"
            : System.IO.Path.GetFileName(expense.ReferenceNumber);

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

        // Find and remove associated receipt
        Core.Models.Tracking.Receipt? deletedReceipt = null;
        if (!string.IsNullOrEmpty(expense.ReceiptId))
        {
            deletedReceipt = companyData.Receipts.FirstOrDefault(r => r.Id == expense.ReceiptId);
            if (deletedReceipt != null)
            {
                companyData.Receipts.Remove(deletedReceipt);
            }
        }

        // Create undo action
        var deletedExpense = expense;
        var capturedReceipt = deletedReceipt;
        var action = new ExpenseDeleteAction(
            $"Delete expense {expense.Id}",
            deletedExpense,
            () =>
            {
                companyData.Purchases.Add(deletedExpense);
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Add(capturedReceipt);
                }
                ExpenseDeleted?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Purchases.Remove(deletedExpense);
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Remove(capturedReceipt);
                }
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

    #region Item Status Modal

    public void OpenMarkAsLostDamagedModal(ExpenseDisplayItem? item)
    {
        if (item == null) return;

        _itemStatusItem = item;
        ItemStatusAction = "LostDamaged";
        ItemStatusModalTitle = "Mark as Lost/Damaged";
        ItemStatusItemDescription = $"{item.Id} - {item.ProductDescription}";
        ItemStatusSaveButtonText = "Mark as Lost/Damaged";
        IsUndoAction = false;
        CurrentReasonOptions = new ObservableCollection<string>(LostDamagedReasonOptions);
        SelectedItemStatusReason = null;
        ItemStatusNotes = string.Empty;
        IsItemStatusModalOpen = true;
    }

    public void OpenMarkAsReturnedModal(ExpenseDisplayItem? item)
    {
        if (item == null) return;

        _itemStatusItem = item;
        ItemStatusAction = "Returned";
        ItemStatusModalTitle = "Mark as Returned";
        ItemStatusItemDescription = $"{item.Id} - {item.ProductDescription}";
        ItemStatusSaveButtonText = "Mark as Returned";
        IsUndoAction = false;
        CurrentReasonOptions = new ObservableCollection<string>(ReturnReasonOptions);
        SelectedItemStatusReason = null;
        ItemStatusNotes = string.Empty;
        IsItemStatusModalOpen = true;
    }

    public void OpenUndoLostDamagedModal(ExpenseDisplayItem? item)
    {
        if (item == null) return;

        _itemStatusItem = item;
        ItemStatusAction = "UndoLostDamaged";
        ItemStatusModalTitle = "Undo Lost/Damaged Status";
        ItemStatusItemDescription = $"{item.Id} - {item.ProductDescription}";
        ItemStatusSaveButtonText = "Undo Status";
        IsUndoAction = true;
        CurrentReasonOptions = new ObservableCollection<string>(UndoReasonOptions);
        SelectedItemStatusReason = null;
        ItemStatusNotes = string.Empty;
        IsItemStatusModalOpen = true;
    }

    public void OpenUndoReturnedModal(ExpenseDisplayItem? item)
    {
        if (item == null) return;

        _itemStatusItem = item;
        ItemStatusAction = "UndoReturned";
        ItemStatusModalTitle = "Undo Returned Status";
        ItemStatusItemDescription = $"{item.Id} - {item.ProductDescription}";
        ItemStatusSaveButtonText = "Undo Status";
        IsUndoAction = true;
        CurrentReasonOptions = new ObservableCollection<string>(UndoReasonOptions);
        SelectedItemStatusReason = null;
        ItemStatusNotes = string.Empty;
        IsItemStatusModalOpen = true;
    }

    [RelayCommand]
    private void CloseItemStatusModal()
    {
        IsItemStatusModalOpen = false;
        _itemStatusItem = null;
        ItemStatusAction = string.Empty;
        SelectedItemStatusReason = null;
        ItemStatusNotes = string.Empty;
        HasItemStatusReasonError = false;
        ItemStatusReasonErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void ConfirmItemStatus()
    {
        if (_itemStatusItem == null)
        {
            CloseItemStatusModal();
            return;
        }

        // Validate reason is selected
        if (string.IsNullOrEmpty(SelectedItemStatusReason))
        {
            HasItemStatusReasonError = true;
            ItemStatusReasonErrorMessage = "Please select a reason";
            return;
        }

        HasItemStatusReasonError = false;
        ItemStatusReasonErrorMessage = string.Empty;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
        {
            CloseItemStatusModal();
            return;
        }

        var purchase = companyData.Purchases.FirstOrDefault(p => p.Id == _itemStatusItem.Id);
        if (purchase == null)
        {
            CloseItemStatusModal();
            return;
        }

        switch (ItemStatusAction)
        {
            case "LostDamaged":
                CreateLostDamagedRecord(companyData, purchase);
                break;
            case "Returned":
                CreateReturnRecord(companyData, purchase);
                break;
            case "UndoLostDamaged":
                RemoveLostDamagedRecord(companyData, purchase);
                break;
            case "UndoReturned":
                RemoveReturnRecord(companyData, purchase);
                break;
        }

        App.CompanyManager?.MarkAsChanged();
        CloseItemStatusModal();

        // Notify that expense data has changed
        ExpenseSaved?.Invoke(this, EventArgs.Empty);
    }

    private void CreateLostDamagedRecord(Core.Data.CompanyData companyData, Core.Models.Transactions.Purchase purchase)
    {
        var reason = MapToLostDamagedReason(SelectedItemStatusReason ?? "Other");
        var productId = purchase.LineItems.FirstOrDefault()?.ProductId ?? "";
        var valueLost = purchase.Total;

        var lostDamaged = new Core.Models.Tracking.LostDamaged
        {
            Id = $"LOST-{++companyData.IdCounters.LostDamaged:D3}",
            ProductId = productId,
            InventoryItemId = purchase.Id,
            Quantity = (int)purchase.Quantity,
            Reason = reason,
            DateDiscovered = DateTime.UtcNow,
            ValueLost = valueLost,
            Notes = $"From purchase {purchase.Id}. {ItemStatusNotes}".Trim(),
            InsuranceClaim = false,
            CreatedAt = DateTime.UtcNow
        };

        companyData.LostDamaged.Add(lostDamaged);
    }

    private void CreateReturnRecord(Core.Data.CompanyData companyData, Core.Models.Transactions.Purchase purchase)
    {
        var productId = purchase.LineItems.FirstOrDefault()?.ProductId ?? "";

        var returnRecord = new Core.Models.Tracking.Return
        {
            Id = $"RET-{++companyData.IdCounters.Return:D3}",
            OriginalTransactionId = purchase.Id,
            ReturnType = "Expense",
            SupplierId = purchase.SupplierId ?? "",
            CustomerId = "",
            ReturnDate = DateTime.UtcNow,
            Items =
            [
                new Core.Models.Common.ReturnItem
                {
                    ProductId = productId,
                    Quantity = (int)purchase.Quantity,
                    Reason = SelectedItemStatusReason ?? "Other"
                }
            ],
            RefundAmount = purchase.Total,
            RestockingFee = 0,
            Status = Core.Enums.ReturnStatus.Completed,
            Notes = ItemStatusNotes,
            ProcessedBy = purchase.AccountantId ?? "",
            CreatedAt = DateTime.UtcNow
        };

        companyData.Returns.Add(returnRecord);
    }

    private static void RemoveLostDamagedRecord(Core.Data.CompanyData companyData, Core.Models.Transactions.Purchase purchase)
    {
        var record = companyData.LostDamaged.FirstOrDefault(ld => ld.InventoryItemId == purchase.Id);
        if (record != null)
        {
            companyData.LostDamaged.Remove(record);
        }
    }

    private static void RemoveReturnRecord(Core.Data.CompanyData companyData, Core.Models.Transactions.Purchase purchase)
    {
        var record = companyData.Returns.FirstOrDefault(r => r.OriginalTransactionId == purchase.Id);
        if (record != null)
        {
            companyData.Returns.Remove(record);
        }
    }

    private static Core.Enums.LostDamagedReason MapToLostDamagedReason(string reason)
    {
        return reason.ToLowerInvariant() switch
        {
            "damaged in transit" or "damaged during storage" or "defective product" => Core.Enums.LostDamagedReason.Damaged,
            "lost in warehouse" => Core.Enums.LostDamagedReason.Lost,
            "expired" => Core.Enums.LostDamagedReason.Expired,
            _ => Core.Enums.LostDamagedReason.Other
        };
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

        // Create description from line items for display purposes
        var description = LineItems.Count == 1
            ? LineItems[0].Description
            : string.Join(", ", LineItems.Select(li => li.Description).Where(d => !string.IsNullOrEmpty(d)));
        var totalQuantity = LineItems.Sum(li => li.Quantity);
        var averageUnitPrice = LineItems.Count > 0 ? LineItems.Average(li => li.UnitPrice) : 0;

        // Convert UI line items to model line items
        var modelLineItems = LineItems.Select(li => new LineItem
        {
            ProductId = li.SelectedProduct?.Id,
            Description = li.Description,
            Quantity = li.Quantity,
            UnitPrice = li.UnitPrice,
            TaxRate = 0,  // Tax applied at transaction level
            Discount = 0  // Discount applied at transaction level
        }).ToList();

        var expense = new Purchase
        {
            Id = expenseId,
            Date = ModalDate?.DateTime ?? DateTime.Now,
            SupplierId = SelectedSupplier?.Id,
            CategoryId = SelectedCategory?.Id,
            Description = description,
            LineItems = modelLineItems,
            Quantity = totalQuantity,
            UnitPrice = averageUnitPrice,
            Amount = Subtotal,
            TaxRate = Subtotal > 0 ? (TaxAmount / Subtotal) * 100 : 0,  // Calculate percentage for records
            TaxAmount = TaxAmount,  // Store entered dollar amount
            ShippingCost = ModalShipping,
            Discount = ModalDiscount,
            Total = Total,
            PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash,
            Notes = ModalNotes,
            ReferenceNumber = _receiptFilePath ?? string.Empty,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Create Receipt if file was attached
        Core.Models.Tracking.Receipt? receipt = null;
        if (!string.IsNullOrEmpty(_receiptFilePath))
        {
            companyData.IdCounters.Receipt++;
            var receiptId = $"RCP-{DateTime.Now:yyyy}-{companyData.IdCounters.Receipt:D5}";

            var fileInfo = new System.IO.FileInfo(_receiptFilePath);
            var fileType = System.IO.Path.GetExtension(_receiptFilePath).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

            // Read file contents and store as Base64
            string? fileData = null;
            if (fileInfo.Exists)
            {
                try
                {
                    var bytes = System.IO.File.ReadAllBytes(_receiptFilePath);
                    fileData = Convert.ToBase64String(bytes);
                }
                catch
                {
                    // Failed to read file, continue without file data
                }
            }

            receipt = new Core.Models.Tracking.Receipt
            {
                Id = receiptId,
                TransactionId = expenseId,
                TransactionType = "Expense",
                FileName = fileInfo.Name,
                FileType = fileType,
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                FileData = fileData,
                OriginalFilePath = _receiptFilePath,
                Amount = Total,
                Date = ModalDate?.DateTime ?? DateTime.Now,
                Vendor = SelectedSupplier?.Name ?? "",
                Source = "Manual",
                CreatedAt = DateTime.Now
            };

            expense.ReceiptId = receiptId;
            companyData.Receipts.Add(receipt);
        }

        // Create undo action
        var capturedReceipt = receipt;
        var action = new ExpenseAddAction(
            $"Add expense {expenseId}",
            expense,
            () =>
            {
                companyData.Purchases.Remove(expense);
                companyData.IdCounters.Purchase--;
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Remove(capturedReceipt);
                    companyData.IdCounters.Receipt--;
                }
                ExpenseSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Purchases.Add(expense);
                companyData.IdCounters.Purchase++;
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Add(capturedReceipt);
                    companyData.IdCounters.Receipt++;
                }
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
        var originalLineItems = expense.LineItems.ToList();
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
        var originalReceiptId = expense.ReceiptId;

        // Create description and line items from UI
        var description = LineItems.Count == 1
            ? LineItems[0].Description
            : string.Join(", ", LineItems.Select(li => li.Description).Where(d => !string.IsNullOrEmpty(d)));
        var totalQuantity = LineItems.Sum(li => li.Quantity);
        var averageUnitPrice = LineItems.Count > 0 ? LineItems.Average(li => li.UnitPrice) : 0;

        var modelLineItems = LineItems.Select(li => new LineItem
        {
            ProductId = li.SelectedProduct?.Id,
            Description = li.Description,
            Quantity = li.Quantity,
            UnitPrice = li.UnitPrice,
            TaxRate = 0,
            Discount = 0
        }).ToList();

        // Apply changes
        expense.Date = ModalDate?.DateTime ?? DateTime.Now;
        expense.SupplierId = SelectedSupplier?.Id;
        expense.CategoryId = SelectedCategory?.Id;
        expense.Description = description;
        expense.LineItems = modelLineItems;
        expense.Quantity = totalQuantity;
        expense.UnitPrice = averageUnitPrice;
        expense.Amount = Subtotal;
        expense.TaxRate = Subtotal > 0 ? (TaxAmount / Subtotal) * 100 : 0;  // Calculate percentage for records
        expense.TaxAmount = TaxAmount;  // Store entered dollar amount
        expense.ShippingCost = ModalShipping;
        expense.Discount = ModalDiscount;
        expense.Total = Total;
        expense.PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash;
        expense.Notes = ModalNotes;
        expense.ReferenceNumber = _receiptFilePath ?? string.Empty;
        expense.UpdatedAt = DateTime.Now;

        // Handle receipt - create new receipt if file was attached and expense doesn't have one
        Core.Models.Tracking.Receipt? newReceipt = null;
        if (!string.IsNullOrEmpty(_receiptFilePath) && string.IsNullOrEmpty(originalReceiptId))
        {
            companyData.IdCounters.Receipt++;
            var receiptId = $"RCP-{DateTime.Now:yyyy}-{companyData.IdCounters.Receipt:D5}";

            var fileInfo = new System.IO.FileInfo(_receiptFilePath);
            var fileType = System.IO.Path.GetExtension(_receiptFilePath).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

            newReceipt = new Core.Models.Tracking.Receipt
            {
                Id = receiptId,
                TransactionId = expense.Id,
                TransactionType = "Expense",
                FileName = fileInfo.Name,
                FileType = fileType,
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                Amount = Total,
                Date = ModalDate?.DateTime ?? DateTime.Now,
                Vendor = SelectedSupplier?.Name ?? "",
                Source = "Manual",
                CreatedAt = DateTime.Now
            };

            expense.ReceiptId = receiptId;
            companyData.Receipts.Add(newReceipt);
        }
        else if (!string.IsNullOrEmpty(_receiptFilePath) && !string.IsNullOrEmpty(originalReceiptId))
        {
            // Update existing receipt's file reference
            var existingReceipt = companyData.Receipts.FirstOrDefault(r => r.Id == originalReceiptId);
            if (existingReceipt != null)
            {
                var fileInfo = new System.IO.FileInfo(_receiptFilePath);
                existingReceipt.FileName = fileInfo.Name;
                existingReceipt.FileSize = fileInfo.Exists ? fileInfo.Length : 0;
            }
        }

        // Create undo action (capture current values for redo)
        var newDescription = description;
        var newLineItems = modelLineItems;
        var newQuantity = totalQuantity;
        var newUnitPrice = averageUnitPrice;
        var newAmount = Subtotal;
        var newTaxRate = ModalTaxRate;
        var newTaxAmount = TaxAmount;
        var newShippingCost = ModalShipping;
        var newDiscount = ModalDiscount;
        var newTotal = Total;
        var capturedNewReceipt = newReceipt;

        var action = new ExpenseEditAction(
            $"Edit expense {_editingExpenseId}",
            expense,
            () =>
            {
                expense.Date = originalDate;
                expense.SupplierId = originalSupplierId;
                expense.CategoryId = originalCategoryId;
                expense.Description = originalDescription;
                expense.LineItems = originalLineItems;
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
                expense.ReceiptId = originalReceiptId;
                if (capturedNewReceipt != null)
                {
                    companyData.Receipts.Remove(capturedNewReceipt);
                    companyData.IdCounters.Receipt--;
                }
                ExpenseSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                expense.Date = ModalDate?.DateTime ?? DateTime.Now;
                expense.SupplierId = SelectedSupplier?.Id;
                expense.CategoryId = SelectedCategory?.Id;
                expense.Description = newDescription;
                expense.LineItems = newLineItems;
                expense.Quantity = newQuantity;
                expense.UnitPrice = newUnitPrice;
                expense.Amount = newAmount;
                expense.TaxRate = newTaxRate;
                expense.TaxAmount = newTaxAmount;
                expense.ShippingCost = newShippingCost;
                expense.Discount = newDiscount;
                expense.Total = newTotal;
                expense.PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash;
                expense.Notes = ModalNotes;
                expense.ReferenceNumber = _receiptFilePath ?? string.Empty;
                if (capturedNewReceipt != null)
                {
                    expense.ReceiptId = capturedNewReceipt.Id;
                    companyData.Receipts.Add(capturedNewReceipt);
                    companyData.IdCounters.Receipt++;
                }
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
        // Add a default line item
        AddLineItem();
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
        App.NavigationService?.NavigateTo("Categories", new Dictionary<string, object?> { { "openAddModal", true }, { "selectedTabIndex", 0 } });
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
        var lineItem = new ExpenseLineItem();
        lineItem.PropertyChanged += (_, _) => UpdateTotals();
        LineItems.Add(lineItem);
        UpdateTotals();
    }

    [RelayCommand]
    private void RemoveLineItem(ExpenseLineItem? item)
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
/// Option for product selection.
/// </summary>
public class ProductOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }

    public override string ToString() => Name;
}
