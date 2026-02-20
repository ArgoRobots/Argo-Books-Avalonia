using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for supplier modals, shared between SuppliersPage and AppShell.
/// </summary>
public partial class SupplierModalsViewModel : ObservableObject
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
    private bool _hasValidationMessage;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    #endregion

    #region Modal Form Fields

    [ObservableProperty]
    private string _modalId = string.Empty;

    [ObservableProperty]
    private string _modalSupplierName = string.Empty;

    [ObservableProperty]
    private string _modalEmail = string.Empty;

    [ObservableProperty]
    private string _modalPhone = string.Empty;

    [ObservableProperty]
    private string _modalWebsite = string.Empty;

    [ObservableProperty]
    private string _modalStreetAddress = string.Empty;

    [ObservableProperty]
    private string _modalCity = string.Empty;

    [ObservableProperty]
    private string _modalStateProvince = string.Empty;

    [ObservableProperty]
    private string _modalZipCode = string.Empty;

    [ObservableProperty]
    private string _modalCountry = string.Empty;

    [ObservableProperty]
    private string _modalNotes = string.Empty;

    [ObservableProperty]
    private string? _modalError;

    [ObservableProperty]
    private string? _modalSupplierNameError;

    [ObservableProperty]
    private string? _modalEmailError;

    [ObservableProperty]
    private string? _modalPhoneError;

    private Supplier? _editingSupplier;

    // Original values for change detection in edit mode
    private string _originalSupplierName = string.Empty;
    private string _originalEmail = string.Empty;
    private string _originalPhone = string.Empty;
    private string _originalWebsite = string.Empty;
    private string _originalStreetAddress = string.Empty;
    private string _originalCity = string.Empty;
    private string _originalStateProvince = string.Empty;
    private string _originalZipCode = string.Empty;
    private string _originalCountry = string.Empty;
    private string _originalNotes = string.Empty;

    /// <summary>
    /// Returns true if any data has been entered in the Add modal.
    /// </summary>
    public bool HasAddModalEnteredData =>
        !string.IsNullOrWhiteSpace(ModalSupplierName) ||
        !string.IsNullOrWhiteSpace(ModalEmail) ||
        !string.IsNullOrWhiteSpace(ModalPhone) ||
        !string.IsNullOrWhiteSpace(ModalWebsite) ||
        !string.IsNullOrWhiteSpace(ModalStreetAddress) ||
        !string.IsNullOrWhiteSpace(ModalCity) ||
        !string.IsNullOrWhiteSpace(ModalStateProvince) ||
        !string.IsNullOrWhiteSpace(ModalZipCode) ||
        !string.IsNullOrWhiteSpace(ModalCountry) ||
        !string.IsNullOrWhiteSpace(ModalNotes);

    /// <summary>
    /// Returns true if any changes have been made in the Edit modal.
    /// </summary>
    public bool HasEditModalChanges =>
        ModalSupplierName != _originalSupplierName ||
        ModalEmail != _originalEmail ||
        ModalPhone != _originalPhone ||
        ModalWebsite != _originalWebsite ||
        ModalStreetAddress != _originalStreetAddress ||
        ModalCity != _originalCity ||
        ModalStateProvince != _originalStateProvince ||
        ModalZipCode != _originalZipCode ||
        ModalCountry != _originalCountry ||
        ModalNotes != _originalNotes;

    #endregion

    #region Filter Fields

    [ObservableProperty]
    private string _filterCountry = "All";

    [ObservableProperty]
    private string _filterStatus = "All";

    public ObservableCollection<string> CountryOptions { get; } = ["All"];
    public ObservableCollection<string> StatusOptions { get; } = ["All", "Active", "Inactive"];

    // Original filter values for change detection (captured when modal opens)
    private string _originalFilterCountry = "All";
    private string _originalFilterStatus = "All";

    /// <summary>
    /// Returns true if any filter has been changed from the state when the modal was opened.
    /// </summary>
    public bool HasFilterModalChanges =>
        FilterCountry != _originalFilterCountry ||
        FilterStatus != _originalFilterStatus;

    /// <summary>
    /// Captures the current filter state as original values for change detection.
    /// </summary>
    private void CaptureOriginalFilterValues()
    {
        _originalFilterCountry = FilterCountry;
        _originalFilterStatus = FilterStatus;
    }

    #endregion

    #region Events

    public event EventHandler? SupplierSaved;
    public event EventHandler? SupplierDeleted;
    public event EventHandler? FiltersApplied;
    public event EventHandler? FiltersCleared;

    #endregion

    #region Add Supplier

    [RelayCommand]
    public void OpenAddModal()
    {
        _editingSupplier = null;
        ClearModalFields();
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

    [RelayCommand]
    public void SaveNewSupplier()
    {
        if (!ValidateModal()) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        string newId;
        if (!string.IsNullOrWhiteSpace(ModalId))
        {
            newId = ModalId.Trim();
        }
        else
        {
            companyData.IdCounters.Supplier++;
            newId = $"SUP-{companyData.IdCounters.Supplier:D3}";
        }

        var newSupplier = new Supplier
        {
            Id = newId,
            Name = ModalSupplierName.Trim(),
            Email = string.IsNullOrWhiteSpace(ModalEmail) ? string.Empty : ModalEmail.Trim(),
            Phone = string.IsNullOrWhiteSpace(ModalPhone) ? string.Empty : ModalPhone.Trim(),
            Website = string.IsNullOrWhiteSpace(ModalWebsite) ? string.Empty : ModalWebsite.Trim(),
            Address = new Address
            {
                Street = string.IsNullOrWhiteSpace(ModalStreetAddress) ? string.Empty : ModalStreetAddress.Trim(),
                City = string.IsNullOrWhiteSpace(ModalCity) ? string.Empty : ModalCity.Trim(),
                State = string.IsNullOrWhiteSpace(ModalStateProvince) ? string.Empty : ModalStateProvince.Trim(),
                ZipCode = string.IsNullOrWhiteSpace(ModalZipCode) ? string.Empty : ModalZipCode.Trim(),
                Country = string.IsNullOrWhiteSpace(ModalCountry) ? string.Empty : ModalCountry.Trim()
            },
            Notes = string.IsNullOrWhiteSpace(ModalNotes) ? string.Empty : ModalNotes.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        companyData.Suppliers.Add(newSupplier);
        companyData.MarkAsModified();

        var supplierToUndo = newSupplier;
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Add supplier '{newSupplier.Name}'",
            () => { companyData.Suppliers.Remove(supplierToUndo); companyData.MarkAsModified(); SupplierSaved?.Invoke(this, EventArgs.Empty); },
            () => { companyData.Suppliers.Add(supplierToUndo); companyData.MarkAsModified(); SupplierSaved?.Invoke(this, EventArgs.Empty); }));

        SupplierSaved?.Invoke(this, EventArgs.Empty);
        CloseAddModal();
    }

    #endregion

    #region Edit Supplier

    public void OpenEditModal(SupplierDisplayItem? item)
    {
        if (item == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        var supplier = companyData?.Suppliers.FirstOrDefault(s => s.Id == item.Id);
        if (supplier == null) return;

        _editingSupplier = supplier;
        ModalId = supplier.Id;
        ModalSupplierName = supplier.Name;
        ModalEmail = supplier.Email;
        ModalPhone = supplier.Phone;
        ModalWebsite = supplier.Website;
        ModalStreetAddress = supplier.Address.Street;
        ModalCity = supplier.Address.City;
        ModalStateProvince = supplier.Address.State;
        ModalZipCode = supplier.Address.ZipCode;
        ModalCountry = supplier.Address.Country;
        ModalNotes = supplier.Notes;

        // Store original values for change detection
        _originalSupplierName = ModalSupplierName;
        _originalEmail = ModalEmail;
        _originalPhone = ModalPhone;
        _originalWebsite = ModalWebsite;
        _originalStreetAddress = ModalStreetAddress;
        _originalCity = ModalCity;
        _originalStateProvince = ModalStateProvince;
        _originalZipCode = ModalZipCode;
        _originalCountry = ModalCountry;
        _originalNotes = ModalNotes;

        ModalError = null;
        IsEditModalOpen = true;
    }

    [RelayCommand]
    public void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingSupplier = null;
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
    public void SaveEditedSupplier()
    {
        if (!ValidateModal() || _editingSupplier == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var oldName = _editingSupplier.Name;
        var oldEmail = _editingSupplier.Email;
        var oldPhone = _editingSupplier.Phone;
        var oldWebsite = _editingSupplier.Website;
        var oldAddress = new Address
        {
            Street = _editingSupplier.Address.Street,
            City = _editingSupplier.Address.City,
            State = _editingSupplier.Address.State,
            ZipCode = _editingSupplier.Address.ZipCode,
            Country = _editingSupplier.Address.Country
        };
        var oldNotes = _editingSupplier.Notes;

        var newName = ModalSupplierName.Trim();
        var newEmail = string.IsNullOrWhiteSpace(ModalEmail) ? string.Empty : ModalEmail.Trim();
        var newPhone = string.IsNullOrWhiteSpace(ModalPhone) ? string.Empty : ModalPhone.Trim();
        var newWebsite = string.IsNullOrWhiteSpace(ModalWebsite) ? string.Empty : ModalWebsite.Trim();
        var newAddress = new Address
        {
            Street = string.IsNullOrWhiteSpace(ModalStreetAddress) ? string.Empty : ModalStreetAddress.Trim(),
            City = string.IsNullOrWhiteSpace(ModalCity) ? string.Empty : ModalCity.Trim(),
            State = string.IsNullOrWhiteSpace(ModalStateProvince) ? string.Empty : ModalStateProvince.Trim(),
            ZipCode = string.IsNullOrWhiteSpace(ModalZipCode) ? string.Empty : ModalZipCode.Trim(),
            Country = string.IsNullOrWhiteSpace(ModalCountry) ? string.Empty : ModalCountry.Trim()
        };
        var newNotes = string.IsNullOrWhiteSpace(ModalNotes) ? string.Empty : ModalNotes.Trim();

        // Check if anything actually changed
        var hasChanges = oldName != newName ||
                         oldEmail != newEmail ||
                         oldPhone != newPhone ||
                         oldWebsite != newWebsite ||
                         oldAddress.Street != newAddress.Street ||
                         oldAddress.City != newAddress.City ||
                         oldAddress.State != newAddress.State ||
                         oldAddress.ZipCode != newAddress.ZipCode ||
                         oldAddress.Country != newAddress.Country ||
                         oldNotes != newNotes;

        // If nothing changed, just close the modal without recording an action
        if (!hasChanges)
        {
            CloseEditModal();
            return;
        }

        var supplierToEdit = _editingSupplier;
        App.EventLogService?.CapturePreModificationSnapshot("Supplier", supplierToEdit.Id);
        var changes = new Dictionary<string, FieldChange>();
        if (oldName != newName) changes["Name"] = new FieldChange { OldValue = oldName, NewValue = newName };
        if (oldEmail != newEmail) changes["Email"] = new FieldChange { OldValue = oldEmail, NewValue = newEmail };
        if (oldPhone != newPhone) changes["Phone"] = new FieldChange { OldValue = oldPhone, NewValue = newPhone };
        if (oldWebsite != newWebsite) changes["Website"] = new FieldChange { OldValue = oldWebsite, NewValue = newWebsite };
        var oldAddr = $"{oldAddress.Street}, {oldAddress.City}, {oldAddress.State} {oldAddress.ZipCode}".Trim(' ', ',');
        var newAddr = $"{newAddress.Street}, {newAddress.City}, {newAddress.State} {newAddress.ZipCode}".Trim(' ', ',');
        if (oldAddr != newAddr) changes["Address"] = new FieldChange { OldValue = oldAddr, NewValue = newAddr };
        if (oldNotes != newNotes) changes["Notes"] = new FieldChange { OldValue = oldNotes, NewValue = newNotes };
        if (changes.Count > 0) App.EventLogService?.SetPendingChanges(changes);
        supplierToEdit.Name = newName;
        supplierToEdit.Email = newEmail;
        supplierToEdit.Phone = newPhone;
        supplierToEdit.Website = newWebsite;
        supplierToEdit.Address = newAddress;
        supplierToEdit.Notes = newNotes;
        supplierToEdit.UpdatedAt = DateTime.UtcNow;
        companyData.MarkAsModified();

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Edit supplier '{newName}'",
            () => { supplierToEdit.Name = oldName; supplierToEdit.Email = oldEmail; supplierToEdit.Phone = oldPhone; supplierToEdit.Website = oldWebsite; supplierToEdit.Address = oldAddress; supplierToEdit.Notes = oldNotes; companyData.MarkAsModified(); SupplierSaved?.Invoke(this, EventArgs.Empty); },
            () => { supplierToEdit.Name = newName; supplierToEdit.Email = newEmail; supplierToEdit.Phone = newPhone; supplierToEdit.Website = newWebsite; supplierToEdit.Address = newAddress; supplierToEdit.Notes = newNotes; companyData.MarkAsModified(); SupplierSaved?.Invoke(this, EventArgs.Empty); }));

        SupplierSaved?.Invoke(this, EventArgs.Empty);
        CloseEditModal();
    }

    #endregion

    #region Delete Supplier

    public async void OpenDeleteConfirm(SupplierDisplayItem? item)
    {
        if (item == null) return;

        var dialog = App.ConfirmationDialog;
        if (dialog == null) return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Delete Supplier".Translate(),
            Message = "Are you sure you want to delete this supplier?\n\n{0}".TranslateFormat(item.Name),
            PrimaryButtonText = "Delete".Translate(),
            CancelButtonText = "Cancel".Translate(),
            IsPrimaryDestructive = true
        });

        if (result != ConfirmationResult.Primary) return;

        var companyData = App.CompanyManager?.CompanyData;

        var supplier = companyData?.Suppliers.FirstOrDefault(s => s.Id == item.Id);
        if (supplier == null) return;

        var deletedSupplier = supplier;
        App.EventLogService?.CapturePreDeletionSnapshot("Supplier", deletedSupplier.Id);
        companyData?.Suppliers.Remove(supplier);
        companyData?.MarkAsModified();

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Delete supplier '{supplier.Name}'",
            () => { companyData?.Suppliers.Add(deletedSupplier); companyData?.MarkAsModified(); SupplierDeleted?.Invoke(this, EventArgs.Empty); },
            () => { companyData?.Suppliers.Remove(deletedSupplier); companyData?.MarkAsModified(); SupplierDeleted?.Invoke(this, EventArgs.Empty); }));

        SupplierDeleted?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Filter Modal

    [RelayCommand]
    public void OpenFilterModal()
    {
        UpdateCountryOptions();
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

                // Restore filter values to the state when modal was opened
                FilterCountry = _originalFilterCountry;
                FilterStatus = _originalFilterStatus;
            }
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

    private void UpdateCountryOptions()
    {
        CountryOptions.Clear();
        CountryOptions.Add("All");

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var countries = companyData.Suppliers
            .Select(s => s.Address.Country)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c);

        foreach (var country in countries)
            CountryOptions.Add(country);
    }

    #endregion

    #region Property Changed Handlers

    partial void OnModalSupplierNameChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            ModalSupplierNameError = null;
        }
    }

    partial void OnModalEmailChanged(string value)
    {
        // Clear error when user modifies the field
        ModalEmailError = null;
    }

    partial void OnModalPhoneChanged(string value)
    {
        // Clear error when user modifies the field
        ModalPhoneError = null;
    }

    #endregion

    #region Helpers

    private void ResetFilterDefaults()
    {
        FilterCountry = "All";
        FilterStatus = "All";
    }

    private void ClearModalFields()
    {
        ModalId = string.Empty;
        ModalSupplierName = string.Empty;
        ModalEmail = string.Empty;
        ModalPhone = string.Empty;
        ModalWebsite = string.Empty;
        ModalStreetAddress = string.Empty;
        ModalCity = string.Empty;
        ModalStateProvince = string.Empty;
        ModalZipCode = string.Empty;
        ModalCountry = string.Empty;
        ModalNotes = string.Empty;
        ModalError = null;
        ModalSupplierNameError = null;
        ModalEmailError = null;
        ModalPhoneError = null;
        HasValidationMessage = false;
    }

    private bool ValidateModal()
    {
        ModalError = null;
        ModalSupplierNameError = null;
        ModalEmailError = null;
        ModalPhoneError = null;
        var isValid = true;

        if (string.IsNullOrWhiteSpace(ModalSupplierName))
        {
            ModalSupplierNameError = "Supplier name is required.".Translate();
            isValid = false;
        }
        else
        {
            var companyData = App.CompanyManager?.CompanyData;
            var existingWithSameName = companyData?.Suppliers.Any(s =>
                s.Name.Equals(ModalSupplierName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (_editingSupplier == null || s.Id != _editingSupplier.Id)) ?? false;

            if (existingWithSameName)
            {
                ModalSupplierNameError = "A supplier with this name already exists.".Translate();
                isValid = false;
            }
        }

        if (!string.IsNullOrWhiteSpace(ModalEmail) && !ModalEmail.Contains('@'))
        {
            ModalEmailError = "Please enter a valid email address.".Translate();
            isValid = false;
        }

        if (!IsPhoneComplete(ModalPhone))
        {
            ModalPhoneError = "Please enter a complete phone number.".Translate();
            isValid = false;
        }

        HasValidationMessage = !isValid;
        return isValid;
    }

    /// <summary>
    /// Checks if a phone number is complete based on its country's expected format.
    /// Returns true if empty (optional field) or has the correct number of digits.
    /// </summary>
    private static bool IsPhoneComplete(string fullPhone)
    {
        if (string.IsNullOrWhiteSpace(fullPhone))
            return true; // Phone is optional

        // Extract digits from the phone number (excluding dial code)
        var parts = fullPhone.Split(' ', 2);
        if (parts.Length < 2)
            return true; // No number entered yet

        var dialCode = parts[0];
        var numberPart = parts[1];
        var digits = new string(numberPart.Where(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(digits))
            return true; // No digits entered

        // Find the country by dial code
        var country = PhoneInput.AllDialCodes
            .OrderByDescending(c => c.DialCode.Length)
            .FirstOrDefault(c => dialCode.Equals(c.DialCode, StringComparison.OrdinalIgnoreCase));

        if (country == null)
            return true; // Unknown country, allow it

        var expectedDigits = country.PhoneFormat.Count(c => c == 'X');
        return digits.Length == expectedDigits;
    }

    #endregion
}
