using ArgoBooks.Localization;
using ArgoBooks.Services;
using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Rentals;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for rental records modals.
/// </summary>
public partial class RentalRecordsModalsViewModel : ObservableObject
{
    #region Modal State

    [ObservableProperty]
    private bool _isAddModalOpen;

    [ObservableProperty]
    private bool _isEditModalOpen;

    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    [ObservableProperty]
    private bool _isFilterModalOpen;

    [ObservableProperty]
    private bool _isReturnModalOpen;

    [ObservableProperty]
    private bool _isViewModalOpen;

    #endregion

    #region Modal Form Fields

    [ObservableProperty]
    private CustomerOption? _modalCustomer;

    [ObservableProperty]
    private AccountantOption? _modalAccountant;

    [ObservableProperty]
    private DateTimeOffset? _modalStartDate = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset? _modalDueDate = DateTimeOffset.Now.AddDays(1);

    [ObservableProperty]
    private string _modalNotes = string.Empty;

    [ObservableProperty]
    private string? _modalCustomerError;

    [ObservableProperty]
    private string? _modalLineItemsError;

    private RentalRecord? _editingRecord;

    // Original values for change detection in edit mode
    private CustomerOption? _originalCustomer;
    private AccountantOption? _originalAccountant;
    private DateTimeOffset? _originalStartDate;
    private DateTimeOffset? _originalDueDate;
    private string _originalNotes = string.Empty;
    private List<(string? ItemId, string Quantity, string RateType, string RateAmount, string SecurityDeposit)> _originalLineItems = [];

    /// <summary>
    /// Line items in the Add/Edit modal.
    /// </summary>
    public ObservableCollection<RentalModalLineItem> RentalLineItems { get; } = [];

    /// <summary>
    /// Total security deposit across all line items.
    /// </summary>
    [ObservableProperty]
    private string _totalSecurityDeposit = "$0.00";

    /// <summary>
    /// Total estimated amount across all line items.
    /// </summary>
    [ObservableProperty]
    private string _totalEstimatedAmount = "$0.00";

    /// <summary>
    /// Returns true if any data has been entered in the Add modal.
    /// </summary>
    public bool HasAddModalEnteredData =>
        ModalCustomer != null ||
        ModalAccountant != null ||
        RentalLineItems.Any(li => li.SelectedItem != null || !string.IsNullOrWhiteSpace(li.RateAmount)) ||
        !string.IsNullOrWhiteSpace(ModalNotes);

    /// <summary>
    /// Returns true if any changes have been made in the Edit modal.
    /// </summary>
    public bool HasEditModalChanges
    {
        get
        {
            if (ModalCustomer?.Id != _originalCustomer?.Id ||
                ModalAccountant?.Id != _originalAccountant?.Id ||
                ModalStartDate != _originalStartDate ||
                ModalDueDate != _originalDueDate ||
                ModalNotes != _originalNotes)
                return true;

            if (RentalLineItems.Count != _originalLineItems.Count)
                return true;

            for (int i = 0; i < RentalLineItems.Count; i++)
            {
                var current = RentalLineItems[i];
                var original = _originalLineItems[i];
                if (current.SelectedItem?.Id != original.ItemId ||
                    current.Quantity != original.Quantity ||
                    current.RateType != original.RateType ||
                    current.RateAmount != original.RateAmount ||
                    current.SecurityDeposit != original.SecurityDeposit)
                    return true;
            }

            return false;
        }
    }

    partial void OnModalCustomerChanged(CustomerOption? value)
    {
        if (value != null)
        {
            ModalCustomerError = null;
        }
    }

    /// <summary>
    /// Updates total security deposit and estimated amount from all line items.
    /// </summary>
    public void UpdateLineItemTotals()
    {
        var totalDeposit = RentalLineItems.Sum(li => decimal.TryParse(li.SecurityDeposit, out var d) ? d : 0);
        var totalAmount = RentalLineItems.Sum(li => li.Amount);
        TotalSecurityDeposit = $"${totalDeposit:N2}";
        TotalEstimatedAmount = $"${totalAmount:N2}";
    }

    [RelayCommand]
    public void AddRentalLineItem()
    {
        var lineItem = new RentalModalLineItem(this);
        RentalLineItems.Add(lineItem);
        ModalLineItemsError = null;
        UpdateLineItemTotals();
    }

    [RelayCommand]
    public void RemoveRentalLineItem(RentalModalLineItem? item)
    {
        if (item != null && RentalLineItems.Count > 1)
        {
            RentalLineItems.Remove(item);
            UpdateLineItemTotals();
        }
    }

    #endregion

    #region Return Modal Fields

    [ObservableProperty]
    private string _returnRecordId = string.Empty;

    [ObservableProperty]
    private string _returnItemName = string.Empty;

    [ObservableProperty]
    private string _returnCustomerName = string.Empty;

    [ObservableProperty]
    private int _returnQuantity;

    [ObservableProperty]
    private string _returnRateType = "Daily";

    [ObservableProperty]
    private decimal _returnRateAmount;

    [ObservableProperty]
    private DateTimeOffset? _returnDate = DateTimeOffset.Now;

    [ObservableProperty]
    private decimal _returnTotalCost;

    [ObservableProperty]
    private decimal _returnDeposit;

    [ObservableProperty]
    private bool _returnRefundDeposit = true;

    [ObservableProperty]
    private string _returnNotes = string.Empty;

    [ObservableProperty]
    private bool _returnMarkAsPaid;

    private RentalRecord? _returningRecord;

    public string ReturnRateFormatted => $"${ReturnRateAmount:N2}/{ReturnRateType}";
    public string ReturnTotalCostFormatted => $"${ReturnTotalCost:N2}";
    public string ReturnDepositFormatted => $"${ReturnDeposit:N2}";

    #endregion

    #region View Modal Fields

    [ObservableProperty]
    private string _viewRecordId = string.Empty;

    [ObservableProperty]
    private string _viewItemName = string.Empty;

    [ObservableProperty]
    private string _viewCustomerName = string.Empty;

    [ObservableProperty]
    private string _viewAccountantName = string.Empty;

    [ObservableProperty]
    private int _viewQuantity;

    [ObservableProperty]
    private string _viewRateType = string.Empty;

    [ObservableProperty]
    private decimal _viewRateAmount;

    [ObservableProperty]
    private decimal _viewSecurityDeposit;

    [ObservableProperty]
    private DateTime _viewStartDate;

    [ObservableProperty]
    private DateTime _viewDueDate;

    [ObservableProperty]
    private DateTime? _viewReturnDate;

    [ObservableProperty]
    private string _viewStatus = string.Empty;

    [ObservableProperty]
    private decimal _viewTotalCost;

    [ObservableProperty]
    private decimal? _viewDepositRefundedAmount;

    [ObservableProperty]
    private string _viewNotes = string.Empty;

    [ObservableProperty]
    private int _viewDaysOverdue;

    [ObservableProperty]
    private bool _viewHasMultipleItems;

    /// <summary>
    /// Line items for display in the view modal.
    /// </summary>
    public ObservableCollection<RentalViewLineItemDisplay> ViewLineItems { get; } = [];

    public string ViewRateFormatted => $"${ViewRateAmount:N2}/{ViewRateType}";
    public string ViewDepositFormatted => $"${ViewSecurityDeposit:N2}";
    public string ViewTotalCostFormatted => $"${ViewTotalCost:N2}";
    public string ViewStartDateFormatted => ViewStartDate.ToString("MMMM d, yyyy");
    public string ViewDueDateFormatted => ViewDueDate.ToString("MMMM d, yyyy");
    public string ViewReturnDateFormatted => ViewReturnDate?.ToString("MMMM d, yyyy") ?? "Not returned";
    public string ViewDepositStatusFormatted =>
        ViewDepositRefundedAmount is > 0
            ? $"Refunded (${ViewDepositRefundedAmount.Value:N2})"
            : ViewStatus == "Returned"
                ? (ViewDepositRefundedAmount == 0 ? "Not Refunded" : "Refunded")
                : "Held";

    #endregion

    #region Filter Fields

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string? _filterCustomer;

    [ObservableProperty]
    private string? _filterItem;

    [ObservableProperty]
    private DateTimeOffset? _filterStartDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterStartDateTo;

    [ObservableProperty]
    private DateTimeOffset? _filterDueDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterDueDateTo;

    #endregion

    #region Dropdown Options

    public ObservableCollection<RentalItemOption> AvailableItems { get; } = [];
    public ObservableCollection<CustomerOption> AvailableCustomers { get; } = [];
    public ObservableCollection<AccountantOption> AvailableAccountants { get; } = [];
    public ObservableCollection<string> RateTypeOptions { get; } = ["Daily", "Weekly", "Monthly"];
    public ObservableCollection<string> StatusOptions { get; } = ["All", "Active", "Returned", "Overdue", "Cancelled"];

    #endregion

    #region Events

    public event EventHandler? RecordSaved;
    public event EventHandler? RecordDeleted;
    public event EventHandler? RecordReturned;
    public event EventHandler? FiltersApplied;
    public event EventHandler? FiltersCleared;

    #endregion

    #region Add Record

    [RelayCommand]
    public void OpenAddModal()
    {
        _editingRecord = null;
        ClearModalFields();
        UpdateDropdownOptions();
        IsAddModalOpen = true;
    }

    [RelayCommand]
    public void CloseAddModal()
    {
        IsAddModalOpen = false;
        ClearModalFields();
    }

    /// <summary>
    /// Requests to close the Add modal, showing confirmation if data was entered.
    /// </summary>
    [RelayCommand]
    public async Task RequestCloseAddModalAsync()
    {
        if (HasAddModalEnteredData)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Changes?".Translate(),
                    Message = "You have entered data that will be lost. Are you sure you want to close?".Translate(),
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                    return;
            }
        }

        CloseAddModal();
    }

    /// <summary>
    /// Navigates to Rental Inventory page and opens the create item modal.
    /// </summary>
    [RelayCommand]
    private void NavigateToCreateRentalItem()
    {
        IsAddModalOpen = false;
        IsEditModalOpen = false;
        App.NavigationService?.NavigateTo("RentalInventory", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    /// <summary>
    /// Navigates to Customers page and opens the create customer modal.
    /// </summary>
    [RelayCommand]
    private void NavigateToCreateCustomer()
    {
        IsAddModalOpen = false;
        IsEditModalOpen = false;
        App.NavigationService?.NavigateTo("Customers", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    [RelayCommand]
    public void SaveNewRecord()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null || ModalCustomer == null || RentalLineItems.Count == 0)
            return;

        // Build line items and check availability
        var lineItems = new List<RentalLineItem>();
        var inventoryChanges = new List<(RentalItem Item, int OldAvailable, int OldRented, int QtyChange)>();

        var hasAvailabilityIssue = false;
        foreach (var li in RentalLineItems)
        {
            if (li.SelectedItem == null)
            {
                li.HasItemError = true;
                li.ItemError = "Please select an item.".Translate();
                hasAvailabilityIssue = true;
                continue;
            }
            var rentQty = int.TryParse(li.Quantity, out var q) ? q : 1;
            var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == li.SelectedItem.Id);
            if (item == null || item.AvailableQuantity < rentQty)
            {
                li.QuantityError = item == null ? "Item not found." : $"Only {item.AvailableQuantity} available.";
                hasAvailabilityIssue = true;
                continue;
            }

            lineItems.Add(new RentalLineItem
            {
                RentalItemId = li.SelectedItem.Id,
                Quantity = rentQty,
                RateType = li.RateType switch
                {
                    "Weekly" => RateType.Weekly,
                    "Monthly" => RateType.Monthly,
                    _ => RateType.Daily
                },
                RateAmount = decimal.TryParse(li.RateAmount, out var r) ? r : 0,
                SecurityDeposit = decimal.TryParse(li.SecurityDeposit, out var d) ? d : 0
            });

            inventoryChanges.Add((item, item.AvailableQuantity, item.RentedQuantity, rentQty));
        }

        if (hasAvailabilityIssue)
            return;

        companyData.IdCounters.Rental++;
        var newId = $"RNT-{companyData.IdCounters.Rental:D3}";

        // Use first line item for top-level backward-compatible fields
        var firstLi = lineItems[0];
        var totalDeposit = lineItems.Sum(li => li.SecurityDeposit * li.Quantity);
        var totalQty = lineItems.Sum(li => li.Quantity);

        var newRecord = new RentalRecord
        {
            Id = newId,
            RentalItemId = firstLi.RentalItemId,
            CustomerId = ModalCustomer!.Id!,
            AccountantId = ModalAccountant?.Id,
            Quantity = totalQty,
            RateType = firstLi.RateType,
            RateAmount = firstLi.RateAmount,
            SecurityDeposit = totalDeposit,
            LineItems = lineItems,
            StartDate = ModalStartDate?.DateTime ?? DateTime.Today,
            DueDate = ModalDueDate?.DateTime ?? DateTime.Today.AddDays(1),
            Status = RentalStatus.Active,
            Notes = ModalNotes.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Update inventory quantities
        foreach (var (item, _, _, qtyChange) in inventoryChanges)
        {
            item.AvailableQuantity -= qtyChange;
            item.RentedQuantity += qtyChange;
            item.UpdatedAt = DateTime.UtcNow;
        }

        companyData.Rentals.Add(newRecord);
        companyData.MarkAsModified();

        var recordToUndo = newRecord;
        var invChanges = inventoryChanges;
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Create rental '{newId}'",
            () =>
            {
                companyData.Rentals.Remove(recordToUndo);
                foreach (var (item, oldAvail, oldRented, _) in invChanges)
                {
                    item.AvailableQuantity = oldAvail;
                    item.RentedQuantity = oldRented;
                }
                companyData.MarkAsModified();
                RecordSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Rentals.Add(recordToUndo);
                foreach (var (item, _, _, qtyChange) in invChanges)
                {
                    item.AvailableQuantity -= qtyChange;
                    item.RentedQuantity += qtyChange;
                }
                companyData.MarkAsModified();
                RecordSaved?.Invoke(this, EventArgs.Empty);
                App.CheckAndNotifyRentalOverdue(recordToUndo);
            }));

        RecordSaved?.Invoke(this, EventArgs.Empty);
        App.CheckAndNotifyRentalOverdue(newRecord);
        CloseAddModal();
    }

    #endregion

    #region Edit Record

    public void OpenEditModal(RentalRecordDisplayItem? record)
    {
        if (record == null || !record.IsActive)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        var rentalRecord = companyData?.Rentals.FirstOrDefault(r => r.Id == record.Id);
        if (rentalRecord == null)
            return;

        _editingRecord = rentalRecord;
        UpdateDropdownOptions();

        ModalCustomer = AvailableCustomers.FirstOrDefault(c => c.Id == rentalRecord.CustomerId);
        ModalAccountant = AvailableAccountants.FirstOrDefault(a => a.Id == rentalRecord.AccountantId);
        ModalStartDate = new DateTimeOffset(rentalRecord.StartDate);
        ModalDueDate = new DateTimeOffset(rentalRecord.DueDate);
        ModalNotes = rentalRecord.Notes;

        // Populate line items from record
        RentalLineItems.Clear();
        if (rentalRecord.LineItems.Count > 0)
        {
            foreach (var li in rentalRecord.LineItems)
            {
                var lineItem = new RentalModalLineItem(this)
                {
                    SelectedItem = AvailableItems.FirstOrDefault(i => i.Id == li.RentalItemId),
                    Quantity = li.Quantity.ToString(),
                    RateType = li.RateType.ToString(),
                    RateAmount = li.RateAmount.ToString("0.00"),
                    SecurityDeposit = li.SecurityDeposit.ToString("0.00")
                };
                RentalLineItems.Add(lineItem);
            }
        }
        else
        {
            // Legacy single-item record
            var lineItem = new RentalModalLineItem(this)
            {
                SelectedItem = AvailableItems.FirstOrDefault(i => i.Id == rentalRecord.RentalItemId),
                Quantity = rentalRecord.Quantity.ToString(),
                RateType = rentalRecord.RateType.ToString(),
                RateAmount = rentalRecord.RateAmount.ToString("0.00"),
                SecurityDeposit = rentalRecord.SecurityDeposit.ToString("0.00")
            };
            RentalLineItems.Add(lineItem);
        }
        UpdateLineItemTotals();

        // Store original values for change detection
        _originalCustomer = ModalCustomer;
        _originalAccountant = ModalAccountant;
        _originalStartDate = ModalStartDate;
        _originalDueDate = ModalDueDate;
        _originalNotes = ModalNotes;
        _originalLineItems = RentalLineItems.Select(li =>
            (li.SelectedItem?.Id, li.Quantity, li.RateType, li.RateAmount, li.SecurityDeposit)).ToList();

        ClearModalErrors();
        IsEditModalOpen = true;
    }

    [RelayCommand]
    public void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingRecord = null;
        ClearModalFields();
    }

    /// <summary>
    /// Requests to close the Edit modal, showing confirmation if changes were made.
    /// </summary>
    [RelayCommand]
    public async Task RequestCloseEditModalAsync()
    {
        if (HasEditModalChanges)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Changes?".Translate(),
                    Message = "You have unsaved changes that will be lost. Are you sure you want to close?".Translate(),
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                    return;
            }
        }

        CloseEditModal();
    }

    [RelayCommand]
    public void SaveEditedRecord()
    {
        if (!ValidateModal() || _editingRecord == null || ModalCustomer == null || RentalLineItems.Count == 0)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Save old values for undo
        var oldCustomerId = _editingRecord.CustomerId;
        var oldAccountantId = _editingRecord.AccountantId;
        var oldItemId = _editingRecord.RentalItemId;
        var oldQty = _editingRecord.Quantity;
        var oldRateType = _editingRecord.RateType;
        var oldRateAmount = _editingRecord.RateAmount;
        var oldDeposit = _editingRecord.SecurityDeposit;
        var oldLineItems = _editingRecord.LineItems.Select(li => new RentalLineItem
        {
            RentalItemId = li.RentalItemId, Quantity = li.Quantity,
            RateType = li.RateType, RateAmount = li.RateAmount, SecurityDeposit = li.SecurityDeposit
        }).ToList();
        var oldStartDate = _editingRecord.StartDate;
        var oldDueDate = _editingRecord.DueDate;
        var oldNotes = _editingRecord.Notes;

        // Build new line items
        var newLineItems = new List<RentalLineItem>();
        foreach (var li in RentalLineItems)
        {
            if (li.SelectedItem == null) return;
            newLineItems.Add(new RentalLineItem
            {
                RentalItemId = li.SelectedItem.Id,
                Quantity = int.TryParse(li.Quantity, out var q) ? q : 1,
                RateType = li.RateType switch
                {
                    "Weekly" => RateType.Weekly,
                    "Monthly" => RateType.Monthly,
                    _ => RateType.Daily
                },
                RateAmount = decimal.TryParse(li.RateAmount, out var r) ? r : 0,
                SecurityDeposit = decimal.TryParse(li.SecurityDeposit, out var d) ? d : 0
            });
        }

        // Calculate inventory changes - restore old, apply new
        var oldInventoryState = new Dictionary<string, (int Available, int Rented)>();
        var effectiveOldLineItems = oldLineItems.Count > 0 ? oldLineItems :
        [
            new RentalLineItem { RentalItemId = oldItemId, Quantity = oldQty, RateType = oldRateType, RateAmount = oldRateAmount, SecurityDeposit = oldDeposit }
        ];

        // Capture current inventory state and restore old quantities
        foreach (var oldLi in effectiveOldLineItems)
        {
            var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == oldLi.RentalItemId);
            if (item != null && !oldInventoryState.ContainsKey(item.Id))
                oldInventoryState[item.Id] = (item.AvailableQuantity, item.RentedQuantity);
        }
        foreach (var newLi in newLineItems)
        {
            var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == newLi.RentalItemId);
            if (item != null && !oldInventoryState.ContainsKey(item.Id))
                oldInventoryState[item.Id] = (item.AvailableQuantity, item.RentedQuantity);
        }

        // Restore old quantities first
        foreach (var oldLi in effectiveOldLineItems)
        {
            var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == oldLi.RentalItemId);
            if (item != null)
            {
                item.AvailableQuantity += oldLi.Quantity;
                item.RentedQuantity -= oldLi.Quantity;
            }
        }
        // Apply new quantities
        foreach (var newLi in newLineItems)
        {
            var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == newLi.RentalItemId);
            if (item != null)
            {
                item.AvailableQuantity -= newLi.Quantity;
                item.RentedQuantity += newLi.Quantity;
            }
        }

        // Update record
        var recordToEdit = _editingRecord;
        var firstLi = newLineItems[0];
        var newCustomerId = ModalCustomer!.Id;
        var newAccountantId = ModalAccountant?.Id;
        var newStartDate = ModalStartDate?.DateTime ?? DateTime.Today;
        var newDueDate = ModalDueDate?.DateTime ?? DateTime.Today.AddDays(1);
        var newNotes = ModalNotes.Trim();
        var totalDeposit = newLineItems.Sum(li => li.SecurityDeposit * li.Quantity);
        var totalQty = newLineItems.Sum(li => li.Quantity);

        recordToEdit.RentalItemId = firstLi.RentalItemId;
        recordToEdit.CustomerId = newCustomerId!;
        recordToEdit.AccountantId = newAccountantId;
        recordToEdit.Quantity = totalQty;
        recordToEdit.RateType = firstLi.RateType;
        recordToEdit.RateAmount = firstLi.RateAmount;
        recordToEdit.SecurityDeposit = totalDeposit;
        recordToEdit.LineItems = newLineItems;
        recordToEdit.StartDate = newStartDate;
        recordToEdit.DueDate = newDueDate;
        recordToEdit.Notes = newNotes;
        recordToEdit.UpdatedAt = DateTime.UtcNow;

        companyData.MarkAsModified();

        var savedOldInventory = oldInventoryState;
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Edit rental '{recordToEdit.Id}'",
            () =>
            {
                recordToEdit.RentalItemId = oldItemId;
                recordToEdit.CustomerId = oldCustomerId;
                recordToEdit.AccountantId = oldAccountantId;
                recordToEdit.Quantity = oldQty;
                recordToEdit.RateType = oldRateType;
                recordToEdit.RateAmount = oldRateAmount;
                recordToEdit.SecurityDeposit = oldDeposit;
                recordToEdit.LineItems = oldLineItems;
                recordToEdit.StartDate = oldStartDate;
                recordToEdit.DueDate = oldDueDate;
                recordToEdit.Notes = oldNotes;

                // Restore old inventory state
                foreach (var (id, (avail, rented)) in savedOldInventory)
                {
                    var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == id);
                    if (item != null)
                    {
                        item.AvailableQuantity = avail;
                        item.RentedQuantity = rented;
                    }
                }

                companyData.MarkAsModified();
                RecordSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                recordToEdit.RentalItemId = firstLi.RentalItemId;
                recordToEdit.CustomerId = newCustomerId!;
                recordToEdit.AccountantId = newAccountantId;
                recordToEdit.Quantity = totalQty;
                recordToEdit.RateType = firstLi.RateType;
                recordToEdit.RateAmount = firstLi.RateAmount;
                recordToEdit.SecurityDeposit = totalDeposit;
                recordToEdit.LineItems = newLineItems;
                recordToEdit.StartDate = newStartDate;
                recordToEdit.DueDate = newDueDate;
                recordToEdit.Notes = newNotes;

                // Re-apply new inventory state
                foreach (var (id, (avail, rented)) in savedOldInventory)
                {
                    var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == id);
                    if (item != null)
                    {
                        item.AvailableQuantity = avail;
                        item.RentedQuantity = rented;
                    }
                }
                foreach (var oldLi in effectiveOldLineItems)
                {
                    var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == oldLi.RentalItemId);
                    if (item != null)
                    {
                        item.AvailableQuantity += oldLi.Quantity;
                        item.RentedQuantity -= oldLi.Quantity;
                    }
                }
                foreach (var nli in newLineItems)
                {
                    var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == nli.RentalItemId);
                    if (item != null)
                    {
                        item.AvailableQuantity -= nli.Quantity;
                        item.RentedQuantity += nli.Quantity;
                    }
                }

                companyData.MarkAsModified();
                RecordSaved?.Invoke(this, EventArgs.Empty);
                App.CheckAndNotifyRentalOverdue(recordToEdit);
            }));

        RecordSaved?.Invoke(this, EventArgs.Empty);
        App.CheckAndNotifyRentalOverdue(recordToEdit);
        CloseEditModal();
    }

    #endregion

    #region Delete Record

    public async void OpenDeleteConfirm(RentalRecordDisplayItem? record)
    {
        if (record == null)
            return;

        var dialog = App.ConfirmationDialog;
        if (dialog == null)
            return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Delete Rental Record".Translate(),
            Message = "Are you sure you want to delete this rental record?\n\nRecord ID: {0}".TranslateFormat(record.Id),
            PrimaryButtonText = "Delete".Translate(),
            CancelButtonText = "Cancel".Translate(),
            IsPrimaryDestructive = true
        });

        if (result != ConfirmationResult.Primary)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var rentalRecord = companyData.Rentals.FirstOrDefault(r => r.Id == record.Id);
        if (rentalRecord != null)
        {
            var deletedRecord = rentalRecord;
            var wasActive = rentalRecord.Status == RentalStatus.Active || rentalRecord.Status == RentalStatus.Overdue;

            // Capture inventory state and restore if active
            var inventorySnapshot = new Dictionary<string, (int Available, int Rented)>();
            if (wasActive)
            {
                var effectiveItems = GetEffectiveLineItems(rentalRecord);
                foreach (var li in effectiveItems)
                {
                    var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == li.RentalItemId);
                    if (item != null)
                    {
                        if (!inventorySnapshot.ContainsKey(item.Id))
                            inventorySnapshot[item.Id] = (item.AvailableQuantity, item.RentedQuantity);
                        item.AvailableQuantity += li.Quantity;
                        item.RentedQuantity -= li.Quantity;
                    }
                }
            }

            companyData.Rentals.Remove(rentalRecord);
            companyData.MarkAsModified();

            var savedSnapshot = inventorySnapshot;
            App.UndoRedoManager.RecordAction(new DelegateAction(
                $"Delete rental '{deletedRecord.Id}'",
                () =>
                {
                    companyData.Rentals.Add(deletedRecord);
                    if (wasActive)
                    {
                        foreach (var (id, (avail, rented)) in savedSnapshot)
                        {
                            var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == id);
                            if (item != null)
                            {
                                item.AvailableQuantity = avail;
                                item.RentedQuantity = rented;
                            }
                        }
                    }
                    companyData.MarkAsModified();
                    RecordDeleted?.Invoke(this, EventArgs.Empty);
                },
                () =>
                {
                    companyData.Rentals.Remove(deletedRecord);
                    if (wasActive)
                    {
                        var effectiveItems = GetEffectiveLineItems(deletedRecord);
                        foreach (var li in effectiveItems)
                        {
                            var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == li.RentalItemId);
                            if (item != null)
                            {
                                item.AvailableQuantity += li.Quantity;
                                item.RentedQuantity -= li.Quantity;
                            }
                        }
                    }
                    companyData.MarkAsModified();
                    RecordDeleted?.Invoke(this, EventArgs.Empty);
                }));
        }

        RecordDeleted?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Return Modal

    public void OpenReturnModal(RentalRecordDisplayItem? record)
    {
        if (record == null || !record.IsActive)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        var rentalRecord = companyData?.Rentals.FirstOrDefault(r => r.Id == record.Id);
        if (rentalRecord == null)
            return;

        _returningRecord = rentalRecord;

        ReturnRecordId = rentalRecord.Id;
        ReturnItemName = record.ItemName;
        ReturnCustomerName = record.CustomerName;
        ReturnQuantity = rentalRecord.Quantity;
        ReturnRateType = rentalRecord.RateType.ToString();
        ReturnRateAmount = rentalRecord.RateAmount;
        ReturnDate = DateTimeOffset.Now;
        ReturnDeposit = rentalRecord.SecurityDeposit;
        ReturnRefundDeposit = true;
        ReturnMarkAsPaid = false;
        ReturnNotes = string.Empty;

        // Calculate total cost from all line items
        var days = ((ReturnDate?.DateTime ?? DateTime.Today) - rentalRecord.StartDate).Days;
        if (days < 1) days = 1;

        var effectiveItems = GetEffectiveLineItems(rentalRecord);
        ReturnTotalCost = effectiveItems.Sum(li => li.RateType switch
        {
            RateType.Daily => li.RateAmount * days * li.Quantity,
            RateType.Weekly => li.RateAmount * (decimal)Math.Ceiling(days / 7.0) * li.Quantity,
            RateType.Monthly => li.RateAmount * (decimal)Math.Ceiling(days / 30.0) * li.Quantity,
            _ => 0
        });

        OnPropertyChanged(nameof(ReturnRateFormatted));
        OnPropertyChanged(nameof(ReturnTotalCostFormatted));
        OnPropertyChanged(nameof(ReturnDepositFormatted));
        IsReturnModalOpen = true;
    }

    [RelayCommand]
    public void CloseReturnModal()
    {
        IsReturnModalOpen = false;
        _returningRecord = null;
    }

    [RelayCommand]
    public void ConfirmReturn()
    {
        if (_returningRecord == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var oldStatus = _returningRecord.Status;
        var oldReturnDate = _returningRecord.ReturnDate;
        var oldTotalCost = _returningRecord.TotalCost;
        var oldDepositRefunded = _returningRecord.DepositRefunded;
        var oldPaid = _returningRecord.Paid;
        var oldNotes = _returningRecord.Notes;

        // Update rental record
        _returningRecord.Status = RentalStatus.Returned;
        _returningRecord.ReturnDate = ReturnDate?.DateTime;
        _returningRecord.TotalCost = ReturnTotalCost;
        _returningRecord.DepositRefunded = ReturnRefundDeposit ? _returningRecord.SecurityDeposit : 0;
        _returningRecord.Paid = ReturnMarkAsPaid;
        if (!string.IsNullOrWhiteSpace(ReturnNotes))
        {
            _returningRecord.Notes = string.IsNullOrWhiteSpace(_returningRecord.Notes)
                ? ReturnNotes.Trim()
                : $"{_returningRecord.Notes}\n\nReturn notes: {ReturnNotes.Trim()}";
        }
        _returningRecord.UpdatedAt = DateTime.UtcNow;

        // Update inventory for all line items
        var inventorySnapshot = new Dictionary<string, (int Available, int Rented)>();
        var effectiveItems = GetEffectiveLineItems(_returningRecord);
        foreach (var li in effectiveItems)
        {
            var invItem = companyData.RentalInventory.FirstOrDefault(i => i.Id == li.RentalItemId);
            if (invItem != null)
            {
                if (!inventorySnapshot.ContainsKey(invItem.Id))
                    inventorySnapshot[invItem.Id] = (invItem.AvailableQuantity, invItem.RentedQuantity);
                invItem.AvailableQuantity += li.Quantity;
                invItem.RentedQuantity -= li.Quantity;
                invItem.UpdatedAt = DateTime.UtcNow;
            }
        }

        companyData.MarkAsModified();

        var recordToReturn = _returningRecord;
        var savedSnapshot = inventorySnapshot;
        var newNotes = _returningRecord.Notes;
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Return rental '{recordToReturn.Id}'",
            () =>
            {
                recordToReturn.Status = oldStatus;
                recordToReturn.ReturnDate = oldReturnDate;
                recordToReturn.TotalCost = oldTotalCost;
                recordToReturn.DepositRefunded = oldDepositRefunded;
                recordToReturn.Paid = oldPaid;
                recordToReturn.Notes = oldNotes;
                foreach (var (id, (avail, rented)) in savedSnapshot)
                {
                    var invItem = companyData.RentalInventory.FirstOrDefault(i => i.Id == id);
                    if (invItem != null)
                    {
                        invItem.AvailableQuantity = avail;
                        invItem.RentedQuantity = rented;
                    }
                }
                companyData.MarkAsModified();
                RecordReturned?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                recordToReturn.Status = RentalStatus.Returned;
                recordToReturn.ReturnDate = ReturnDate?.DateTime;
                recordToReturn.TotalCost = ReturnTotalCost;
                recordToReturn.DepositRefunded = ReturnRefundDeposit ? recordToReturn.SecurityDeposit : 0;
                recordToReturn.Paid = ReturnMarkAsPaid;
                recordToReturn.Notes = newNotes;
                var returnItems = GetEffectiveLineItems(recordToReturn);
                foreach (var li in returnItems)
                {
                    var invItem = companyData.RentalInventory.FirstOrDefault(i => i.Id == li.RentalItemId);
                    if (invItem != null)
                    {
                        invItem.AvailableQuantity += li.Quantity;
                        invItem.RentedQuantity -= li.Quantity;
                    }
                }
                companyData.MarkAsModified();
                RecordReturned?.Invoke(this, EventArgs.Empty);
            }));

        RecordReturned?.Invoke(this, EventArgs.Empty);
        CloseReturnModal();
    }

    #endregion

    #region View Modal

    public void OpenViewModal(RentalRecordDisplayItem? record)
    {
        if (record == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        var rentalRecord = companyData?.Rentals.FirstOrDefault(r => r.Id == record.Id);
        if (rentalRecord == null)
            return;

        var item = companyData?.RentalInventory.FirstOrDefault(i => i.Id == rentalRecord.RentalItemId);
        var customer = companyData?.Customers.FirstOrDefault(c => c.Id == rentalRecord.CustomerId);
        var accountant = companyData?.Accountants.FirstOrDefault(a => a.Id == rentalRecord.AccountantId);

        ViewRecordId = rentalRecord.Id;
        ViewItemName = GetItemDisplayName(rentalRecord, companyData);
        ViewCustomerName = customer?.Name ?? "Unknown Customer";
        ViewAccountantName = accountant?.Name ?? "-";
        ViewQuantity = rentalRecord.Quantity;
        ViewRateType = rentalRecord.RateType.ToString();
        ViewRateAmount = rentalRecord.RateAmount;
        ViewSecurityDeposit = rentalRecord.SecurityDeposit;
        ViewStartDate = rentalRecord.StartDate;
        ViewDueDate = rentalRecord.DueDate;
        ViewReturnDate = rentalRecord.ReturnDate;
        ViewStatus = rentalRecord.Status.ToString();
        ViewTotalCost = rentalRecord.TotalCost ?? 0;
        ViewDepositRefundedAmount = rentalRecord.DepositRefunded;
        ViewNotes = rentalRecord.Notes;
        ViewDaysOverdue = rentalRecord.DaysOverdue;

        // Populate view line items
        ViewLineItems.Clear();
        var effectiveItems = GetEffectiveLineItems(rentalRecord);
        ViewHasMultipleItems = effectiveItems.Count > 1;
        foreach (var li in effectiveItems)
        {
            var liItem = companyData?.RentalInventory.FirstOrDefault(i => i.Id == li.RentalItemId);
            ViewLineItems.Add(new RentalViewLineItemDisplay
            {
                ItemName = liItem?.Name ?? "Unknown Item",
                Quantity = li.Quantity,
                RateType = li.RateType.ToString(),
                RateAmount = li.RateAmount,
                SecurityDeposit = li.SecurityDeposit
            });
        }

        OnPropertyChanged(nameof(ViewRateFormatted));
        OnPropertyChanged(nameof(ViewDepositFormatted));
        OnPropertyChanged(nameof(ViewTotalCostFormatted));
        OnPropertyChanged(nameof(ViewStartDateFormatted));
        OnPropertyChanged(nameof(ViewDueDateFormatted));
        OnPropertyChanged(nameof(ViewReturnDateFormatted));
        OnPropertyChanged(nameof(ViewDepositStatusFormatted));

        IsViewModalOpen = true;
    }

    [RelayCommand]
    public void CloseViewModal()
    {
        IsViewModalOpen = false;
    }

    #endregion

    #region Filter Modal

    // Original filter values for change detection
    private string _originalFilterStatus = "All";
    private string? _originalFilterCustomer;
    private string? _originalFilterItem;
    private DateTimeOffset? _originalFilterStartDateFrom;
    private DateTimeOffset? _originalFilterStartDateTo;
    private DateTimeOffset? _originalFilterDueDateFrom;
    private DateTimeOffset? _originalFilterDueDateTo;

    /// <summary>
    /// Returns true if any filter has been changed from its original value when the modal was opened.
    /// </summary>
    public bool HasFilterModalChanges =>
        FilterStatus != _originalFilterStatus ||
        FilterCustomer != _originalFilterCustomer ||
        FilterItem != _originalFilterItem ||
        FilterStartDateFrom != _originalFilterStartDateFrom ||
        FilterStartDateTo != _originalFilterStartDateTo ||
        FilterDueDateFrom != _originalFilterDueDateFrom ||
        FilterDueDateTo != _originalFilterDueDateTo;

    /// <summary>
    /// Captures the current filter values as the original values for change detection.
    /// </summary>
    private void CaptureOriginalFilterValues()
    {
        _originalFilterStatus = FilterStatus;
        _originalFilterCustomer = FilterCustomer;
        _originalFilterItem = FilterItem;
        _originalFilterStartDateFrom = FilterStartDateFrom;
        _originalFilterStartDateTo = FilterStartDateTo;
        _originalFilterDueDateFrom = FilterDueDateFrom;
        _originalFilterDueDateTo = FilterDueDateTo;
    }

    /// <summary>
    /// Restores filter values to their original values when the modal was opened.
    /// </summary>
    private void RestoreOriginalFilterValues()
    {
        FilterStatus = _originalFilterStatus;
        FilterCustomer = _originalFilterCustomer;
        FilterItem = _originalFilterItem;
        FilterStartDateFrom = _originalFilterStartDateFrom;
        FilterStartDateTo = _originalFilterStartDateTo;
        FilterDueDateFrom = _originalFilterDueDateFrom;
        FilterDueDateTo = _originalFilterDueDateTo;
    }

    [RelayCommand]
    public void OpenFilterModal()
    {
        UpdateDropdownOptions();
        CaptureOriginalFilterValues();
        IsFilterModalOpen = true;
    }

    [RelayCommand]
    public void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    /// <summary>
    /// Requests to close the Filter modal, showing confirmation if filters have been changed.
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

            RestoreOriginalFilterValues();
        }

        CloseFilterModal();
    }

    [RelayCommand]
    public void ApplyFilters()
    {
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        ResetFilterDefaults();
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    /// <summary>
    /// Resets all filter fields to their default values.
    /// </summary>
    private void ResetFilterDefaults()
    {
        FilterStatus = "All";
        FilterCustomer = null;
        FilterItem = null;
        FilterStartDateFrom = null;
        FilterStartDateTo = null;
        FilterDueDateFrom = null;
        FilterDueDateTo = null;
    }

    #endregion

    #region Modal Helpers

    private void UpdateDropdownOptions()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        AvailableItems.Clear();
        foreach (var item in companyData.RentalInventory.Where(i => i.Status == EntityStatus.Active).OrderBy(i => i.Name))
        {
            AvailableItems.Add(new RentalItemOption
            {
                Id = item.Id,
                Name = item.Name,
                AvailableQuantity = item.AvailableQuantity
            });
        }

        AvailableCustomers.Clear();
        foreach (var customer in companyData.Customers.Where(c => c.Status == EntityStatus.Active).OrderBy(c => c.Name))
        {
            AvailableCustomers.Add(new CustomerOption { Id = customer.Id, Name = customer.Name });
        }

        AvailableAccountants.Clear();
        foreach (var accountant in companyData.Accountants.OrderBy(a => a.Name))
        {
            AvailableAccountants.Add(new AccountantOption { Id = accountant.Id, Name = accountant.Name });
        }
    }

    private void ClearModalFields()
    {
        ModalCustomer = null;
        ModalAccountant = null;
        ModalStartDate = DateTimeOffset.Now;
        ModalDueDate = DateTimeOffset.Now.AddDays(1);
        ModalNotes = string.Empty;
        RentalLineItems.Clear();
        AddRentalLineItem();
        ClearModalErrors();
    }

    private void ClearModalErrors()
    {
        ModalCustomerError = null;
        ModalLineItemsError = null;
        foreach (var li in RentalLineItems)
        {
            li.HasItemError = false;
            li.ItemError = null;
            li.QuantityError = null;
            li.RateError = null;
        }
    }

    private bool ValidateModal()
    {
        ClearModalErrors();
        var isValid = true;

        if (ModalCustomer == null)
        {
            ModalCustomerError = "Please select a customer.".Translate();
            isValid = false;
        }

        if (RentalLineItems.Count == 0)
        {
            ModalLineItemsError = "Please add at least one line item.".Translate();
            isValid = false;
        }

        var companyData = App.CompanyManager?.CompanyData;

        foreach (var li in RentalLineItems)
        {
            if (li.SelectedItem == null)
            {
                li.HasItemError = true;
                li.ItemError = "Please select an item.".Translate();
                isValid = false;
            }

            if (!int.TryParse(li.Quantity, out var qty) || qty <= 0)
            {
                li.QuantityError = "Invalid quantity.".Translate();
                isValid = false;
            }
            else if (_editingRecord == null && li.SelectedItem != null)
            {
                var item = companyData?.RentalInventory.FirstOrDefault(i => i.Id == li.SelectedItem.Id);
                if (item == null)
                {
                    li.QuantityError = "Item not found in inventory.".Translate();
                    isValid = false;
                }
                else if (qty > item.AvailableQuantity)
                {
                    li.QuantityError = $"Only {item.AvailableQuantity} available.";
                    isValid = false;
                }
            }
        }

        return isValid;
    }

    /// <summary>
    /// Gets the effective line items from a rental record.
    /// If the record has LineItems, returns those. Otherwise, creates a single line item from the top-level fields.
    /// </summary>
    public static List<RentalLineItem> GetEffectiveLineItems(RentalRecord record)
    {
        if (record.LineItems.Count > 0)
            return record.LineItems;

        return
        [
            new RentalLineItem
            {
                RentalItemId = record.RentalItemId,
                Quantity = record.Quantity,
                RateType = record.RateType,
                RateAmount = record.RateAmount,
                SecurityDeposit = record.SecurityDeposit
            }
        ];
    }

    /// <summary>
    /// Gets a display name for a rental record's items.
    /// Shows item name for single-item, or "Item1, Item2, ..." for multi-item.
    /// </summary>
    public static string GetItemDisplayName(RentalRecord record, Core.Data.CompanyData? companyData)
    {
        var effectiveItems = GetEffectiveLineItems(record);
        if (effectiveItems.Count == 1)
        {
            var item = companyData?.RentalInventory.FirstOrDefault(i => i.Id == effectiveItems[0].RentalItemId);
            return item?.Name ?? "Unknown Item";
        }

        var names = effectiveItems.Select(li =>
        {
            var item = companyData?.RentalInventory.FirstOrDefault(i => i.Id == li.RentalItemId);
            return item?.Name ?? "Unknown";
        });
        return string.Join(", ", names);
    }

    #endregion
}

/// <summary>
/// Option model for rental item dropdown.
/// </summary>
public class RentalItemOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }

    public override string ToString() => $"{Name} ({AvailableQuantity} available)";
}

/// <summary>
/// ViewModel for a single line item in the rental Add/Edit modal.
/// </summary>
public partial class RentalModalLineItem : ObservableObject
{
    private readonly RentalRecordsModalsViewModel _parent;

    public RentalModalLineItem(RentalRecordsModalsViewModel parent)
    {
        _parent = parent;
    }

    [ObservableProperty]
    private RentalItemOption? _selectedItem;

    [ObservableProperty]
    private string _quantity = "1";

    [ObservableProperty]
    private string _rateType = "Daily";

    [ObservableProperty]
    private string _rateAmount = string.Empty;

    [ObservableProperty]
    private string _securityDeposit = string.Empty;

    [ObservableProperty]
    private bool _hasItemError;

    [ObservableProperty]
    private string? _itemError;

    [ObservableProperty]
    private string? _quantityError;

    [ObservableProperty]
    private string? _rateError;

    public decimal Amount
    {
        get
        {
            var qty = int.TryParse(Quantity, out var q) ? q : 0;
            var rate = decimal.TryParse(RateAmount, out var r) ? r : 0;
            return qty * rate;
        }
    }

    public string AmountFormatted => $"${Amount:N2}";
    public string RateAmountDisplay => decimal.TryParse(RateAmount, out var r) ? $"${r:N2}" : "-";
    public string SecurityDepositDisplay => decimal.TryParse(SecurityDeposit, out var d) ? $"${d:N2}" : "-";

    partial void OnSelectedItemChanged(RentalItemOption? value)
    {
        if (value != null)
        {
            HasItemError = false;
            ItemError = null;

            // Auto-populate rate and deposit from the selected item
            var companyData = App.CompanyManager?.CompanyData;
            var item = companyData?.RentalInventory.FirstOrDefault(i => i.Id == value.Id);
            if (item != null)
            {
                RateAmount = RateType switch
                {
                    "Weekly" => item.WeeklyRate.ToString("0.00"),
                    "Monthly" => item.MonthlyRate.ToString("0.00"),
                    _ => item.DailyRate.ToString("0.00")
                };
                SecurityDeposit = item.SecurityDeposit.ToString("0.00");
                OnPropertyChanged(nameof(SecurityDepositDisplay));
            }
        }

        _parent.UpdateLineItemTotals();
    }

    partial void OnQuantityChanged(string value)
    {
        if (int.TryParse(value, out var qty) && qty > 0)
            QuantityError = null;

        OnPropertyChanged(nameof(Amount));
        OnPropertyChanged(nameof(AmountFormatted));
        _parent.UpdateLineItemTotals();
    }

    partial void OnRateAmountChanged(string value)
    {
        if (decimal.TryParse(value, out var rate) && rate >= 0)
            RateError = null;

        OnPropertyChanged(nameof(Amount));
        OnPropertyChanged(nameof(AmountFormatted));
        OnPropertyChanged(nameof(RateAmountDisplay));
        _parent.UpdateLineItemTotals();
    }

    partial void OnRateTypeChanged(string value)
    {
        if (SelectedItem != null)
        {
            var companyData = App.CompanyManager?.CompanyData;
            var item = companyData?.RentalInventory.FirstOrDefault(i => i.Id == SelectedItem.Id);
            if (item != null)
            {
                RateAmount = value switch
                {
                    "Weekly" => item.WeeklyRate.ToString("0.00"),
                    "Monthly" => item.MonthlyRate.ToString("0.00"),
                    _ => item.DailyRate.ToString("0.00")
                };
            }
        }
    }
}

/// <summary>
/// Display model for line items in the view modal.
/// </summary>
public class RentalViewLineItemDisplay
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string RateType { get; set; } = string.Empty;
    public decimal RateAmount { get; set; }
    public decimal SecurityDeposit { get; set; }
    public string RateFormatted => $"${RateAmount:N2}/{RateType}";
    public string DepositFormatted => $"${SecurityDeposit:N2}";
    public string AmountFormatted => $"${Quantity * RateAmount:N2}";
}
