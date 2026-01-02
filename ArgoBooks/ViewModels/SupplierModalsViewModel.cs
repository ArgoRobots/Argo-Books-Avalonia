using ArgoBooks.Services;
using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
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

    #endregion

    #region Modal Form Fields

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

    private Supplier? _editingSupplier;
    private SupplierDisplayItem? _deletingSupplier;

    #endregion

    #region Filter Fields

    [ObservableProperty]
    private string _filterCountry = "All";

    [ObservableProperty]
    private string _filterStatus = "All";

    public ObservableCollection<string> CountryOptions { get; } = ["All"];
    public ObservableCollection<string> StatusOptions { get; } = ["All", "Active", "Inactive"];

    #endregion

    #region Delete Properties

    public string DeletingSupplierName => _deletingSupplier?.Name ?? string.Empty;

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

    [RelayCommand]
    public void SaveNewSupplier()
    {
        if (!ValidateModal()) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        companyData.IdCounters.Supplier++;
        var newId = $"SUP-{companyData.IdCounters.Supplier:D3}";

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
        App.UndoRedoManager?.RecordAction(new DelegateAction(
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
        ModalSupplierName = supplier.Name;
        ModalEmail = supplier.Email;
        ModalPhone = supplier.Phone;
        ModalWebsite = supplier.Website ?? string.Empty;
        ModalStreetAddress = supplier.Address.Street;
        ModalCity = supplier.Address.City;
        ModalStateProvince = supplier.Address.State;
        ModalZipCode = supplier.Address.ZipCode;
        ModalCountry = supplier.Address.Country;
        ModalNotes = supplier.Notes;
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
        supplierToEdit.Name = newName;
        supplierToEdit.Email = newEmail;
        supplierToEdit.Phone = newPhone;
        supplierToEdit.Website = newWebsite;
        supplierToEdit.Address = newAddress;
        supplierToEdit.Notes = newNotes;
        supplierToEdit.UpdatedAt = DateTime.UtcNow;
        companyData.MarkAsModified();

        App.UndoRedoManager?.RecordAction(new DelegateAction(
            $"Edit supplier '{newName}'",
            () => { supplierToEdit.Name = oldName; supplierToEdit.Email = oldEmail; supplierToEdit.Phone = oldPhone; supplierToEdit.Website = oldWebsite; supplierToEdit.Address = oldAddress; supplierToEdit.Notes = oldNotes; companyData.MarkAsModified(); SupplierSaved?.Invoke(this, EventArgs.Empty); },
            () => { supplierToEdit.Name = newName; supplierToEdit.Email = newEmail; supplierToEdit.Phone = newPhone; supplierToEdit.Website = newWebsite; supplierToEdit.Address = newAddress; supplierToEdit.Notes = newNotes; companyData.MarkAsModified(); SupplierSaved?.Invoke(this, EventArgs.Empty); }));

        SupplierSaved?.Invoke(this, EventArgs.Empty);
        CloseEditModal();
    }

    #endregion

    #region Delete Supplier

    public void OpenDeleteConfirm(SupplierDisplayItem? item)
    {
        if (item == null) return;
        _deletingSupplier = item;
        OnPropertyChanged(nameof(DeletingSupplierName));
        IsDeleteConfirmOpen = true;
    }

    [RelayCommand]
    public void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingSupplier = null;
    }

    [RelayCommand]
    public void ConfirmDelete()
    {
        if (_deletingSupplier == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var supplier = companyData.Suppliers.FirstOrDefault(s => s.Id == _deletingSupplier.Id);
        if (supplier == null) return;

        var deletedSupplier = supplier;
        companyData.Suppliers.Remove(supplier);
        companyData.MarkAsModified();

        App.UndoRedoManager?.RecordAction(new DelegateAction(
            $"Delete supplier '{deletedSupplier.Name}'",
            () => { companyData.Suppliers.Add(deletedSupplier); companyData.MarkAsModified(); SupplierDeleted?.Invoke(this, EventArgs.Empty); },
            () => { companyData.Suppliers.Remove(deletedSupplier); companyData.MarkAsModified(); SupplierDeleted?.Invoke(this, EventArgs.Empty); }));

        SupplierDeleted?.Invoke(this, EventArgs.Empty);
        CloseDeleteConfirm();
    }

    #endregion

    #region Filter Modal

    [RelayCommand]
    public void OpenFilterModal()
    {
        UpdateCountryOptions();
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
        FilterCountry = "All";
        FilterStatus = "All";
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

    #region Helpers

    private void ClearModalFields()
    {
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
    }

    private bool ValidateModal()
    {
        ModalError = null;
        ModalSupplierNameError = null;
        ModalEmailError = null;
        var isValid = true;

        if (string.IsNullOrWhiteSpace(ModalSupplierName))
        {
            ModalSupplierNameError = "Supplier name is required.";
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
                ModalSupplierNameError = "A supplier with this name already exists.";
                isValid = false;
            }
        }

        if (!string.IsNullOrWhiteSpace(ModalEmail) && !ModalEmail.Contains('@'))
        {
            ModalEmailError = "Please enter a valid email address.";
            isValid = false;
        }

        return isValid;
    }

    #endregion
}
