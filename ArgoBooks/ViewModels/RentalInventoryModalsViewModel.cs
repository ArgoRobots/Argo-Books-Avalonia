using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for rental inventory modals.
/// </summary>
public partial class RentalInventoryModalsViewModel : ObservableObject
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
    private bool _isRentOutModalOpen;

    #endregion

    #region Modal Form Fields

    [ObservableProperty]
    private string _modalItemName = string.Empty;

    [ObservableProperty]
    private SupplierOption? _modalSupplier;

    [ObservableProperty]
    private string _modalTotalQuantity = string.Empty;

    [ObservableProperty]
    private string _modalDailyRate = string.Empty;

    [ObservableProperty]
    private string _modalWeeklyRate = string.Empty;

    [ObservableProperty]
    private string _modalMonthlyRate = string.Empty;

    [ObservableProperty]
    private string _modalSecurityDeposit = string.Empty;

    [ObservableProperty]
    private string _modalNotes = string.Empty;

    [ObservableProperty]
    private string _modalStatus = "Active";

    [ObservableProperty]
    private string? _modalItemNameError;

    [ObservableProperty]
    private string? _modalQuantityError;

    [ObservableProperty]
    private string? _modalDailyRateError;

    private RentalItem? _editingItem;
    private RentalItemDisplayItem? _deletingItem;

    #endregion

    #region Rent Out Modal Fields

    [ObservableProperty]
    private string _rentOutItemName = string.Empty;

    [ObservableProperty]
    private string _rentOutItemId = string.Empty;

    [ObservableProperty]
    private int _rentOutAvailableQuantity;

    [ObservableProperty]
    private CustomerOption? _rentOutCustomer;

    [ObservableProperty]
    private AccountantOption? _rentOutAccountant;

    [ObservableProperty]
    private string _rentOutQuantity = "1";

    [ObservableProperty]
    private string _rentOutRateType = "Daily";

    [ObservableProperty]
    private decimal _rentOutRateAmount;

    [ObservableProperty]
    private decimal _rentOutDeposit;

    [ObservableProperty]
    private DateTime _rentOutStartDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _rentOutDueDate = DateTime.Today.AddDays(1);

    [ObservableProperty]
    private string _rentOutNotes = string.Empty;

    [ObservableProperty]
    private string? _rentOutCustomerError;

    [ObservableProperty]
    private string? _rentOutQuantityError;

    private RentalItem? _rentingItem;

    public string RentOutEstimatedTotal
    {
        get
        {
            if (!int.TryParse(RentOutQuantity, out var qty) || qty <= 0)
                return "$0.00";

            var days = (RentOutDueDate - RentOutStartDate).Days;
            if (days <= 0) days = 1;

            var total = RentOutRateType switch
            {
                "Daily" => RentOutRateAmount * days * qty,
                "Weekly" => RentOutRateAmount * Math.Ceiling(days / 7.0m) * qty,
                "Monthly" => RentOutRateAmount * Math.Ceiling(days / 30.0m) * qty,
                _ => 0
            };

            return $"${total:N2}";
        }
    }

    partial void OnRentOutQuantityChanged(string value)
    {
        OnPropertyChanged(nameof(RentOutEstimatedTotal));
    }

    partial void OnRentOutRateTypeChanged(string value)
    {
        UpdateRentOutRateAmount();
        OnPropertyChanged(nameof(RentOutEstimatedTotal));
    }

    partial void OnRentOutStartDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(RentOutEstimatedTotal));
    }

    partial void OnRentOutDueDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(RentOutEstimatedTotal));
    }

    private void UpdateRentOutRateAmount()
    {
        if (_rentingItem == null) return;

        RentOutRateAmount = RentOutRateType switch
        {
            "Daily" => _rentingItem.DailyRate,
            "Weekly" => _rentingItem.WeeklyRate,
            "Monthly" => _rentingItem.MonthlyRate,
            _ => _rentingItem.DailyRate
        };
    }

    #endregion

    #region Filter Fields

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string? _filterSupplier;

    [ObservableProperty]
    private string? _filterDailyRateMin;

    [ObservableProperty]
    private string? _filterDailyRateMax;

    [ObservableProperty]
    private string _filterAvailability = "All";

    #endregion

    #region Dropdown Options

    public ObservableCollection<SupplierOption> AvailableSuppliers { get; } = [];
    public ObservableCollection<CustomerOption> AvailableCustomers { get; } = [];
    public ObservableCollection<AccountantOption> AvailableAccountants { get; } = [];
    public ObservableCollection<string> StatusOptions { get; } = ["Active", "In Maintenance"];
    public ObservableCollection<string> FilterStatusOptions { get; } = ["All", "Available", "In Maintenance", "All Rented"];
    public ObservableCollection<string> AvailabilityOptions { get; } = ["All", "Available Only", "Unavailable Only"];
    public ObservableCollection<string> RateTypeOptions { get; } = ["Daily", "Weekly", "Monthly"];

    #endregion

    #region Events

    public event EventHandler? ItemSaved;
    public event EventHandler? ItemDeleted;
    public event EventHandler? FiltersApplied;
    public event EventHandler? FiltersCleared;
    public event EventHandler? RentalCreated;

    #endregion

    #region Add Item

    [RelayCommand]
    public void OpenAddModal()
    {
        _editingItem = null;
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
    public void SaveNewItem()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        companyData.IdCounters.RentalItem++;
        var newId = $"RNT-ITM-{companyData.IdCounters.RentalItem:D3}";

        var totalQty = int.TryParse(ModalTotalQuantity, out var qty) ? qty : 0;

        var newItem = new RentalItem
        {
            Id = newId,
            Name = ModalItemName.Trim(),
            SupplierId = ModalSupplier?.Id,
            TotalQuantity = totalQty,
            AvailableQuantity = totalQty,
            RentedQuantity = 0,
            DailyRate = decimal.TryParse(ModalDailyRate, out var daily) ? daily : 0,
            WeeklyRate = decimal.TryParse(ModalWeeklyRate, out var weekly) ? weekly : 0,
            MonthlyRate = decimal.TryParse(ModalMonthlyRate, out var monthly) ? monthly : 0,
            SecurityDeposit = decimal.TryParse(ModalSecurityDeposit, out var deposit) ? deposit : 0,
            Status = ModalStatus == "In Maintenance" ? EntityStatus.Inactive : EntityStatus.Active,
            Notes = ModalNotes.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        companyData.RentalInventory.Add(newItem);
        companyData.MarkAsModified();

        var itemToUndo = newItem;
        App.UndoRedoManager?.RecordAction(new RentalItemAddAction(
            $"Add rental item '{newItem.Name}'",
            itemToUndo,
            () =>
            {
                companyData.RentalInventory.Remove(itemToUndo);
                companyData.MarkAsModified();
                ItemSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.RentalInventory.Add(itemToUndo);
                companyData.MarkAsModified();
                ItemSaved?.Invoke(this, EventArgs.Empty);
            }));

        ItemSaved?.Invoke(this, EventArgs.Empty);
        CloseAddModal();
    }

    #endregion

    #region Edit Item

    public void OpenEditModal(RentalItemDisplayItem? item)
    {
        if (item == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        var rentalItem = companyData?.RentalInventory.FirstOrDefault(i => i.Id == item.Id);
        if (rentalItem == null)
            return;

        _editingItem = rentalItem;
        UpdateDropdownOptions();

        ModalItemName = rentalItem.Name;
        ModalSupplier = AvailableSuppliers.FirstOrDefault(s => s.Id == rentalItem.SupplierId);
        ModalTotalQuantity = rentalItem.TotalQuantity.ToString();
        ModalDailyRate = rentalItem.DailyRate.ToString("0.00");
        ModalWeeklyRate = rentalItem.WeeklyRate.ToString("0.00");
        ModalMonthlyRate = rentalItem.MonthlyRate.ToString("0.00");
        ModalSecurityDeposit = rentalItem.SecurityDeposit.ToString("0.00");
        ModalStatus = rentalItem.Status == EntityStatus.Inactive ? "In Maintenance" : "Active";
        ModalNotes = rentalItem.Notes;

        ClearModalErrors();
        IsEditModalOpen = true;
    }

    [RelayCommand]
    public void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingItem = null;
        ClearModalFields();
    }

    [RelayCommand]
    public void SaveEditedItem()
    {
        if (!ValidateModal() || _editingItem == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var oldName = _editingItem.Name;
        var oldSupplierId = _editingItem.SupplierId;
        var oldTotalQuantity = _editingItem.TotalQuantity;
        var oldAvailableQuantity = _editingItem.AvailableQuantity;
        var oldDailyRate = _editingItem.DailyRate;
        var oldWeeklyRate = _editingItem.WeeklyRate;
        var oldMonthlyRate = _editingItem.MonthlyRate;
        var oldSecurityDeposit = _editingItem.SecurityDeposit;
        var oldStatus = _editingItem.Status;
        var oldNotes = _editingItem.Notes;

        var newTotalQty = int.TryParse(ModalTotalQuantity, out var qty) ? qty : 0;
        var qtyDiff = newTotalQty - oldTotalQuantity;

        var newName = ModalItemName.Trim();
        var newSupplierId = ModalSupplier?.Id;
        var newDailyRate = decimal.TryParse(ModalDailyRate, out var daily) ? daily : 0;
        var newWeeklyRate = decimal.TryParse(ModalWeeklyRate, out var weekly) ? weekly : 0;
        var newMonthlyRate = decimal.TryParse(ModalMonthlyRate, out var monthly) ? monthly : 0;
        var newSecurityDeposit = decimal.TryParse(ModalSecurityDeposit, out var deposit) ? deposit : 0;
        var newStatus = ModalStatus == "In Maintenance" ? EntityStatus.Inactive : EntityStatus.Active;
        var newNotes = ModalNotes.Trim();
        var newAvailableQuantity = Math.Max(0, oldAvailableQuantity + qtyDiff);

        var itemToEdit = _editingItem;
        itemToEdit.Name = newName;
        itemToEdit.SupplierId = newSupplierId;
        itemToEdit.TotalQuantity = newTotalQty;
        itemToEdit.AvailableQuantity = newAvailableQuantity;
        itemToEdit.DailyRate = newDailyRate;
        itemToEdit.WeeklyRate = newWeeklyRate;
        itemToEdit.MonthlyRate = newMonthlyRate;
        itemToEdit.SecurityDeposit = newSecurityDeposit;
        itemToEdit.Status = newStatus;
        itemToEdit.Notes = newNotes;
        itemToEdit.UpdatedAt = DateTime.UtcNow;

        companyData.MarkAsModified();

        App.UndoRedoManager?.RecordAction(new RentalItemEditAction(
            $"Edit rental item '{newName}'",
            itemToEdit,
            () =>
            {
                itemToEdit.Name = oldName;
                itemToEdit.SupplierId = oldSupplierId;
                itemToEdit.TotalQuantity = oldTotalQuantity;
                itemToEdit.AvailableQuantity = oldAvailableQuantity;
                itemToEdit.DailyRate = oldDailyRate;
                itemToEdit.WeeklyRate = oldWeeklyRate;
                itemToEdit.MonthlyRate = oldMonthlyRate;
                itemToEdit.SecurityDeposit = oldSecurityDeposit;
                itemToEdit.Status = oldStatus;
                itemToEdit.Notes = oldNotes;
                companyData.MarkAsModified();
                ItemSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                itemToEdit.Name = newName;
                itemToEdit.SupplierId = newSupplierId;
                itemToEdit.TotalQuantity = newTotalQty;
                itemToEdit.AvailableQuantity = newAvailableQuantity;
                itemToEdit.DailyRate = newDailyRate;
                itemToEdit.WeeklyRate = newWeeklyRate;
                itemToEdit.MonthlyRate = newMonthlyRate;
                itemToEdit.SecurityDeposit = newSecurityDeposit;
                itemToEdit.Status = newStatus;
                itemToEdit.Notes = newNotes;
                companyData.MarkAsModified();
                ItemSaved?.Invoke(this, EventArgs.Empty);
            }));

        ItemSaved?.Invoke(this, EventArgs.Empty);
        CloseEditModal();
    }

    #endregion

    #region Delete Item

    public void OpenDeleteConfirm(RentalItemDisplayItem? item)
    {
        if (item == null)
            return;

        _deletingItem = item;
        OnPropertyChanged(nameof(DeletingItemName));
        IsDeleteConfirmOpen = true;
    }

    [RelayCommand]
    public void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingItem = null;
    }

    [RelayCommand]
    public void ConfirmDelete()
    {
        if (_deletingItem == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var item = companyData.RentalInventory.FirstOrDefault(i => i.Id == _deletingItem.Id);
        if (item != null)
        {
            var deletedItem = item;
            companyData.RentalInventory.Remove(item);
            companyData.MarkAsModified();

            App.UndoRedoManager?.RecordAction(new RentalItemDeleteAction(
                $"Delete rental item '{deletedItem.Name}'",
                deletedItem,
                () =>
                {
                    companyData.RentalInventory.Add(deletedItem);
                    companyData.MarkAsModified();
                    ItemDeleted?.Invoke(this, EventArgs.Empty);
                },
                () =>
                {
                    companyData.RentalInventory.Remove(deletedItem);
                    companyData.MarkAsModified();
                    ItemDeleted?.Invoke(this, EventArgs.Empty);
                }));
        }

        ItemDeleted?.Invoke(this, EventArgs.Empty);
        CloseDeleteConfirm();
    }

    public string DeletingItemName => _deletingItem?.Name ?? string.Empty;

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
        FilterSupplier = null;
        FilterDailyRateMin = null;
        FilterDailyRateMax = null;
        FilterAvailability = "All";
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    #endregion

    #region Rent Out Modal

    public void OpenRentOutModal(RentalItemDisplayItem? item)
    {
        if (item == null || !item.IsAvailable)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        var rentalItem = companyData?.RentalInventory.FirstOrDefault(i => i.Id == item.Id);
        if (rentalItem == null)
            return;

        _rentingItem = rentalItem;
        UpdateDropdownOptions();

        RentOutItemName = rentalItem.Name;
        RentOutItemId = rentalItem.Id;
        RentOutAvailableQuantity = rentalItem.AvailableQuantity;
        RentOutCustomer = null;
        RentOutAccountant = null;
        RentOutQuantity = "1";
        RentOutRateType = "Daily";
        RentOutRateAmount = rentalItem.DailyRate;
        RentOutDeposit = rentalItem.SecurityDeposit;
        RentOutStartDate = DateTime.Today;
        RentOutDueDate = DateTime.Today.AddDays(1);
        RentOutNotes = string.Empty;

        ClearRentOutErrors();
        OnPropertyChanged(nameof(RentOutEstimatedTotal));
        IsRentOutModalOpen = true;
    }

    [RelayCommand]
    public void CloseRentOutModal()
    {
        IsRentOutModalOpen = false;
        _rentingItem = null;
    }

    [RelayCommand]
    public void ConfirmRentOut()
    {
        if (!ValidateRentOut() || _rentingItem == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var rentQty = int.TryParse(RentOutQuantity, out var qty) ? qty : 1;

        companyData.IdCounters.Rental++;
        var newId = $"RNT-{companyData.IdCounters.Rental:D3}";

        var newRental = new RentalRecord
        {
            Id = newId,
            RentalItemId = _rentingItem.Id,
            CustomerId = RentOutCustomer?.Id ?? string.Empty,
            AccountantId = RentOutAccountant?.Id,
            Quantity = rentQty,
            RateType = RentOutRateType switch
            {
                "Weekly" => RateType.Weekly,
                "Monthly" => RateType.Monthly,
                _ => RateType.Daily
            },
            RateAmount = RentOutRateAmount,
            SecurityDeposit = RentOutDeposit,
            StartDate = RentOutStartDate,
            DueDate = RentOutDueDate,
            Status = RentalStatus.Active,
            Notes = RentOutNotes.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Update inventory quantities
        var oldAvailable = _rentingItem.AvailableQuantity;
        var oldRented = _rentingItem.RentedQuantity;
        _rentingItem.AvailableQuantity -= rentQty;
        _rentingItem.RentedQuantity += rentQty;
        _rentingItem.UpdatedAt = DateTime.UtcNow;

        companyData.Rentals.Add(newRental);
        companyData.MarkAsModified();

        var rentalToUndo = newRental;
        var itemToUpdate = _rentingItem;
        App.UndoRedoManager?.RecordAction(new RentalRecordAddAction(
            $"Rent out '{_rentingItem.Name}' to customer",
            rentalToUndo,
            () =>
            {
                companyData.Rentals.Remove(rentalToUndo);
                itemToUpdate.AvailableQuantity = oldAvailable;
                itemToUpdate.RentedQuantity = oldRented;
                companyData.MarkAsModified();
                RentalCreated?.Invoke(this, EventArgs.Empty);
                ItemSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Rentals.Add(rentalToUndo);
                itemToUpdate.AvailableQuantity -= rentQty;
                itemToUpdate.RentedQuantity += rentQty;
                companyData.MarkAsModified();
                RentalCreated?.Invoke(this, EventArgs.Empty);
                ItemSaved?.Invoke(this, EventArgs.Empty);
            }));

        RentalCreated?.Invoke(this, EventArgs.Empty);
        ItemSaved?.Invoke(this, EventArgs.Empty);
        CloseRentOutModal();
    }

    private bool ValidateRentOut()
    {
        ClearRentOutErrors();
        var isValid = true;

        if (RentOutCustomer == null)
        {
            RentOutCustomerError = "Customer is required.";
            isValid = false;
        }

        if (!int.TryParse(RentOutQuantity, out var qty) || qty <= 0)
        {
            RentOutQuantityError = "Please enter a valid quantity.";
            isValid = false;
        }
        else if (qty > RentOutAvailableQuantity)
        {
            RentOutQuantityError = $"Only {RentOutAvailableQuantity} available.";
            isValid = false;
        }

        return isValid;
    }

    private void ClearRentOutErrors()
    {
        RentOutCustomerError = null;
        RentOutQuantityError = null;
    }

    #endregion

    #region Modal Helpers

    private void UpdateDropdownOptions()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        AvailableSuppliers.Clear();
        foreach (var supplier in companyData.Suppliers.OrderBy(s => s.Name))
        {
            AvailableSuppliers.Add(new SupplierOption { Id = supplier.Id, Name = supplier.Name });
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
        ModalItemName = string.Empty;
        ModalSupplier = null;
        ModalTotalQuantity = string.Empty;
        ModalDailyRate = string.Empty;
        ModalWeeklyRate = string.Empty;
        ModalMonthlyRate = string.Empty;
        ModalSecurityDeposit = string.Empty;
        ModalNotes = string.Empty;
        ModalStatus = "Active";
        ClearModalErrors();
    }

    private void ClearModalErrors()
    {
        ModalItemNameError = null;
        ModalQuantityError = null;
        ModalDailyRateError = null;
    }

    private bool ValidateModal()
    {
        ClearModalErrors();
        var isValid = true;

        if (string.IsNullOrWhiteSpace(ModalItemName))
        {
            ModalItemNameError = "Item name is required.";
            isValid = false;
        }
        else
        {
            var companyData = App.CompanyManager?.CompanyData;
            var existingWithSameName = companyData?.RentalInventory.Any(i =>
                i.Name.Equals(ModalItemName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (_editingItem == null || i.Id != _editingItem.Id)) ?? false;

            if (existingWithSameName)
            {
                ModalItemNameError = "An item with this name already exists.";
                isValid = false;
            }
        }

        if (!int.TryParse(ModalTotalQuantity, out var qty) || qty <= 0)
        {
            ModalQuantityError = "Please enter a valid quantity.";
            isValid = false;
        }

        if (!decimal.TryParse(ModalDailyRate, out var rate) || rate < 0)
        {
            ModalDailyRateError = "Please enter a valid daily rate.";
            isValid = false;
        }

        return isValid;
    }

    #endregion
}

/// <summary>
/// Option model for customer dropdown.
/// </summary>
public class CustomerOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}

/// <summary>
/// Option model for accountant dropdown.
/// </summary>
public class AccountantOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}

/// <summary>
/// Undoable action for adding a rental record.
/// </summary>
public class RentalRecordAddAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public RentalRecordAddAction(string description, RentalRecord _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}
