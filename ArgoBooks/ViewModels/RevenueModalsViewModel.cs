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
/// ViewModel for revenue modals (Add, Edit, Delete, Filter).
/// </summary>
public partial class RevenueModalsViewModel : ViewModelBase
{
    #region Events

    public event EventHandler? RevenueSaved;
    public event EventHandler? RevenueDeleted;
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
    private string _modalTitle = "Add Revenue";

    [ObservableProperty]
    private string _saveButtonText = "Add Revenue";

    #endregion

    #region Item Status Modal Fields

    private RevenueDisplayItem? _itemStatusItem;

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
        "Customer damaged",
        "Other"
    ];

    public ObservableCollection<string> ReturnReasonOptions { get; } =
    [
        "Customer return",
        "Wrong item sent",
        "Quality issues",
        "Not as described",
        "Changed mind",
        "Defective",
        "Other"
    ];

    public ObservableCollection<string> UndoReasonOptions { get; } =
    [
        "Item found",
        "Damage was repairable",
        "Customer changed mind",
        "Incorrect status",
        "Administrative error",
        "Other"
    ];

    [ObservableProperty]
    private ObservableCollection<string> _currentReasonOptions = [];

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
        LoadProductOptions();

        _editingRevenueId = sale.Id;
        IsEditMode = true;
        ModalTitle = $"Edit Sale {sale.Id}";
        SaveButtonText = "Save Changes";

        // Populate form
        ModalDate = new DateTimeOffset(sale.Date);
        SelectedCustomer = CustomerOptions.FirstOrDefault(c => c.Id == sale.CustomerId);
        SelectedCategory = CategoryOptions.FirstOrDefault(c => c.Id == sale.CategoryId);
        ModalTaxRate = sale.TaxAmount;  // Load tax as dollar amount
        ModalShipping = sale.ShippingCost;
        ModalDiscount = sale.Discount;
        SelectedPaymentMethod = sale.PaymentMethod.ToString();
        ModalNotes = sale.Notes;

        // Load line items from sale
        LineItems.Clear();
        if (sale.LineItems.Count > 0)
        {
            foreach (var li in sale.LineItems)
            {
                // Set SelectedProduct first so OnSelectedProductChanged fires before we set saved values
                var lineItem = new RevenueLineItem
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
            var lineItem = new RevenueLineItem
            {
                Description = sale.Description,
                Quantity = sale.Quantity,
                UnitPrice = sale.UnitPrice
            };
            lineItem.PropertyChanged += (_, _) => UpdateTotals();
            LineItems.Add(lineItem);
        }
        UpdateTotals();

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

        // Find and remove associated receipt
        Core.Models.Tracking.Receipt? deletedReceipt = null;
        if (!string.IsNullOrEmpty(sale.ReceiptId))
        {
            deletedReceipt = companyData.Receipts.FirstOrDefault(r => r.Id == sale.ReceiptId);
            if (deletedReceipt != null)
            {
                companyData.Receipts.Remove(deletedReceipt);
            }
        }

        // Create undo action
        var deletedSale = sale;
        var capturedReceipt = deletedReceipt;
        var action = new RevenueDeleteAction(
            $"Delete sale {sale.Id}",
            deletedSale,
            () =>
            {
                companyData.Sales.Add(deletedSale);
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Add(capturedReceipt);
                }
                RevenueDeleted?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Sales.Remove(deletedSale);
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Remove(capturedReceipt);
                }
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

    #region Item Status Modal

    public void OpenMarkAsLostDamagedModal(RevenueDisplayItem? item)
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

    public void OpenMarkAsReturnedModal(RevenueDisplayItem? item)
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

    public void OpenUndoLostDamagedModal(RevenueDisplayItem? item)
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

    public void OpenUndoReturnedModal(RevenueDisplayItem? item)
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

        var sale = companyData.Sales.FirstOrDefault(s => s.Id == _itemStatusItem.Id);
        if (sale == null)
        {
            CloseItemStatusModal();
            return;
        }

        switch (ItemStatusAction)
        {
            case "LostDamaged":
                CreateLostDamagedRecord(companyData, sale);
                break;
            case "Returned":
                CreateReturnRecord(companyData, sale);
                break;
            case "UndoLostDamaged":
                RemoveLostDamagedRecord(companyData, sale);
                break;
            case "UndoReturned":
                RemoveReturnRecord(companyData, sale);
                break;
        }

        App.CompanyManager?.MarkAsChanged();
        CloseItemStatusModal();

        // Notify that revenue data has changed
        RevenueSaved?.Invoke(this, EventArgs.Empty);
    }

    private void CreateLostDamagedRecord(Core.Data.CompanyData companyData, Core.Models.Transactions.Sale sale)
    {
        var reason = MapToLostDamagedReason(SelectedItemStatusReason ?? "Other");
        var productId = sale.LineItems.FirstOrDefault()?.ProductId ?? "";
        var valueLost = sale.Total;

        var lostDamaged = new Core.Models.Tracking.LostDamaged
        {
            Id = $"LOST-{++companyData.IdCounters.LostDamaged:D3}",
            ProductId = productId,
            InventoryItemId = sale.Id,
            Quantity = (int)sale.Quantity,
            Reason = reason,
            DateDiscovered = DateTime.UtcNow,
            ValueLost = valueLost,
            Notes = $"From sale {sale.Id}. {ItemStatusNotes}".Trim(),
            InsuranceClaim = false,
            CreatedAt = DateTime.UtcNow
        };

        companyData.LostDamaged.Add(lostDamaged);
    }

    private void CreateReturnRecord(Core.Data.CompanyData companyData, Core.Models.Transactions.Sale sale)
    {
        var productId = sale.LineItems.FirstOrDefault()?.ProductId ?? "";

        var returnRecord = new Core.Models.Tracking.Return
        {
            Id = $"RET-{++companyData.IdCounters.Return:D3}",
            OriginalTransactionId = sale.Id,
            ReturnType = "Customer",
            SupplierId = "",
            CustomerId = sale.CustomerId ?? "",
            ReturnDate = DateTime.UtcNow,
            Items =
            [
                new Core.Models.Common.ReturnItem
                {
                    ProductId = productId,
                    Quantity = (int)sale.Quantity,
                    Reason = SelectedItemStatusReason ?? "Other"
                }
            ],
            RefundAmount = sale.Total,
            RestockingFee = 0,
            Status = Core.Enums.ReturnStatus.Completed,
            Notes = ItemStatusNotes,
            ProcessedBy = sale.AccountantId ?? "",
            CreatedAt = DateTime.UtcNow
        };

        companyData.Returns.Add(returnRecord);
    }

    private static void RemoveLostDamagedRecord(Core.Data.CompanyData companyData, Core.Models.Transactions.Sale sale)
    {
        var record = companyData.LostDamaged.FirstOrDefault(ld => ld.InventoryItemId == sale.Id);
        if (record != null)
        {
            companyData.LostDamaged.Remove(record);
        }
    }

    private static void RemoveReturnRecord(Core.Data.CompanyData companyData, Core.Models.Transactions.Sale sale)
    {
        var record = companyData.Returns.FirstOrDefault(r => r.OriginalTransactionId == sale.Id);
        if (record != null)
        {
            companyData.Returns.Remove(record);
        }
    }

    private static Core.Enums.LostDamagedReason MapToLostDamagedReason(string reason)
    {
        return reason.ToLowerInvariant() switch
        {
            "damaged in transit" or "damaged during storage" or "defective product" or "customer damaged" => Core.Enums.LostDamagedReason.Damaged,
            "lost in warehouse" => Core.Enums.LostDamagedReason.Lost,
            _ => Core.Enums.LostDamagedReason.Other
        };
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

        var sale = new Sale
        {
            Id = saleId,
            Date = ModalDate?.DateTime ?? DateTime.Now,
            CustomerId = SelectedCustomer?.Id,
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
                TransactionId = saleId,
                TransactionType = "Revenue",
                FileName = fileInfo.Name,
                FileType = fileType,
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                FileData = fileData,
                OriginalFilePath = _receiptFilePath,
                Amount = Total,
                Date = ModalDate?.DateTime ?? DateTime.Now,
                Vendor = SelectedCustomer?.Name ?? "",
                Source = "Manual",
                CreatedAt = DateTime.Now
            };

            sale.ReceiptId = receiptId;
            companyData.Receipts.Add(receipt);
        }

        // Create undo action
        var capturedReceipt = receipt;
        var action = new RevenueAddAction(
            $"Add sale {saleId}",
            sale,
            () =>
            {
                companyData.Sales.Remove(sale);
                companyData.IdCounters.Sale--;
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Remove(capturedReceipt);
                    companyData.IdCounters.Receipt--;
                }
                RevenueSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Sales.Add(sale);
                companyData.IdCounters.Sale++;
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Add(capturedReceipt);
                    companyData.IdCounters.Receipt++;
                }
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
        var originalLineItems = sale.LineItems.ToList();
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
        var originalReceiptId = sale.ReceiptId;

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
        sale.Date = ModalDate?.DateTime ?? DateTime.Now;
        sale.CustomerId = SelectedCustomer?.Id;
        sale.CategoryId = SelectedCategory?.Id;
        sale.Description = description;
        sale.LineItems = modelLineItems;
        sale.Quantity = totalQuantity;
        sale.UnitPrice = averageUnitPrice;
        sale.Amount = Subtotal;
        sale.TaxRate = Subtotal > 0 ? (TaxAmount / Subtotal) * 100 : 0;  // Calculate percentage for records
        sale.TaxAmount = TaxAmount;  // Store entered dollar amount
        sale.ShippingCost = ModalShipping;
        sale.Discount = ModalDiscount;
        sale.Total = Total;
        sale.PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash;
        sale.Notes = ModalNotes;
        sale.ReferenceNumber = _receiptFilePath ?? string.Empty;
        sale.UpdatedAt = DateTime.Now;

        // Handle receipt - create new receipt if file was attached and sale doesn't have one
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
                TransactionId = sale.Id,
                TransactionType = "Revenue",
                FileName = fileInfo.Name,
                FileType = fileType,
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                Amount = Total,
                Date = ModalDate?.DateTime ?? DateTime.Now,
                Vendor = SelectedCustomer?.Name ?? "",
                Source = "Manual",
                CreatedAt = DateTime.Now
            };

            sale.ReceiptId = receiptId;
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

        var action = new RevenueEditAction(
            $"Edit sale {_editingRevenueId}",
            sale,
            () =>
            {
                sale.Date = originalDate;
                sale.CustomerId = originalCustomerId;
                sale.CategoryId = originalCategoryId;
                sale.Description = originalDescription;
                sale.LineItems = originalLineItems;
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
                sale.ReceiptId = originalReceiptId;
                if (capturedNewReceipt != null)
                {
                    companyData.Receipts.Remove(capturedNewReceipt);
                    companyData.IdCounters.Receipt--;
                }
                RevenueSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                sale.Date = ModalDate?.DateTime ?? DateTime.Now;
                sale.CustomerId = SelectedCustomer?.Id;
                sale.CategoryId = SelectedCategory?.Id;
                sale.Description = newDescription;
                sale.LineItems = newLineItems;
                sale.Quantity = newQuantity;
                sale.UnitPrice = newUnitPrice;
                sale.Amount = newAmount;
                sale.TaxRate = newTaxRate;
                sale.TaxAmount = newTaxAmount;
                sale.ShippingCost = newShippingCost;
                sale.Discount = newDiscount;
                sale.Total = newTotal;
                sale.PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash;
                sale.Notes = ModalNotes;
                sale.ReferenceNumber = _receiptFilePath ?? string.Empty;
                if (capturedNewReceipt != null)
                {
                    sale.ReceiptId = capturedNewReceipt.Id;
                    companyData.Receipts.Add(capturedNewReceipt);
                    companyData.IdCounters.Receipt++;
                }
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
        App.NavigationService?.NavigateTo("Categories", new Dictionary<string, object?> { { "openAddModal", true }, { "selectedTabIndex", 1 } });
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
