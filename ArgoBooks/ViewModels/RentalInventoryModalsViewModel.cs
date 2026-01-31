using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Localization;
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

    // Original values for change detection in edit mode
    private string _originalItemName = string.Empty;
    private SupplierOption? _originalSupplier;
    private string _originalTotalQuantity = string.Empty;
    private string _originalDailyRate = string.Empty;
    private string _originalWeeklyRate = string.Empty;
    private string _originalMonthlyRate = string.Empty;
    private string _originalSecurityDeposit = string.Empty;
    private string _originalNotes = string.Empty;
    private string _originalStatus = "Active";

    /// <summary>
    /// Returns true if any data has been entered in the Add modal.
    /// </summary>
    public bool HasAddModalEnteredData =>
        !string.IsNullOrWhiteSpace(ModalItemName) ||
        ModalSupplier != null ||
        !string.IsNullOrWhiteSpace(ModalTotalQuantity) ||
        !string.IsNullOrWhiteSpace(ModalDailyRate) ||
        !string.IsNullOrWhiteSpace(ModalWeeklyRate) ||
        !string.IsNullOrWhiteSpace(ModalMonthlyRate) ||
        !string.IsNullOrWhiteSpace(ModalSecurityDeposit) ||
        !string.IsNullOrWhiteSpace(ModalNotes) ||
        ModalStatus != "Active";

    /// <summary>
    /// Returns true if any changes have been made in the Edit modal.
    /// </summary>
    public bool HasEditModalChanges =>
        ModalItemName != _originalItemName ||
        ModalSupplier?.Id != _originalSupplier?.Id ||
        ModalTotalQuantity != _originalTotalQuantity ||
        ModalDailyRate != _originalDailyRate ||
        ModalWeeklyRate != _originalWeeklyRate ||
        ModalMonthlyRate != _originalMonthlyRate ||
        ModalSecurityDeposit != _originalSecurityDeposit ||
        ModalNotes != _originalNotes ||
        ModalStatus != _originalStatus;

    /// <summary>
    /// Returns true if any filter differs from default values.
    /// </summary>
    public bool HasFilterChanges =>
        FilterStatus != "All" ||
        FilterAvailability != "All" ||
        !string.IsNullOrWhiteSpace(FilterDailyRateMin) ||
        !string.IsNullOrWhiteSpace(FilterDailyRateMax);

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
    private DateTimeOffset? _rentOutStartDate = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset? _rentOutDueDate = DateTimeOffset.Now.AddDays(1);

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

            if (RentOutStartDate == null || RentOutDueDate == null)
                return "$0.00";

            var days = (RentOutDueDate.Value - RentOutStartDate.Value).Days;
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
        // Clear error when user modifies the field
        if (int.TryParse(value, out var qty) && qty > 0)
        {
            RentOutQuantityError = null;
        }
    }

    partial void OnRentOutCustomerChanged(CustomerOption? value)
    {
        if (value != null)
        {
            RentOutCustomerError = null;
        }
    }

    partial void OnModalItemNameChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            ModalItemNameError = null;
        }
    }

    partial void OnModalTotalQuantityChanged(string value)
    {
        if (int.TryParse(value, out var qty) && qty > 0)
        {
            ModalQuantityError = null;
        }
    }

    partial void OnModalDailyRateChanged(string value)
    {
        // Clear error when user modifies any rate field
        ModalDailyRateError = null;
    }

    partial void OnRentOutRateTypeChanged(string value)
    {
        UpdateRentOutRateAmount();
        OnPropertyChanged(nameof(RentOutEstimatedTotal));
    }

    partial void OnRentOutStartDateChanged(DateTimeOffset? value)
    {
        OnPropertyChanged(nameof(RentOutEstimatedTotal));
    }

    partial void OnRentOutDueDateChanged(DateTimeOffset? value)
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
    /// Navigates to Suppliers page and opens the create supplier modal.
    /// </summary>
    [RelayCommand]
    private void NavigateToCreateSupplier()
    {
        IsAddModalOpen = false;
        IsEditModalOpen = false;
        App.NavigationService?.NavigateTo("Suppliers", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    /// <summary>
    /// Navigates to Customers page and opens the create customer modal.
    /// </summary>
    [RelayCommand]
    private void NavigateToCreateCustomer()
    {
        IsAddModalOpen = false;
        IsEditModalOpen = false;
        IsRentOutModalOpen = false;
        App.NavigationService?.NavigateTo("Customers", new Dictionary<string, object?> { { "openAddModal", true } });
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
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Add rental item '{newItem.Name}'",
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

        // Store original values for change detection
        _originalItemName = ModalItemName;
        _originalSupplier = ModalSupplier;
        _originalTotalQuantity = ModalTotalQuantity;
        _originalDailyRate = ModalDailyRate;
        _originalWeeklyRate = ModalWeeklyRate;
        _originalMonthlyRate = ModalMonthlyRate;
        _originalSecurityDeposit = ModalSecurityDeposit;
        _originalNotes = ModalNotes;
        _originalStatus = ModalStatus;

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

        // Check if anything actually changed
        var hasChanges = oldName != newName ||
                         oldSupplierId != newSupplierId ||
                         oldTotalQuantity != newTotalQty ||
                         oldDailyRate != newDailyRate ||
                         oldWeeklyRate != newWeeklyRate ||
                         oldMonthlyRate != newMonthlyRate ||
                         oldSecurityDeposit != newSecurityDeposit ||
                         oldStatus != newStatus ||
                         oldNotes != newNotes;

        // If nothing changed, just close the modal without recording an action
        if (!hasChanges)
        {
            CloseEditModal();
            return;
        }

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

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Edit rental item '{newName}'",
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

    public async void OpenDeleteConfirm(RentalItemDisplayItem? item)
    {
        if (item == null)
            return;

        var dialog = App.ConfirmationDialog;
        if (dialog == null)
            return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Delete Rental Item".Translate(),
            Message = "Are you sure you want to delete this rental item?\n\n{0}".TranslateFormat(item.Name),
            PrimaryButtonText = "Delete".Translate(),
            CancelButtonText = "Cancel".Translate(),
            IsPrimaryDestructive = true
        });

        if (result != ConfirmationResult.Primary)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var rentalItem = companyData.RentalInventory.FirstOrDefault(i => i.Id == item.Id);
        if (rentalItem != null)
        {
            var deletedItem = rentalItem;
            companyData.RentalInventory.Remove(rentalItem);
            companyData.MarkAsModified();

            App.UndoRedoManager.RecordAction(new DelegateAction(
                $"Delete rental item '{deletedItem.Name}'",
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

    /// <summary>
    /// Requests to close the Filter modal, showing confirmation if filters have been changed.
    /// </summary>
    [RelayCommand]
    public async Task RequestCloseFilterModalAsync()
    {
        if (HasFilterChanges)
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

            ResetFilterDefaults();
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
        FilterSupplier = null;
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
        RentOutStartDate = DateTimeOffset.Now;
        RentOutDueDate = DateTimeOffset.Now.AddDays(1);
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
            StartDate = RentOutStartDate?.DateTime ?? DateTime.Today,
            DueDate = RentOutDueDate?.DateTime ?? DateTime.Today.AddDays(1),
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
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Rent out '{_rentingItem.Name}' to customer",
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
            RentOutCustomerError = "Customer is required.".Translate();
            isValid = false;
        }

        if (!int.TryParse(RentOutQuantity, out var qty) || qty <= 0)
        {
            RentOutQuantityError = "Please enter a valid quantity.".Translate();
            isValid = false;
        }
        else if (qty > RentOutAvailableQuantity)
        {
            RentOutQuantityError = "Only {0} available.".TranslateFormat(RentOutAvailableQuantity);
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

    private void ResetFilterDefaults()
    {
        FilterStatus = "All";
        FilterAvailability = "All";
        FilterDailyRateMin = null;
        FilterDailyRateMax = null;
    }

    private bool ValidateModal()
    {
        ClearModalErrors();
        var isValid = true;

        if (string.IsNullOrWhiteSpace(ModalItemName))
        {
            ModalItemNameError = "Item name is required.".Translate();
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
                ModalItemNameError = "An item with this name already exists.".Translate();
                isValid = false;
            }
        }

        if (!int.TryParse(ModalTotalQuantity, out var qty) || qty <= 0)
        {
            ModalQuantityError = "Please enter a valid quantity.".Translate();
            isValid = false;
        }

        // Require at least one valid rate (daily, weekly, or monthly)
        var hasDaily = decimal.TryParse(ModalDailyRate, out var daily) && daily > 0;
        var hasWeekly = decimal.TryParse(ModalWeeklyRate, out var weekly) && weekly > 0;
        var hasMonthly = decimal.TryParse(ModalMonthlyRate, out var monthly) && monthly > 0;

        if (!hasDaily && !hasWeekly && !hasMonthly)
        {
            ModalDailyRateError = "Please enter at least one rental rate.".Translate();
            isValid = false;
        }

        return isValid;
    }

    #endregion
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
