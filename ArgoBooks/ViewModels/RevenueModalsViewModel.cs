using ArgoBooks.Localization;
using ArgoBooks.Services;
using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Models.Transactions;
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
    protected override CategoryType CategoryTypeFilter => CategoryType.Revenue;
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
    protected override void OnCounterpartyChanged(CounterpartyOption? value)
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

        var revenue = App.CompanyManager?.CompanyData?.Revenues.FirstOrDefault(s => s.Id == item.Id);
        if (revenue == null) return;

        LoadCounterpartyOptions();
        LoadCategoryOptions();
        LoadProductOptions();

        EditingTransactionId = revenue.Id;
        IsEditMode = true;
        ModalTitle = $"Edit Revenue {revenue.Id}";
        SaveButtonText = "Save Changes";

        SelectedCustomer = CustomerOptions.FirstOrDefault(c => c.Id == revenue.CustomerId);
        PopulateFormFromTransaction(revenue);

        IsAddEditModalOpen = true;
    }

    #endregion

    #region Delete

    public async void OpenDeleteConfirm(RevenueDisplayItem? item)
    {
        if (item == null) return;

        var dialog = App.ConfirmationDialog;
        if (dialog == null) return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Delete Revenue",
            Message = $"Are you sure you want to delete this revenue?\n\nID: {item.Id}\nDescription: {item.ProductDescription}\nAmount: {item.TotalFormatted}",
            PrimaryButtonText = "Delete",
            CancelButtonText = "Cancel",
            IsPrimaryDestructive = true
        });

        if (result != ConfirmationResult.Primary) return;

        var companyData = App.CompanyManager?.CompanyData;

        var revenue = companyData?.Revenues.FirstOrDefault(s => s.Id == item.Id);
        if (revenue == null) return;

        // Find and remove associated receipt
        Receipt? deletedReceipt = null;
        if (!string.IsNullOrEmpty(revenue.ReceiptId))
        {
            deletedReceipt = companyData?.Receipts.FirstOrDefault(r => r.Id == revenue.ReceiptId);
            if (deletedReceipt != null)
            {
                companyData?.Receipts.Remove(deletedReceipt);
            }
        }

        var deletedRevenue = revenue;
        var capturedReceipt = deletedReceipt;
        var action = new DelegateAction(
            $"Delete revenue {revenue.Id}",
            () =>
            {
                companyData?.Revenues.Add(deletedRevenue);
                if (capturedReceipt != null)
                    companyData?.Receipts.Add(capturedReceipt);
                RaiseTransactionDeleted();
            },
            () =>
            {
                companyData?.Revenues.Remove(deletedRevenue);
                if (capturedReceipt != null)
                    companyData?.Receipts.Remove(capturedReceipt);
                RaiseTransactionDeleted();
            });

        companyData?.Revenues.Remove(revenue);
        App.UndoRedoManager.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        RaiseTransactionDeleted();
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
            ItemStatusReasonErrorMessage = "Please select a reason".Translate();
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

        var revenue = companyData.Revenues.FirstOrDefault(s => s.Id == ItemStatusItem.Id);
        if (revenue == null)
        {
            CloseItemStatusModal();
            return;
        }

        switch (ItemStatusAction)
        {
            case "LostDamaged":
                CreateLostDamagedRecord(companyData, revenue);
                break;
            case "Returned":
                CreateReturnRecord(companyData, revenue);
                break;
            case "UndoLostDamaged":
                RemoveLostDamagedRecord(companyData, revenue);
                break;
            case "UndoReturned":
                RemoveReturnRecord(companyData, revenue);
                break;
        }

        App.CompanyManager?.MarkAsChanged();
        CloseItemStatusModal();
        RaiseTransactionSaved();
    }

    private void CreateLostDamagedRecord(CompanyData companyData, Revenue revenue)
    {
        var reason = MapToLostDamagedReason(SelectedItemStatusReason ?? "Other");
        var productId = revenue.LineItems.FirstOrDefault()?.ProductId ?? "";
        var valueLost = revenue.Total;

        var lostDamaged = new LostDamaged
        {
            Id = $"LOST-{++companyData.IdCounters.LostDamaged:D3}",
            ProductId = productId,
            InventoryItemId = revenue.Id,
            Quantity = (int)revenue.Quantity,
            Reason = reason,
            DateDiscovered = DateTime.UtcNow,
            ValueLost = valueLost,
            Notes = $"From revenue {revenue.Id}. {ItemStatusNotes}".Trim(),
            InsuranceClaim = false,
            CreatedAt = DateTime.UtcNow
        };

        companyData.LostDamaged.Add(lostDamaged);
    }

    private void CreateReturnRecord(CompanyData companyData, Revenue revenue)
    {
        var productId = revenue.LineItems.FirstOrDefault()?.ProductId ?? "";

        var returnRecord = new Return
        {
            Id = $"RET-{++companyData.IdCounters.Return:D3}",
            OriginalTransactionId = revenue.Id,
            ReturnType = "Customer",
            SupplierId = "",
            CustomerId = revenue.CustomerId ?? "",
            ReturnDate = DateTime.UtcNow,
            Items =
            [
                new ReturnItem
                {
                    ProductId = productId,
                    Quantity = (int)revenue.Quantity,
                    Reason = SelectedItemStatusReason ?? "Other"
                }
            ],
            RefundAmount = revenue.Total,
            RestockingFee = 0,
            Status = ReturnStatus.Completed,
            Notes = ItemStatusNotes,
            ProcessedBy = revenue.AccountantId ?? "",
            CreatedAt = DateTime.UtcNow
        };

        companyData.Returns.Add(returnRecord);
    }

    private static void RemoveLostDamagedRecord(CompanyData companyData, Revenue revenue)
    {
        var record = companyData.LostDamaged.FirstOrDefault(ld => ld.InventoryItemId == revenue.Id);
        if (record != null)
            companyData.LostDamaged.Remove(record);
    }

    private static void RemoveReturnRecord(CompanyData companyData, Revenue revenue)
    {
        var record = companyData.Returns.FirstOrDefault(r => r.OriginalTransactionId == revenue.Id);
        if (record != null)
            companyData.Returns.Remove(record);
    }

    #endregion

    #region Save Implementation

    protected override void SaveNewTransaction(CompanyData companyData)
    {
        companyData.IdCounters.Revenue++;
        var revenueId = $"REV-{DateTime.Now:yyyy}-{companyData.IdCounters.Revenue:D5}";

        var (description, totalQuantity, averageUnitPrice) = GetLineItemSummary();
        var modelLineItems = CreateModelLineItems();

        var revenue = new Revenue
        {
            Id = revenueId,
            Date = ModalDate?.DateTime ?? DateTime.Now,
            CustomerId = SelectedCustomer?.Id,
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
        Receipt? receipt = null;
        if (!string.IsNullOrEmpty(ReceiptFilePath))
        {
            receipt = CreateReceipt(companyData, revenueId, "Revenue", SelectedCustomer?.Name ?? "");
            revenue.ReceiptId = receipt.Id;
            companyData.Receipts.Add(receipt);
        }

        var capturedReceipt = receipt;
        var action = new DelegateAction(
            $"Add revenue {revenueId}",
            () =>
            {
                companyData.Revenues.Remove(revenue);
                companyData.IdCounters.Revenue--;
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Remove(capturedReceipt);
                    companyData.IdCounters.Receipt--;
                }
                RaiseTransactionSaved();
            },
            () =>
            {
                companyData.Revenues.Add(revenue);
                companyData.IdCounters.Revenue++;
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Add(capturedReceipt);
                    companyData.IdCounters.Receipt++;
                }
                RaiseTransactionSaved();
            });

        companyData.Revenues.Add(revenue);
        App.UndoRedoManager.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();
        RaiseTransactionSaved();
    }

    protected override void SaveEditedTransaction(CompanyData companyData)
    {
        var revenue = companyData.Revenues.FirstOrDefault(s => s.Id == EditingTransactionId);
        if (revenue == null) return;

        // Store original values for undo
        var original = CaptureTransactionState(revenue);

        var (description, totalQuantity, averageUnitPrice) = GetLineItemSummary();
        var modelLineItems = CreateModelLineItems();

        // Apply changes
        revenue.Date = ModalDate?.DateTime ?? DateTime.Now;
        revenue.CustomerId = SelectedCustomer?.Id;
        revenue.Description = description;
        revenue.LineItems = modelLineItems;
        revenue.Quantity = totalQuantity;
        revenue.UnitPrice = averageUnitPrice;
        revenue.Amount = Subtotal;
        revenue.TaxRate = Subtotal > 0 ? (TaxAmount / Subtotal) * 100 : 0;
        revenue.TaxAmount = TaxAmount;
        revenue.ShippingCost = ModalShipping;
        revenue.Discount = ModalDiscount;
        revenue.Total = Total;
        revenue.PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash;
        revenue.Notes = ModalNotes;
        revenue.ReferenceNumber = ReceiptFilePath ?? string.Empty;
        revenue.UpdatedAt = DateTime.Now;

        // Handle receipt
        Receipt? newReceipt = null;
        if (!string.IsNullOrEmpty(ReceiptFilePath) && string.IsNullOrEmpty(original.ReceiptId))
        {
            newReceipt = CreateReceipt(companyData, revenue.Id, "Revenue", SelectedCustomer?.Name ?? "");
            revenue.ReceiptId = newReceipt.Id;
            companyData.Receipts.Add(newReceipt);
        }

        var capturedNewReceipt = newReceipt;
        var action = new DelegateAction(
            $"Edit revenue {EditingTransactionId}",
            () =>
            {
                RestoreTransactionState(revenue, original);
                if (capturedNewReceipt != null)
                {
                    companyData.Receipts.Remove(capturedNewReceipt);
                    companyData.IdCounters.Receipt--;
                }
                RaiseTransactionSaved();
            },
            () =>
            {
                revenue.Date = ModalDate?.DateTime ?? DateTime.Now;
                revenue.CustomerId = SelectedCustomer?.Id;
                revenue.Description = description;
                revenue.LineItems = modelLineItems;
                revenue.Quantity = totalQuantity;
                revenue.UnitPrice = averageUnitPrice;
                revenue.Amount = Subtotal;
                revenue.TaxRate = Subtotal > 0 ? (TaxAmount / Subtotal) * 100 : 0;
                revenue.TaxAmount = TaxAmount;
                revenue.ShippingCost = ModalShipping;
                revenue.Discount = ModalDiscount;
                revenue.Total = Total;
                revenue.PaymentMethod = pm;
                revenue.Notes = ModalNotes;
                revenue.ReferenceNumber = ReceiptFilePath ?? string.Empty;
                if (capturedNewReceipt != null)
                {
                    revenue.ReceiptId = capturedNewReceipt.Id;
                    companyData.Receipts.Add(capturedNewReceipt);
                    companyData.IdCounters.Receipt++;
                }
                RaiseTransactionSaved();
            });

        App.UndoRedoManager.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();
        RaiseTransactionSaved();
    }

    private Receipt CreateReceipt(CompanyData companyData, string transactionId, string transactionType, string supplier)
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

        return new Receipt
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
            Supplier = supplier,
            Source = "Manual",
            CreatedAt = DateTime.Now
        };
    }

    private static TransactionState CaptureTransactionState(Revenue revenue)
    {
        return new TransactionState
        {
            Date = revenue.Date,
            CounterpartyId = revenue.CustomerId,
            Description = revenue.Description,
            LineItems = revenue.LineItems.ToList(),
            Quantity = revenue.Quantity,
            UnitPrice = revenue.UnitPrice,
            Amount = revenue.Amount,
            TaxRate = revenue.TaxRate,
            TaxAmount = revenue.TaxAmount,
            ShippingCost = revenue.ShippingCost,
            Discount = revenue.Discount,
            Total = revenue.Total,
            PaymentMethod = revenue.PaymentMethod,
            Notes = revenue.Notes,
            ReferenceNumber = revenue.ReferenceNumber,
            ReceiptId = revenue.ReceiptId
        };
    }

    private static void RestoreTransactionState(Revenue revenue, TransactionState state)
    {
        revenue.Date = state.Date;
        revenue.CustomerId = state.CounterpartyId;
        revenue.Description = state.Description;
        revenue.LineItems = state.LineItems;
        revenue.Quantity = state.Quantity;
        revenue.UnitPrice = state.UnitPrice;
        revenue.Amount = state.Amount;
        revenue.TaxRate = state.TaxRate;
        revenue.TaxAmount = state.TaxAmount;
        revenue.ShippingCost = state.ShippingCost;
        revenue.Discount = state.Discount;
        revenue.Total = state.Total;
        revenue.PaymentMethod = state.PaymentMethod;
        revenue.Notes = state.Notes;
        revenue.ReferenceNumber = state.ReferenceNumber;
        revenue.ReceiptId = state.ReceiptId;
    }

    #endregion

    #region Navigation Aliases

    [RelayCommand]
    private void NavigateToCreateCustomer() => NavigateToCreateCounterparty();

    #endregion
}

/// <summary>
/// Line item for revenue form.
/// </summary>
public class RevenueLineItem : TransactionLineItemBase
{
}
