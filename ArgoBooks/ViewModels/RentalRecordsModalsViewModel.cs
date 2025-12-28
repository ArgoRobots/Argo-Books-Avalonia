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
    private RentalItemOption? _modalItem;

    [ObservableProperty]
    private CustomerOption? _modalCustomer;

    [ObservableProperty]
    private AccountantOption? _modalAccountant;

    [ObservableProperty]
    private string _modalQuantity = "1";

    [ObservableProperty]
    private string _modalRateType = "Daily";

    [ObservableProperty]
    private string _modalRateAmount = string.Empty;

    [ObservableProperty]
    private string _modalSecurityDeposit = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _modalStartDate = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset? _modalDueDate = DateTimeOffset.Now.AddDays(1);

    [ObservableProperty]
    private string _modalNotes = string.Empty;

    [ObservableProperty]
    private string? _modalItemError;

    [ObservableProperty]
    private string? _modalCustomerError;

    [ObservableProperty]
    private string? _modalQuantityError;

    [ObservableProperty]
    private string? _modalRateError;

    private RentalRecord? _editingRecord;
    private RentalRecordDisplayItem? _deletingRecord;

    partial void OnModalItemChanged(RentalItemOption? value)
    {
        if (value != null && _editingRecord == null)
        {
            var companyData = App.CompanyManager?.CompanyData;
            var item = companyData?.RentalInventory.FirstOrDefault(i => i.Id == value.Id);
            if (item != null)
            {
                ModalRateAmount = ModalRateType switch
                {
                    "Weekly" => item.WeeklyRate.ToString("0.00"),
                    "Monthly" => item.MonthlyRate.ToString("0.00"),
                    _ => item.DailyRate.ToString("0.00")
                };
                ModalSecurityDeposit = item.SecurityDeposit.ToString("0.00");
            }
        }
    }

    partial void OnModalRateTypeChanged(string value)
    {
        if (ModalItem != null && _editingRecord == null)
        {
            var companyData = App.CompanyManager?.CompanyData;
            var item = companyData?.RentalInventory.FirstOrDefault(i => i.Id == ModalItem.Id);
            if (item != null)
            {
                ModalRateAmount = value switch
                {
                    "Weekly" => item.WeeklyRate.ToString("0.00"),
                    "Monthly" => item.MonthlyRate.ToString("0.00"),
                    _ => item.DailyRate.ToString("0.00")
                };
            }
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
    private DateTimeOffset? _returnDate = DateTimeOffset.Now;

    [ObservableProperty]
    private decimal _returnTotalCost;

    [ObservableProperty]
    private decimal _returnDeposit;

    [ObservableProperty]
    private bool _returnRefundDeposit = true;

    [ObservableProperty]
    private string _returnNotes = string.Empty;

    private RentalRecord? _returningRecord;

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

    public string ViewRateFormatted => $"${ViewRateAmount:N2}/{ViewRateType}";
    public string ViewDepositFormatted => $"${ViewSecurityDeposit:N2}";
    public string ViewTotalCostFormatted => $"${ViewTotalCost:N2}";
    public string ViewStartDateFormatted => ViewStartDate.ToString("MMMM d, yyyy");
    public string ViewDueDateFormatted => ViewDueDate.ToString("MMMM d, yyyy");
    public string ViewReturnDateFormatted => ViewReturnDate?.ToString("MMMM d, yyyy") ?? "Not returned";
    public string ViewDepositStatusFormatted => ViewDepositRefundedAmount is > 0 ? $"Refunded (${ViewDepositRefundedAmount.Value:N2})" : "Held";

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

    [RelayCommand]
    public void SaveNewRecord()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null || ModalItem == null || ModalCustomer == null)
            return;

        var rentQty = int.TryParse(ModalQuantity, out var qty) ? qty : 1;

        // Check availability
        var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == ModalItem.Id);
        if (item == null || item.AvailableQuantity < rentQty)
        {
            ModalQuantityError = item == null ? "Item not found." : $"Only {item.AvailableQuantity} available.";
            return;
        }

        companyData.IdCounters.Rental++;
        var newId = $"RNT-{companyData.IdCounters.Rental:D3}";

        var newRecord = new RentalRecord
        {
            Id = newId,
            RentalItemId = ModalItem!.Id,
            CustomerId = ModalCustomer!.Id!,
            AccountantId = ModalAccountant?.Id,
            Quantity = rentQty,
            RateType = ModalRateType switch
            {
                "Weekly" => RateType.Weekly,
                "Monthly" => RateType.Monthly,
                _ => RateType.Daily
            },
            RateAmount = decimal.TryParse(ModalRateAmount, out var rate) ? rate : 0,
            SecurityDeposit = decimal.TryParse(ModalSecurityDeposit, out var deposit) ? deposit : 0,
            StartDate = ModalStartDate?.DateTime ?? DateTime.Today,
            DueDate = ModalDueDate?.DateTime ?? DateTime.Today.AddDays(1),
            Status = RentalStatus.Active,
            Notes = ModalNotes.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Update inventory quantities
        var oldAvailable = item.AvailableQuantity;
        var oldRented = item.RentedQuantity;
        item.AvailableQuantity -= rentQty;
        item.RentedQuantity += rentQty;
        item.UpdatedAt = DateTime.UtcNow;

        companyData.Rentals.Add(newRecord);
        companyData.MarkAsModified();

        var recordToUndo = newRecord;
        var itemToUpdate = item;
        App.UndoRedoManager?.RecordAction(new RentalRecordAddAction(
            $"Create rental '{newId}'",
            recordToUndo,
            () =>
            {
                companyData.Rentals.Remove(recordToUndo);
                itemToUpdate.AvailableQuantity = oldAvailable;
                itemToUpdate.RentedQuantity = oldRented;
                companyData.MarkAsModified();
                RecordSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Rentals.Add(recordToUndo);
                itemToUpdate.AvailableQuantity -= rentQty;
                itemToUpdate.RentedQuantity += rentQty;
                companyData.MarkAsModified();
                RecordSaved?.Invoke(this, EventArgs.Empty);
            }));

        RecordSaved?.Invoke(this, EventArgs.Empty);
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

        ModalItem = AvailableItems.FirstOrDefault(i => i.Id == rentalRecord.RentalItemId);
        ModalCustomer = AvailableCustomers.FirstOrDefault(c => c.Id == rentalRecord.CustomerId);
        ModalAccountant = AvailableAccountants.FirstOrDefault(a => a.Id == rentalRecord.AccountantId);
        ModalQuantity = rentalRecord.Quantity.ToString();
        ModalRateType = rentalRecord.RateType.ToString();
        ModalRateAmount = rentalRecord.RateAmount.ToString("0.00");
        ModalSecurityDeposit = rentalRecord.SecurityDeposit.ToString("0.00");
        ModalStartDate = new DateTimeOffset(rentalRecord.StartDate);
        ModalDueDate = new DateTimeOffset(rentalRecord.DueDate);
        ModalNotes = rentalRecord.Notes;

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

    [RelayCommand]
    public void SaveEditedRecord()
    {
        if (!ValidateModal() || _editingRecord == null || ModalItem == null || ModalCustomer == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var newQty = int.TryParse(ModalQuantity, out var qty) ? qty : 1;
        var qtyDiff = newQty - _editingRecord.Quantity;

        // Check availability if increasing quantity
        if (qtyDiff > 0)
        {
            var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == ModalItem.Id);
            if (item == null || item.AvailableQuantity < qtyDiff)
            {
                ModalQuantityError = item == null ? "Item not found." : $"Only {item.AvailableQuantity} more available.";
                return;
            }
        }

        var oldItemId = _editingRecord.RentalItemId;
        var oldCustomerId = _editingRecord.CustomerId;
        var oldAccountantId = _editingRecord.AccountantId;
        var oldQty = _editingRecord.Quantity;
        var oldRateType = _editingRecord.RateType;
        var oldRateAmount = _editingRecord.RateAmount;
        var oldDeposit = _editingRecord.SecurityDeposit;
        var oldStartDate = _editingRecord.StartDate;
        var oldDueDate = _editingRecord.DueDate;
        var oldNotes = _editingRecord.Notes;

        var newRateType = ModalRateType switch
        {
            "Weekly" => RateType.Weekly,
            "Monthly" => RateType.Monthly,
            _ => RateType.Daily
        };

        // Capture values for undo/redo lambdas
        var newItemId = ModalItem!.Id;
        var newCustomerId = ModalCustomer!.Id;
        var newAccountantId = ModalAccountant?.Id;

        var recordToEdit = _editingRecord;
        recordToEdit.RentalItemId = newItemId;
        recordToEdit.CustomerId = newCustomerId!;
        recordToEdit.AccountantId = newAccountantId;
        recordToEdit.Quantity = newQty;
        recordToEdit.RateType = newRateType;
        var newRate = decimal.TryParse(ModalRateAmount, out var rate) ? rate : 0;
        var newDeposit = decimal.TryParse(ModalSecurityDeposit, out var deposit) ? deposit : 0;
        var newStartDate = ModalStartDate?.DateTime ?? DateTime.Today;
        var newDueDate = ModalDueDate?.DateTime ?? DateTime.Today.AddDays(1);
        var newNotes = ModalNotes.Trim();

        recordToEdit.RateAmount = newRate;
        recordToEdit.SecurityDeposit = newDeposit;
        recordToEdit.StartDate = newStartDate;
        recordToEdit.DueDate = newDueDate;
        recordToEdit.Notes = newNotes;
        recordToEdit.UpdatedAt = DateTime.UtcNow;

        // Update inventory if quantity changed
        if (qtyDiff != 0)
        {
            var currentItem = companyData.RentalInventory.FirstOrDefault(i => i.Id == newItemId);
            if (currentItem != null)
            {
                currentItem.AvailableQuantity -= qtyDiff;
                currentItem.RentedQuantity += qtyDiff;
            }
        }

        companyData.MarkAsModified();

        App.UndoRedoManager?.RecordAction(new RentalRecordEditAction(
            $"Edit rental '{recordToEdit.Id}'",
            recordToEdit,
            () =>
            {
                recordToEdit.RentalItemId = oldItemId;
                recordToEdit.CustomerId = oldCustomerId;
                recordToEdit.AccountantId = oldAccountantId;
                recordToEdit.Quantity = oldQty;
                recordToEdit.RateType = oldRateType;
                recordToEdit.RateAmount = oldRateAmount;
                recordToEdit.SecurityDeposit = oldDeposit;
                recordToEdit.StartDate = oldStartDate;
                recordToEdit.DueDate = oldDueDate;
                recordToEdit.Notes = oldNotes;

                if (qtyDiff != 0)
                {
                    var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == newItemId);
                    if (item != null)
                    {
                        item.AvailableQuantity += qtyDiff;
                        item.RentedQuantity -= qtyDiff;
                    }
                }

                companyData.MarkAsModified();
                RecordSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                recordToEdit.RentalItemId = newItemId;
                recordToEdit.CustomerId = newCustomerId!;
                recordToEdit.AccountantId = newAccountantId;
                recordToEdit.Quantity = newQty;
                recordToEdit.RateType = newRateType;
                recordToEdit.RateAmount = newRate;
                recordToEdit.SecurityDeposit = newDeposit;
                recordToEdit.StartDate = newStartDate;
                recordToEdit.DueDate = newDueDate;
                recordToEdit.Notes = newNotes;

                if (qtyDiff != 0)
                {
                    var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == newItemId);
                    if (item != null)
                    {
                        item.AvailableQuantity -= qtyDiff;
                        item.RentedQuantity += qtyDiff;
                    }
                }

                companyData.MarkAsModified();
                RecordSaved?.Invoke(this, EventArgs.Empty);
            }));

        RecordSaved?.Invoke(this, EventArgs.Empty);
        CloseEditModal();
    }

    #endregion

    #region Delete Record

    public void OpenDeleteConfirm(RentalRecordDisplayItem? record)
    {
        if (record == null)
            return;

        _deletingRecord = record;
        OnPropertyChanged(nameof(DeletingRecordId));
        IsDeleteConfirmOpen = true;
    }

    [RelayCommand]
    public void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingRecord = null;
    }

    [RelayCommand]
    public void ConfirmDelete()
    {
        if (_deletingRecord == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var record = companyData.Rentals.FirstOrDefault(r => r.Id == _deletingRecord.Id);
        if (record != null)
        {
            var deletedRecord = record;

            // Restore inventory if record was active
            RentalItem? item = null;
            int oldAvailable = 0, oldRented = 0;
            if (record.Status == RentalStatus.Active || record.Status == RentalStatus.Overdue)
            {
                item = companyData.RentalInventory.FirstOrDefault(i => i.Id == record.RentalItemId);
                if (item != null)
                {
                    oldAvailable = item.AvailableQuantity;
                    oldRented = item.RentedQuantity;
                    item.AvailableQuantity += record.Quantity;
                    item.RentedQuantity -= record.Quantity;
                }
            }

            companyData.Rentals.Remove(record);
            companyData.MarkAsModified();

            var itemToUpdate = item;
            var wasActive = record.Status == RentalStatus.Active || record.Status == RentalStatus.Overdue;
            App.UndoRedoManager?.RecordAction(new RentalRecordDeleteAction(
                $"Delete rental '{deletedRecord.Id}'",
                deletedRecord,
                () =>
                {
                    companyData.Rentals.Add(deletedRecord);
                    if (wasActive && itemToUpdate != null)
                    {
                        itemToUpdate.AvailableQuantity = oldAvailable;
                        itemToUpdate.RentedQuantity = oldRented;
                    }
                    companyData.MarkAsModified();
                    RecordDeleted?.Invoke(this, EventArgs.Empty);
                },
                () =>
                {
                    companyData.Rentals.Remove(deletedRecord);
                    if (wasActive && itemToUpdate != null)
                    {
                        itemToUpdate.AvailableQuantity += deletedRecord.Quantity;
                        itemToUpdate.RentedQuantity -= deletedRecord.Quantity;
                    }
                    companyData.MarkAsModified();
                    RecordDeleted?.Invoke(this, EventArgs.Empty);
                }));
        }

        RecordDeleted?.Invoke(this, EventArgs.Empty);
        CloseDeleteConfirm();
    }

    public string DeletingRecordId => _deletingRecord?.Id ?? string.Empty;

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
        ReturnDate = DateTimeOffset.Now;
        ReturnDeposit = rentalRecord.SecurityDeposit;
        ReturnRefundDeposit = true;
        ReturnNotes = string.Empty;

        // Calculate total cost
        var days = ((ReturnDate?.DateTime ?? DateTime.Today) - rentalRecord.StartDate).Days;
        if (days < 1) days = 1;
        ReturnTotalCost = rentalRecord.RateType switch
        {
            RateType.Daily => rentalRecord.RateAmount * days * rentalRecord.Quantity,
            RateType.Weekly => rentalRecord.RateAmount * (decimal)Math.Ceiling(days / 7.0) * rentalRecord.Quantity,
            RateType.Monthly => rentalRecord.RateAmount * (decimal)Math.Ceiling(days / 30.0) * rentalRecord.Quantity,
            _ => 0
        };

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
        var oldNotes = _returningRecord.Notes;

        // Update rental record
        _returningRecord.Status = RentalStatus.Returned;
        _returningRecord.ReturnDate = ReturnDate?.DateTime;
        _returningRecord.TotalCost = ReturnTotalCost;
        _returningRecord.DepositRefunded = ReturnRefundDeposit ? _returningRecord.SecurityDeposit : 0;
        if (!string.IsNullOrWhiteSpace(ReturnNotes))
        {
            _returningRecord.Notes = string.IsNullOrWhiteSpace(_returningRecord.Notes)
                ? ReturnNotes.Trim()
                : $"{_returningRecord.Notes}\n\nReturn notes: {ReturnNotes.Trim()}";
        }
        _returningRecord.UpdatedAt = DateTime.UtcNow;

        // Update inventory
        var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == _returningRecord.RentalItemId);
        int oldAvailable = 0, oldRented = 0;
        if (item != null)
        {
            oldAvailable = item.AvailableQuantity;
            oldRented = item.RentedQuantity;
            item.AvailableQuantity += _returningRecord.Quantity;
            item.RentedQuantity -= _returningRecord.Quantity;
            item.UpdatedAt = DateTime.UtcNow;
        }

        companyData.MarkAsModified();

        var recordToReturn = _returningRecord;
        var itemToUpdate = item;
        var returnQty = _returningRecord.Quantity;
        var newNotes = _returningRecord.Notes;
        App.UndoRedoManager?.RecordAction(new RentalReturnAction(
            $"Return rental '{recordToReturn.Id}'",
            recordToReturn,
            () =>
            {
                recordToReturn.Status = oldStatus;
                recordToReturn.ReturnDate = oldReturnDate;
                recordToReturn.TotalCost = oldTotalCost;
                recordToReturn.DepositRefunded = oldDepositRefunded;
                recordToReturn.Notes = oldNotes;
                if (itemToUpdate != null)
                {
                    itemToUpdate.AvailableQuantity = oldAvailable;
                    itemToUpdate.RentedQuantity = oldRented;
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
                recordToReturn.Notes = newNotes;
                if (itemToUpdate != null)
                {
                    itemToUpdate.AvailableQuantity += returnQty;
                    itemToUpdate.RentedQuantity -= returnQty;
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
        ViewItemName = item?.Name ?? "Unknown Item";
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

    [RelayCommand]
    public void OpenFilterModal()
    {
        UpdateDropdownOptions();
        IsFilterModalOpen = true;
    }

    [RelayCommand]
    public void CloseFilterModal()
    {
        IsFilterModalOpen = false;
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
        FilterStatus = "All";
        FilterCustomer = null;
        FilterItem = null;
        FilterStartDateFrom = null;
        FilterStartDateTo = null;
        FilterDueDateFrom = null;
        FilterDueDateTo = null;
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
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
        ModalItem = null;
        ModalCustomer = null;
        ModalAccountant = null;
        ModalQuantity = "1";
        ModalRateType = "Daily";
        ModalRateAmount = string.Empty;
        ModalSecurityDeposit = string.Empty;
        ModalStartDate = DateTimeOffset.Now;
        ModalDueDate = DateTimeOffset.Now.AddDays(1);
        ModalNotes = string.Empty;
        ClearModalErrors();
    }

    private void ClearModalErrors()
    {
        ModalItemError = null;
        ModalCustomerError = null;
        ModalQuantityError = null;
        ModalRateError = null;
    }

    private bool ValidateModal()
    {
        ClearModalErrors();
        var isValid = true;

        if (ModalItem == null)
        {
            ModalItemError = "Please select an item.";
            isValid = false;
        }

        if (ModalCustomer == null)
        {
            ModalCustomerError = "Please select a customer.";
            isValid = false;
        }

        if (!int.TryParse(ModalQuantity, out var qty) || qty <= 0)
        {
            ModalQuantityError = "Please enter a valid quantity.";
            isValid = false;
        }
        else if (_editingRecord == null && ModalItem != null)
        {
            var companyData = App.CompanyManager?.CompanyData;
            var item = companyData?.RentalInventory.FirstOrDefault(i => i.Id == ModalItem.Id);
            if (item != null && qty > item.AvailableQuantity)
            {
                ModalQuantityError = $"Only {item.AvailableQuantity} available.";
                isValid = false;
            }
        }

        if (!decimal.TryParse(ModalRateAmount, out var rate) || rate < 0)
        {
            ModalRateError = "Please enter a valid rate.";
            isValid = false;
        }

        return isValid;
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
