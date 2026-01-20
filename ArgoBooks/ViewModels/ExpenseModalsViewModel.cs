using ArgoBooks.Localization;
using ArgoBooks.Services;
using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Models.Transactions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for expense modals (Add, Edit, Delete, Filter).
/// </summary>
public partial class ExpenseModalsViewModel : TransactionModalsViewModelBase<ExpenseDisplayItem, ExpenseLineItem>
{
    #region Abstract Property Implementations

    protected override string TransactionTypeName => "Expense";
    protected override string CounterpartyName => "Supplier";
    protected override CategoryType CategoryTypeFilter => CategoryType.Expense;
    protected override bool UseCostPrice => true;

    #endregion

    #region Events (Expense-specific aliases)

    public event EventHandler? ExpenseSaved
    {
        add => TransactionSaved += value;
        remove => TransactionSaved -= value;
    }

    public event EventHandler? ExpenseDeleted
    {
        add => TransactionDeleted += value;
        remove => TransactionDeleted -= value;
    }

    #endregion

    #region Expense-Specific Properties

    // Supplier alias for counterparty
    public CounterpartyOption? SelectedSupplier
    {
        get => SelectedCounterparty;
        set => SelectedCounterparty = value;
    }

    public bool HasSupplierError
    {
        get => HasCounterpartyError;
        set => HasCounterpartyError = value;
    }

    // Notify SelectedSupplier when SelectedCounterparty changes so UI bindings update
    protected override void OnCounterpartyChanged(CounterpartyOption? value)
    {
        OnPropertyChanged(nameof(SelectedSupplier));
    }

    public ObservableCollection<CounterpartyOption> SupplierOptions => CounterpartyOptions;

    // Filter aliases
    public CounterpartyOption? FilterSelectedSupplier
    {
        get => FilterSelectedCounterparty;
        set => FilterSelectedCounterparty = value;
    }

    public string? FilterSupplierId
    {
        get => FilterCounterpartyId;
        set => FilterCounterpartyId = value;
    }

    // Delete aliases
    public string DeleteExpenseId
    {
        get => DeleteTransactionId;
        set => DeleteTransactionId = value;
    }

    public string DeleteExpenseDescription
    {
        get => DeleteTransactionDescription;
        set => DeleteTransactionDescription = value;
    }

    public string DeleteExpenseAmount
    {
        get => DeleteTransactionAmount;
        set => DeleteTransactionAmount = value;
    }

    // Command aliases for AXAML bindings
    public IAsyncRelayCommand SaveExpenseCommand => SaveTransactionAsyncCommand;

    // Expense-specific filter
    [ObservableProperty]
    private string _filterReceiptStatus = "All";

    public ObservableCollection<string> ReceiptFilterOptions { get; } = ["All", "With Receipt", "No Receipt"];

    #endregion

    #region Reason Options

    public override ObservableCollection<string> LostDamagedReasonOptions { get; } =
    [
        "Damaged in transit",
        "Defective product",
        "Lost in warehouse",
        "Damaged during storage",
        "Expired",
        "Other"
    ];

    public override ObservableCollection<string> ReturnReasonOptions { get; } =
    [
        "Wrong item received",
        "Quality issues",
        "Not as described",
        "Duplicate order",
        "Changed mind",
        "Better price elsewhere",
        "Other"
    ];

    public override ObservableCollection<string> UndoReasonOptions { get; } =
    [
        "Item found",
        "Damage was repairable",
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
        if (companyData?.Suppliers == null)
            return;

        foreach (var supplier in companyData.Suppliers.OrderBy(s => s.Name))
        {
            CounterpartyOptions.Add(new CounterpartyOption { Id = supplier.Id, Name = supplier.Name });
        }
    }

    protected override void LoadCounterpartyOptionsInternal()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Suppliers == null)
            return;

        foreach (var supplier in companyData.Suppliers.OrderBy(s => s.Name))
        {
            CounterpartyOptions.Add(new CounterpartyOption { Id = supplier.Id, Name = supplier.Name });
        }
    }

    #endregion

    #region Edit Modal

    public override void OpenEditModal(ExpenseDisplayItem? item)
    {
        if (item == null) return;

        var expense = App.CompanyManager?.CompanyData?.Expenses.FirstOrDefault(p => p.Id == item.Id);
        if (expense == null) return;

        LoadCounterpartyOptions();
        LoadCategoryOptions();
        LoadProductOptions();

        EditingTransactionId = expense.Id;
        IsEditMode = true;
        ModalTitle = $"Edit Expense {expense.Id}";
        SaveButtonText = "Save Changes";

        SelectedSupplier = SupplierOptions.FirstOrDefault(s => s.Id == expense.SupplierId);
        PopulateFormFromTransaction(expense);

        IsAddEditModalOpen = true;
    }

    #endregion

    #region Delete

    public async void OpenDeleteConfirm(ExpenseDisplayItem? item)
    {
        if (item == null) return;

        var dialog = App.ConfirmationDialog;
        if (dialog == null) return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Delete Expense".Translate(),
            Message = "Are you sure you want to delete this expense?\n\nID: {0}\nDescription: {1}\nAmount: {2}".TranslateFormat(item.Id, item.ProductDescription, item.TotalFormatted),
            PrimaryButtonText = "Delete".Translate(),
            CancelButtonText = "Cancel".Translate(),
            IsPrimaryDestructive = true
        });

        if (result != ConfirmationResult.Primary) return;

        var companyData = App.CompanyManager?.CompanyData;

        var expense = companyData?.Expenses.FirstOrDefault(p => p.Id == item.Id);
        if (expense == null) return;

        // Find and remove associated receipt
        Receipt? deletedReceipt = null;
        if (!string.IsNullOrEmpty(expense.ReceiptId))
        {
            deletedReceipt = companyData?.Receipts.FirstOrDefault(r => r.Id == expense.ReceiptId);
            if (deletedReceipt != null)
            {
                companyData?.Receipts.Remove(deletedReceipt);
            }
        }

        var deletedExpense = expense;
        var capturedReceipt = deletedReceipt;
        var action = new DelegateAction(
            $"Delete expense {expense.Id}",
            () =>
            {
                companyData?.Expenses.Add(deletedExpense);
                if (capturedReceipt != null)
                    companyData?.Receipts.Add(capturedReceipt);
                RaiseTransactionDeleted();
            },
            () =>
            {
                companyData?.Expenses.Remove(deletedExpense);
                if (capturedReceipt != null)
                    companyData?.Receipts.Remove(capturedReceipt);
                RaiseTransactionDeleted();
            });

        companyData?.Expenses.Remove(expense);
        App.UndoRedoManager.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();

        RaiseTransactionDeleted();
    }

    #endregion

    #region Filter Override

    protected override void ClearFilters()
    {
        FilterReceiptStatus = "All";
        base.ClearFilters();
    }

    #endregion

    #region Item Status Modal

    public void OpenMarkAsLostDamagedModal(ExpenseDisplayItem? item)
    {
        OpenItemStatusModal(item, "LostDamaged", "Mark as Lost/Damaged", "Mark as Lost/Damaged", false, LostDamagedReasonOptions);
    }

    public void OpenMarkAsReturnedModal(ExpenseDisplayItem? item)
    {
        OpenItemStatusModal(item, "Returned", "Mark as Returned", "Mark as Returned", false, ReturnReasonOptions);
    }

    public void OpenUndoLostDamagedModal(ExpenseDisplayItem? item)
    {
        OpenItemStatusModal(item, "UndoLostDamaged", "Undo Lost/Damaged Status", "Undo Status", true, UndoReasonOptions);
    }

    public void OpenUndoReturnedModal(ExpenseDisplayItem? item)
    {
        OpenItemStatusModal(item, "UndoReturned", "Undo Returned Status", "Undo Status", true, UndoReasonOptions);
    }

    protected override string GetItemStatusDescription(ExpenseDisplayItem item)
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

        var purchase = companyData.Expenses.FirstOrDefault(p => p.Id == ItemStatusItem.Id);
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
        RaiseTransactionSaved();
    }

    private void CreateLostDamagedRecord(CompanyData companyData, Expense purchase)
    {
        var reason = MapToLostDamagedReason(SelectedItemStatusReason ?? "Other");
        var productId = purchase.LineItems.FirstOrDefault()?.ProductId ?? "";
        var valueLost = purchase.Total;

        var lostDamaged = new LostDamaged
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

    private void CreateReturnRecord(CompanyData companyData, Expense purchase)
    {
        var productId = purchase.LineItems.FirstOrDefault()?.ProductId ?? "";

        var returnRecord = new Return
        {
            Id = $"RET-{++companyData.IdCounters.Return:D3}",
            OriginalTransactionId = purchase.Id,
            ReturnType = "Expense",
            SupplierId = purchase.SupplierId ?? "",
            CustomerId = "",
            ReturnDate = DateTime.UtcNow,
            Items =
            [
                new ReturnItem
                {
                    ProductId = productId,
                    Quantity = (int)purchase.Quantity,
                    Reason = SelectedItemStatusReason ?? "Other"
                }
            ],
            RefundAmount = purchase.Total,
            RestockingFee = 0,
            Status = ReturnStatus.Completed,
            Notes = ItemStatusNotes,
            ProcessedBy = purchase.AccountantId ?? "",
            CreatedAt = DateTime.UtcNow
        };

        companyData.Returns.Add(returnRecord);
    }

    private static void RemoveLostDamagedRecord(CompanyData companyData, Expense purchase)
    {
        var record = companyData.LostDamaged.FirstOrDefault(ld => ld.InventoryItemId == purchase.Id);
        if (record != null)
            companyData.LostDamaged.Remove(record);
    }

    private static void RemoveReturnRecord(CompanyData companyData, Expense purchase)
    {
        var record = companyData.Returns.FirstOrDefault(r => r.OriginalTransactionId == purchase.Id);
        if (record != null)
            companyData.Returns.Remove(record);
    }

    #endregion

    #region Save Implementation

    protected override void SaveNewTransaction(CompanyData companyData)
    {
        companyData.IdCounters.Expense++;
        var expenseId = $"PUR-{DateTime.Now:yyyy}-{companyData.IdCounters.Expense:D5}";

        var (description, totalQuantity, averageUnitPrice) = GetLineItemSummary();
        var modelLineItems = CreateModelLineItems();

        var expense = new Expense
        {
            Id = expenseId,
            Date = ModalDate?.DateTime ?? DateTime.Now,
            SupplierId = SelectedSupplier?.Id,
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
            UpdatedAt = DateTime.Now,
            // USD conversion fields
            OriginalCurrency = ConvertedTotal?.OriginalCurrency ?? "USD",
            TotalUSD = ConvertedTotal?.AmountUSD ?? Total,
            TaxAmountUSD = ConvertedTaxAmount?.AmountUSD ?? TaxAmount,
            ShippingCostUSD = ConvertedShippingCost?.AmountUSD ?? ModalShipping,
            DiscountUSD = ConvertedDiscount?.AmountUSD ?? ModalDiscount,
            UnitPriceUSD = ConvertedTotal != null && ConvertedTotal.OriginalCurrency != "USD" && Subtotal > 0
                ? Math.Round(ConvertedTotal.AmountUSD / Total * averageUnitPrice, 2)
                : averageUnitPrice
        };

        // Create Receipt if file was attached
        Receipt? receipt = null;
        if (!string.IsNullOrEmpty(ReceiptFilePath))
        {
            receipt = CreateReceipt(companyData, expenseId, "Expense", SelectedSupplier?.Name ?? "");
            expense.ReceiptId = receipt.Id;
            companyData.Receipts.Add(receipt);
        }

        var capturedReceipt = receipt;
        var action = new DelegateAction(
            $"Add expense {expenseId}",
            () =>
            {
                companyData.Expenses.Remove(expense);
                companyData.IdCounters.Expense--;
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Remove(capturedReceipt);
                    companyData.IdCounters.Receipt--;
                }
                RaiseTransactionSaved();
            },
            () =>
            {
                companyData.Expenses.Add(expense);
                companyData.IdCounters.Expense++;
                if (capturedReceipt != null)
                {
                    companyData.Receipts.Add(capturedReceipt);
                    companyData.IdCounters.Receipt++;
                }
                RaiseTransactionSaved();
            });

        companyData.Expenses.Add(expense);
        App.UndoRedoManager.RecordAction(action);
        App.CompanyManager?.MarkAsChanged();
        RaiseTransactionSaved();
    }

    protected override void SaveEditedTransaction(CompanyData companyData)
    {
        var expense = companyData.Expenses.FirstOrDefault(p => p.Id == EditingTransactionId);
        if (expense == null) return;

        // Store original values for undo
        var original = CaptureTransactionState(expense);

        var (description, totalQuantity, averageUnitPrice) = GetLineItemSummary();
        var modelLineItems = CreateModelLineItems();

        // Apply changes
        expense.Date = ModalDate?.DateTime ?? DateTime.Now;
        expense.SupplierId = SelectedSupplier?.Id;
        expense.Description = description;
        expense.LineItems = modelLineItems;
        expense.Quantity = totalQuantity;
        expense.UnitPrice = averageUnitPrice;
        expense.Amount = Subtotal;
        expense.TaxRate = Subtotal > 0 ? (TaxAmount / Subtotal) * 100 : 0;
        expense.TaxAmount = TaxAmount;
        expense.ShippingCost = ModalShipping;
        expense.Discount = ModalDiscount;
        expense.Total = Total;
        expense.PaymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod.Replace(" ", ""), out var pm) ? pm : PaymentMethod.Cash;
        expense.Notes = ModalNotes;
        expense.ReferenceNumber = ReceiptFilePath ?? string.Empty;
        expense.UpdatedAt = DateTime.Now;
        // USD conversion fields
        expense.OriginalCurrency = ConvertedTotal?.OriginalCurrency ?? "USD";
        expense.TotalUSD = ConvertedTotal?.AmountUSD ?? Total;
        expense.TaxAmountUSD = ConvertedTaxAmount?.AmountUSD ?? TaxAmount;
        expense.ShippingCostUSD = ConvertedShippingCost?.AmountUSD ?? ModalShipping;
        expense.DiscountUSD = ConvertedDiscount?.AmountUSD ?? ModalDiscount;
        expense.UnitPriceUSD = ConvertedTotal != null && ConvertedTotal.OriginalCurrency != "USD" && Subtotal > 0
            ? Math.Round(ConvertedTotal.AmountUSD / Total * averageUnitPrice, 2)
            : averageUnitPrice;

        // Handle receipt
        Receipt? newReceipt = null;
        if (!string.IsNullOrEmpty(ReceiptFilePath) && string.IsNullOrEmpty(original.ReceiptId))
        {
            newReceipt = CreateReceipt(companyData, expense.Id, "Expense", SelectedSupplier?.Name ?? "");
            expense.ReceiptId = newReceipt.Id;
            companyData.Receipts.Add(newReceipt);
        }

        var capturedNewReceipt = newReceipt;
        var action = new DelegateAction(
            $"Edit expense {EditingTransactionId}",
            () =>
            {
                RestoreTransactionState(expense, original);
                if (capturedNewReceipt != null)
                {
                    companyData.Receipts.Remove(capturedNewReceipt);
                    companyData.IdCounters.Receipt--;
                }
                RaiseTransactionSaved();
            },
            () =>
            {
                expense.Date = ModalDate?.DateTime ?? DateTime.Now;
                expense.SupplierId = SelectedSupplier?.Id;
                expense.Description = description;
                expense.LineItems = modelLineItems;
                expense.Quantity = totalQuantity;
                expense.UnitPrice = averageUnitPrice;
                expense.Amount = Subtotal;
                expense.TaxRate = Subtotal > 0 ? (TaxAmount / Subtotal) * 100 : 0;
                expense.TaxAmount = TaxAmount;
                expense.ShippingCost = ModalShipping;
                expense.Discount = ModalDiscount;
                expense.Total = Total;
                expense.PaymentMethod = pm;
                expense.Notes = ModalNotes;
                expense.ReferenceNumber = ReceiptFilePath ?? string.Empty;
                if (capturedNewReceipt != null)
                {
                    expense.ReceiptId = capturedNewReceipt.Id;
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

    private static TransactionState CaptureTransactionState(Expense expense)
    {
        return new TransactionState
        {
            Date = expense.Date,
            CounterpartyId = expense.SupplierId,
            Description = expense.Description,
            LineItems = expense.LineItems.ToList(),
            Quantity = expense.Quantity,
            UnitPrice = expense.UnitPrice,
            Amount = expense.Amount,
            TaxRate = expense.TaxRate,
            TaxAmount = expense.TaxAmount,
            ShippingCost = expense.ShippingCost,
            Discount = expense.Discount,
            Total = expense.Total,
            PaymentMethod = expense.PaymentMethod,
            Notes = expense.Notes,
            ReferenceNumber = expense.ReferenceNumber,
            ReceiptId = expense.ReceiptId
        };
    }

    private static void RestoreTransactionState(Expense expense, TransactionState state)
    {
        expense.Date = state.Date;
        expense.SupplierId = state.CounterpartyId;
        expense.Description = state.Description;
        expense.LineItems = state.LineItems;
        expense.Quantity = state.Quantity;
        expense.UnitPrice = state.UnitPrice;
        expense.Amount = state.Amount;
        expense.TaxRate = state.TaxRate;
        expense.TaxAmount = state.TaxAmount;
        expense.ShippingCost = state.ShippingCost;
        expense.Discount = state.Discount;
        expense.Total = state.Total;
        expense.PaymentMethod = state.PaymentMethod;
        expense.Notes = state.Notes;
        expense.ReferenceNumber = state.ReferenceNumber;
        expense.ReceiptId = state.ReceiptId;
    }

    #endregion

    #region Navigation Aliases

    [RelayCommand]
    private void NavigateToCreateSupplier() => NavigateToCreateCounterparty();

    #endregion
}

/// <summary>
/// Line item for expense form.
/// </summary>
public class ExpenseLineItem : TransactionLineItemBase
{
}

/// <summary>
/// Helper class for capturing transaction state for undo/redo.
/// </summary>
internal class TransactionState
{
    public DateTime Date { get; set; }
    public string? CounterpartyId { get; set; }
    public string? CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<LineItem> LineItems { get; set; } = [];
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string? ReceiptId { get; set; }
}
