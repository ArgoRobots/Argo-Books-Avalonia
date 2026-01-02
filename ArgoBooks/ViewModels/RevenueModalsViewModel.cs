using ArgoBooks.Services;
using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for revenue modals (Add, Edit, Delete, Filter).
/// </summary>
public partial class RevenueModalsViewModel : TransactionModalsViewModelBase<RevenueDisplayItem, RevenueLineItem>
{
    #region Abstract Property Implementations

    protected override string TransactionTypeName => "Revenue";
    protected override string CounterpartyName => "Customer";
    protected override CategoryType CategoryTypeFilter => CategoryType.Sales;
    protected override bool UseCostPrice => false;

    #endregion

    #region Events (Revenue-specific aliases)

    public event EventHandler? RevenueSaved
    {
        add => TransactionSaved += value;
        remove => TransactionSaved -= value;
    }

    public event EventHandler? RevenueDeleted
    {
        add => TransactionDeleted += value;
        remove => TransactionDeleted -= value;
    }

    #endregion

    #region Revenue-Specific Properties

    // Customer alias for counterparty
    public CounterpartyOption? SelectedCustomer
    {
        get => SelectedCounterparty;
        set => SelectedCounterparty = value;
    }

    public bool HasCustomerError
    {
        get => HasCounterpartyError;
        set => HasCounterpartyError = value;
    }

    // Notify SelectedCustomer when SelectedCounterparty changes so UI bindings update
    partial void OnSelectedCounterpartyChanged(CounterpartyOption? value)
    {
        OnPropertyChanged(nameof(SelectedCustomer));
    }

    public ObservableCollection<CounterpartyOption> CustomerOptions => CounterpartyOptions;

    // Filter aliases
    public CounterpartyOption? FilterSelectedCustomer
    {
        get => FilterSelectedCounterparty;
        set => FilterSelectedCounterparty = value;
    }

    public string? FilterCustomerId
    {
        get => FilterCounterpartyId;
        set => FilterCounterpartyId = value;
    }

    // Delete aliases
    public string DeleteRevenueId
    {
        get => DeleteTransactionId;
        set => DeleteTransactionId = value;
    }

    public string DeleteRevenueDescription
    {
        get => DeleteTransactionDescription;
        set => DeleteTransactionDescription = value;
    }

    public string DeleteRevenueAmount
    {
        get => DeleteTransactionAmount;
        set => DeleteTransactionAmount = value;
    }

    // Command aliases for AXAML bindings
    public IRelayCommand SaveRevenueCommand => SaveTransactionCommand;
    public IRelayCommand DeleteRevenueCommand => DeleteTransactionCommand;

    #endregion

    #region Reason Options

    public override ObservableCollection<string> LostDamagedReasonOptions { get; } =
    [
        "Damaged in transit",
        "Defective product",
        "Lost in warehouse",
        "Damaged during storage",
        "Customer damaged",
        "Other"
    ];

    public override ObservableCollection<string> ReturnReasonOptions { get; } =
    [
        "Customer return",
        "Wrong item sent",
        "Quality issues",
        "Not as described",
        "Changed mind",
        "Defective",
        "Other"
    ];

    public override ObservableCollection<string> UndoReasonOptions { get; } =
    [
        "Item found",
        "Damage was repairable",
        "Customer changed mind",
        "Incorrect status",
        "Administrative error",
        "Other"
    ];

    #endregion

    #region Data Loading

    protected override void LoadCounterpartyOptions()
    {
        CounterpartyOptions.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Customers == null)
            return;

        foreach (var customer in companyData.Customers.OrderBy(c => c.Name))
        {
            CounterpartyOptions.Add(new CounterpartyOption { Id = customer.Id, Name = customer.Name });
        }
    }

    protected override void LoadCounterpartyOptionsInternal()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Customers == null)
            return;

        foreach (var customer in companyData.Customers.OrderBy(c => c.Name))
        {
            CounterpartyOptions.Add(new CounterpartyOption { Id = customer.Id, Name = customer.Name });
        }
    }

    #endregion

    #region Edit Modal

    public override void OpenEditModal(RevenueDisplayItem? item)
    {
        if (item == null) return;

        var sale = App.CompanyManager?.CompanyData?.Sales?.FirstOrDefault(s => s.Id == item.Id);
        if (sale == null) return;

        LoadCounterpartyOptions();
        LoadCategoryOptions();
        LoadProductOptions();

        EditingTransactionId = sale.Id;
        IsEditMode = true;
        ModalTitle = $"Edit Sale {sale.Id}";
        SaveButtonText = "Save Changes";

        SelectedCustomer = CustomerOptions.FirstOrDefault(c => c.Id == sale.CustomerId);
        PopulateFormFromTransaction(sale);

        IsAddEditModalOpen = true;
    }

    #endregion

    #region Delete

    public void OpenDeleteConfirm(RevenueDisplayItem? item)
    {
        if (item == null) return;

        DeleteTransactionIdInternal = item.Id;
        DeleteRevenueId = item.Id;
        DeleteRevenueDescription = item.ProductDescription;
        DeleteRevenueAmount = item.TotalFormatted;
        IsDeleteConfirmOpen = true;
    }

    protected override void DeleteTransaction()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Sales == null) return;

        var sale = companyData.Sales.FirstOrDefault(s => s.Id == DeleteTransactionIdInternal);
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

        var deletedSale = sale;
        var capturedReceipt = deletedReceipt;
        var action = new DelegateAction(
            $"Delete sale {sale.Id}",
            () =>
            {
                companyData.Sales.Add(deletedSale);
                if (capturedReceipt != null)
                    companyData.Receipts.Add(capturedReceipt);
                RaiseTransactionDeleted();
            },
            () =>
            {
                companyData.Sales.Remove(deletedSale);
                if (capturedReceipt != null)
                    companyData.Receipts.Remove(capturedReceipt);
                RaiseTransactionDeleted();
            });

        companyData.Sales.Remove(sale);
        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        RaiseTransactionDeleted();
        CloseDeleteConfirm();
    }

    #endregion

    #region Item Status Modal

    public void OpenMarkAsLostDamagedModal(RevenueDisplayItem? item)
    {
        OpenItemStatusModal(item, "LostDamaged", "Mark as Lost/Damaged", "Mark as Lost/Damaged", false, LostDamagedReasonOptions);
    }

    public void OpenMarkAsReturnedModal(RevenueDisplayItem? item)
    {
        OpenItemStatusModal(item, "Returned", "Mark as Returned", "Mark as Returned", false, ReturnReasonOptions);
    }

    public void OpenUndoLostDamagedModal(RevenueDisplayItem? item)
    {
        OpenItemStatusModal(item, "UndoLostDamaged", "Undo Lost/Damaged Status", "Undo Status", true, UndoReasonOptions);
    }

    public void OpenUndoReturnedModal(RevenueDisplayItem? item)
    {
        OpenItemStatusModal(item, "UndoReturned", "Undo Returned Status", "Undo Status", true, UndoReasonOptions);
    }

    protected override string GetItemStatusDescription(RevenueDisplayItem item)
    {
        return $"{item.Id} - {item.ProductDescription}";
    }

    protected override void ConfirmItemStatus()
    {
        if (ItemStatusItem == null)
        {
            CloseItemStatusModal();
            return;
        }

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

        var sale = companyData.Sales.FirstOrDefault(s => s.Id == ItemStatusItem.Id);
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
        RaiseTransactionSaved();
    }

    private void CreateLostDamagedRecord(CompanyData companyData, Sale sale)
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

    private void CreateReturnRecord(CompanyData companyData, Sale sale)
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
                new ReturnItem
                {
                    ProductId = productId,
                    Quantity = (int)sale.Quantity,
                    Reason = SelectedItemStatusReason ?? "Other"
                }
            ],
            RefundAmount = sale.Total,
            RestockingFee = 0,
            Status = ReturnStatus.Completed,
            Notes = ItemStatusNotes,
            ProcessedBy = sale.AccountantId ?? "",
            CreatedAt = DateTime.UtcNow
        };

        companyData.Returns.Add(returnRecord);
    }

    private static void RemoveLostDamagedRecord(CompanyData companyData, Sale sale)
    {
        var record = companyData.LostDamaged.FirstOrDefault(ld => ld.InventoryItemId == sale.Id);
        if (record != null)
            companyData.LostDamaged.Remove(record);
    }

    private static void RemoveReturnRecord(CompanyData companyData, Sale sale)
    {
        var record = companyData.Returns.FirstOrDefault(r => r.OriginalTransactionId == sale.Id);
        if (record != null)
            companyData.Returns.Remove(record);
    }

    #endregion

    #region Save Implementation

    protected override void SaveNewTransaction(CompanyData companyData)
    {
        companyData.IdCounters.Sale++;
        var saleId = $"SAL-{DateTime.Now:yyyy}-{companyData.IdCounters.Sale:D5}";

        var (description, totalQuantity, averageUnitPrice) = GetLineItemSummary();
        var modelLineItems = CreateModelLineItems();

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
            TaxRate = Subtotal > 0 ? (TaxAmount / Subtotal) * 100 : 0,
            TaxAmount = TaxAmount,
            ShippingCost = ModalShipping,
            Discount = ModalDiscount,
            Total = Total,
            PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash,
            Notes = ModalNotes,
            ReferenceNumber = ReceiptFilePath ?? string.Empty,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Create Receipt if file was attached
        Core.Models.Tracking.Receipt? receipt = null;
        if (!string.IsNullOrEmpty(ReceiptFilePath))
        {
            receipt = CreateReceipt(companyData, saleId, "Revenue", SelectedCustomer?.Name ?? "");
            sale.ReceiptId = receipt.Id;
            companyData.Receipts.Add(receipt);
        }

        var capturedReceipt = receipt;
        var action = new DelegateAction(
            $"Add sale {saleId}",
            () =>
            {
                companyData.Sales.Remove(sale);
                companyData.IdCounters.Sale--;
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Remove(capturedReceipt);
                    companyData.IdCounters.Receipt--;
                }
                RaiseTransactionSaved();
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
                RaiseTransactionSaved();
            });

        companyData.Sales.Add(sale);
        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();
        RaiseTransactionSaved();
    }

    protected override void SaveEditedTransaction(CompanyData companyData)
    {
        var sale = companyData.Sales.FirstOrDefault(s => s.Id == EditingTransactionId);
        if (sale == null) return;

        // Store original values for undo
        var original = CaptureTransactionState(sale);

        var (description, totalQuantity, averageUnitPrice) = GetLineItemSummary();
        var modelLineItems = CreateModelLineItems();

        // Apply changes
        sale.Date = ModalDate?.DateTime ?? DateTime.Now;
        sale.CustomerId = SelectedCustomer?.Id;
        sale.CategoryId = SelectedCategory?.Id;
        sale.Description = description;
        sale.LineItems = modelLineItems;
        sale.Quantity = totalQuantity;
        sale.UnitPrice = averageUnitPrice;
        sale.Amount = Subtotal;
        sale.TaxRate = Subtotal > 0 ? (TaxAmount / Subtotal) * 100 : 0;
        sale.TaxAmount = TaxAmount;
        sale.ShippingCost = ModalShipping;
        sale.Discount = ModalDiscount;
        sale.Total = Total;
        sale.PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash;
        sale.Notes = ModalNotes;
        sale.ReferenceNumber = ReceiptFilePath ?? string.Empty;
        sale.UpdatedAt = DateTime.Now;

        // Handle receipt
        Core.Models.Tracking.Receipt? newReceipt = null;
        if (!string.IsNullOrEmpty(ReceiptFilePath) && string.IsNullOrEmpty(original.ReceiptId))
        {
            newReceipt = CreateReceipt(companyData, sale.Id, "Revenue", SelectedCustomer?.Name ?? "");
            sale.ReceiptId = newReceipt.Id;
            companyData.Receipts.Add(newReceipt);
        }

        var capturedNewReceipt = newReceipt;
        var action = new DelegateAction(
            $"Edit sale {EditingTransactionId}",
            () =>
            {
                RestoreTransactionState(sale, original);
                if (capturedNewReceipt != null)
                {
                    companyData.Receipts.Remove(capturedNewReceipt);
                    companyData.IdCounters.Receipt--;
                }
                RaiseTransactionSaved();
            },
            () =>
            {
                sale.Date = ModalDate?.DateTime ?? DateTime.Now;
                sale.CustomerId = SelectedCustomer?.Id;
                sale.CategoryId = SelectedCategory?.Id;
                sale.Description = description;
                sale.LineItems = modelLineItems;
                sale.Quantity = totalQuantity;
                sale.UnitPrice = averageUnitPrice;
                sale.Amount = Subtotal;
                sale.TaxRate = Subtotal > 0 ? (TaxAmount / Subtotal) * 100 : 0;
                sale.TaxAmount = TaxAmount;
                sale.ShippingCost = ModalShipping;
                sale.Discount = ModalDiscount;
                sale.Total = Total;
                sale.PaymentMethod = pm;
                sale.Notes = ModalNotes;
                sale.ReferenceNumber = ReceiptFilePath ?? string.Empty;
                if (capturedNewReceipt != null)
                {
                    sale.ReceiptId = capturedNewReceipt.Id;
                    companyData.Receipts.Add(capturedNewReceipt);
                    companyData.IdCounters.Receipt++;
                }
                RaiseTransactionSaved();
            });

        App.UndoRedoManager?.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();
        RaiseTransactionSaved();
    }

    private Core.Models.Tracking.Receipt CreateReceipt(CompanyData companyData, string transactionId, string transactionType, string vendor)
    {
        companyData.IdCounters.Receipt++;
        var receiptId = $"RCP-{DateTime.Now:yyyy}-{companyData.IdCounters.Receipt:D5}";

        var fileInfo = new FileInfo(ReceiptFilePath!);
        var fileType = GetFileType(ReceiptFilePath!);

        string? fileData = null;
        if (fileInfo.Exists)
        {
            try
            {
                var bytes = File.ReadAllBytes(ReceiptFilePath!);
                fileData = Convert.ToBase64String(bytes);
            }
            catch
            {
                // Failed to read file
            }
        }

        return new Core.Models.Tracking.Receipt
        {
            Id = receiptId,
            TransactionId = transactionId,
            TransactionType = transactionType,
            FileName = fileInfo.Name,
            FileType = fileType,
            FileSize = fileInfo.Exists ? fileInfo.Length : 0,
            FileData = fileData,
            OriginalFilePath = ReceiptFilePath,
            Amount = Total,
            Date = ModalDate?.DateTime ?? DateTime.Now,
            Vendor = vendor,
            Source = "Manual",
            CreatedAt = DateTime.Now
        };
    }

    private static TransactionState CaptureTransactionState(Sale sale)
    {
        return new TransactionState
        {
            Date = sale.Date,
            CounterpartyId = sale.CustomerId,
            CategoryId = sale.CategoryId,
            Description = sale.Description,
            LineItems = sale.LineItems.ToList(),
            Quantity = sale.Quantity,
            UnitPrice = sale.UnitPrice,
            Amount = sale.Amount,
            TaxRate = sale.TaxRate,
            TaxAmount = sale.TaxAmount,
            ShippingCost = sale.ShippingCost,
            Discount = sale.Discount,
            Total = sale.Total,
            PaymentMethod = sale.PaymentMethod,
            Notes = sale.Notes,
            ReferenceNumber = sale.ReferenceNumber,
            ReceiptId = sale.ReceiptId
        };
    }

    private static void RestoreTransactionState(Sale sale, TransactionState state)
    {
        sale.Date = state.Date;
        sale.CustomerId = state.CounterpartyId;
        sale.CategoryId = state.CategoryId;
        sale.Description = state.Description;
        sale.LineItems = state.LineItems;
        sale.Quantity = state.Quantity;
        sale.UnitPrice = state.UnitPrice;
        sale.Amount = state.Amount;
        sale.TaxRate = state.TaxRate;
        sale.TaxAmount = state.TaxAmount;
        sale.ShippingCost = state.ShippingCost;
        sale.Discount = state.Discount;
        sale.Total = state.Total;
        sale.PaymentMethod = state.PaymentMethod;
        sale.Notes = state.Notes;
        sale.ReferenceNumber = state.ReferenceNumber;
        sale.ReceiptId = state.ReceiptId;
    }

    #endregion

    #region Navigation Aliases

    [RelayCommand]
    private void NavigateToCreateCustomer() => NavigateToCreateCounterparty();

    #endregion
}

/// <summary>
/// Line item for revenue/sale form.
/// </summary>
public partial class RevenueLineItem : TransactionLineItemBase
{
}
