using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for Locations modals (Add, Edit, Delete, Filter).
/// </summary>
public partial class LocationsModalsViewModel : ViewModelBase
{
    #region Events

    /// <summary>
    /// Raised when a location is saved (added or edited).
    /// </summary>
    public event EventHandler? LocationSaved;

    /// <summary>
    /// Raised when a location is deleted.
    /// </summary>
    public event EventHandler? LocationDeleted;

    /// <summary>
    /// Raised when filters are applied.
    /// </summary>
    public event EventHandler<LocationsFilterAppliedEventArgs>? FiltersApplied;

    #endregion

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
    private string _modalName = string.Empty;

    [ObservableProperty]
    private string _modalCode = string.Empty;

    [ObservableProperty]
    private string _modalType = "Warehouse";

    [ObservableProperty]
    private string _modalStreetAddress = string.Empty;

    [ObservableProperty]
    private string _modalCity = string.Empty;

    [ObservableProperty]
    private string _modalStateProvince = string.Empty;

    [ObservableProperty]
    private string _modalPostalCode = string.Empty;

    [ObservableProperty]
    private string _modalCountry = string.Empty;


    [ObservableProperty]
    private string _modalNotes = string.Empty;

    [ObservableProperty]
    private string? _modalNameError;

    [ObservableProperty]
    private string? _modalError;

    /// <summary>
    /// The location being edited (null for add).
    /// </summary>
    private Location? _editingLocation;

    /// <summary>
    /// The location being deleted.
    /// </summary>
    private LocationDisplayItem? _deletingLocation;

    #endregion

    #region Dropdown Options

    /// <summary>
    /// Location type options.
    /// </summary>
    public ObservableCollection<string> TypeOptions { get; } = ["Warehouse", "Storage Facility", "Factory", "Retail Store", "Distribution Center"];


    #endregion

    #region Filter State

    [ObservableProperty]
    private string _filterType = "All";

    [ObservableProperty]
    private string _filterStatus = "All";

    /// <summary>
    /// Filter type options.
    /// </summary>
    public ObservableCollection<string> FilterTypeOptions { get; } = ["All", "Warehouse", "Storage Facility", "Factory", "Retail Store", "Distribution Center"];

    /// <summary>
    /// Filter status options.
    /// </summary>
    public ObservableCollection<string> FilterStatusOptions { get; } = ["All", "Active", "Inactive"];

    #endregion

    #region Add Location

    /// <summary>
    /// Opens the Add Location modal.
    /// </summary>
    public void OpenAddModal()
    {
        ClearModalFields();
        IsAddModalOpen = true;
    }

    /// <summary>
    /// Closes the Add modal.
    /// </summary>
    [RelayCommand]
    private void CloseAddModal()
    {
        IsAddModalOpen = false;
        ClearModalFields();
    }

    /// <summary>
    /// Saves a new location.
    /// </summary>
    [RelayCommand]
    private void SaveNewLocation()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Generate new ID
        companyData.IdCounters.Location++;
        var newId = string.IsNullOrWhiteSpace(ModalCode)
            ? $"LOC-{companyData.IdCounters.Location:D3}"
            : ModalCode.Trim().ToUpperInvariant();

        // Check for duplicate ID
        if (companyData.Locations?.Any(l => l.Id == newId) == true)
        {
            ModalError = "A location with this code already exists.";
            return;
        }

        var newLocation = new Location
        {
            Id = newId,
            Name = ModalName.Trim(),
            Address = new Core.Models.Common.Address
            {
                Street = ModalStreetAddress.Trim(),
                City = ModalCity.Trim(),
                State = ModalStateProvince.Trim(),
                ZipCode = ModalPostalCode.Trim(),
                Country = ModalCountry.Trim()
            },
            ContactPerson = string.Empty,
            Phone = string.Empty,
            Capacity = 0,
            CurrentUtilization = 0,
            CreatedAt = DateTime.UtcNow
        };

        if (companyData.Locations == null) return;
        companyData.Locations.Add(newLocation);
        companyData.MarkAsModified();

        // Record undo action
        var locationToUndo = newLocation;
        App.UndoRedoManager?.RecordAction(new DelegateAction(
            $"Add location '{newLocation.Name}'",
            () =>
            {
                companyData.Locations?.Remove(locationToUndo);
                companyData.MarkAsModified();
                LocationSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Locations?.Add(locationToUndo);
                companyData.MarkAsModified();
                LocationSaved?.Invoke(this, EventArgs.Empty);
            }));

        LocationSaved?.Invoke(this, EventArgs.Empty);
        CloseAddModal();
    }

    #endregion

    #region Edit Location

    /// <summary>
    /// Opens the Edit Location modal.
    /// </summary>
    public void OpenEditModal(LocationDisplayItem? item)
    {
        if (item == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        var location = companyData?.Locations?.FirstOrDefault(l => l.Id == item.Id);
        if (location == null) return;

        _editingLocation = location;
        ModalName = location.Name;
        ModalCode = location.Id;
        ModalType = item.Type;
        ModalStreetAddress = location.Address.Street;
        ModalCity = location.Address.City;
        ModalStateProvince = location.Address.State;
        ModalPostalCode = location.Address.ZipCode;
        ModalCountry = location.Address.Country;
        ModalNotes = string.Empty;
        ModalNameError = null;
        ModalError = null;

        IsEditModalOpen = true;
    }

    /// <summary>
    /// Closes the Edit modal.
    /// </summary>
    [RelayCommand]
    private void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingLocation = null;
        ClearModalFields();
    }

    /// <summary>
    /// Saves changes to an existing location.
    /// </summary>
    [RelayCommand]
    private void SaveEditedLocation()
    {
        if (!ValidateModal() || _editingLocation == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Store old values for undo
        var oldName = _editingLocation.Name;
        var oldAddress = new Core.Models.Common.Address
        {
            Street = _editingLocation.Address.Street,
            City = _editingLocation.Address.City,
            State = _editingLocation.Address.State,
            ZipCode = _editingLocation.Address.ZipCode,
            Country = _editingLocation.Address.Country
        };

        // Store new values
        var newName = ModalName.Trim();
        var newAddress = new Core.Models.Common.Address
        {
            Street = ModalStreetAddress.Trim(),
            City = ModalCity.Trim(),
            State = ModalStateProvince.Trim(),
            ZipCode = ModalPostalCode.Trim(),
            Country = ModalCountry.Trim()
        };

        // Update the location
        var locationToEdit = _editingLocation;
        locationToEdit.Name = newName;
        locationToEdit.Address = newAddress;

        companyData.MarkAsModified();

        // Record undo action
        App.UndoRedoManager?.RecordAction(new DelegateAction(
            $"Edit location '{newName}'",
            () =>
            {
                locationToEdit.Name = oldName;
                locationToEdit.Address = oldAddress;
                companyData.MarkAsModified();
                LocationSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                locationToEdit.Name = newName;
                locationToEdit.Address = newAddress;
                companyData.MarkAsModified();
                LocationSaved?.Invoke(this, EventArgs.Empty);
            }));

        LocationSaved?.Invoke(this, EventArgs.Empty);
        CloseEditModal();
    }

    #endregion

    #region Delete Location

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    public void OpenDeleteConfirm(LocationDisplayItem? item)
    {
        if (item == null) return;
        _deletingLocation = item;
        IsDeleteConfirmOpen = true;
    }

    /// <summary>
    /// Closes the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingLocation = null;
    }

    /// <summary>
    /// Confirms and deletes the location.
    /// </summary>
    [RelayCommand]
    private void ConfirmDelete()
    {
        if (_deletingLocation == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var location = companyData.Locations?.FirstOrDefault(l => l.Id == _deletingLocation.Id);
        if (location != null)
        {
            var deletedLocation = location;
            companyData.Locations?.Remove(location);
            companyData.MarkAsModified();

            // Record undo action
            App.UndoRedoManager?.RecordAction(new DelegateAction(
                $"Delete location '{deletedLocation.Name}'",
                () =>
                {
                    companyData.Locations?.Add(deletedLocation);
                    companyData.MarkAsModified();
                    LocationDeleted?.Invoke(this, EventArgs.Empty);
                },
                () =>
                {
                    companyData.Locations?.Remove(deletedLocation);
                    companyData.MarkAsModified();
                    LocationDeleted?.Invoke(this, EventArgs.Empty);
                }));
        }

        LocationDeleted?.Invoke(this, EventArgs.Empty);
        CloseDeleteConfirm();
    }

    /// <summary>
    /// Gets the name of the location being deleted (for display in confirmation).
    /// </summary>
    public string DeletingLocationName => _deletingLocation?.Name ?? string.Empty;

    #endregion

    #region Filter Modal

    /// <summary>
    /// Opens the filter modal.
    /// </summary>
    public void OpenFilterModal()
    {
        IsFilterModalOpen = true;
    }

    /// <summary>
    /// Closes the filter modal.
    /// </summary>
    [RelayCommand]
    private void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    /// <summary>
    /// Applies the current filters and closes the modal.
    /// </summary>
    [RelayCommand]
    private void ApplyFilters()
    {
        FiltersApplied?.Invoke(this, new LocationsFilterAppliedEventArgs(FilterType, FilterStatus));
        CloseFilterModal();
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        FilterType = "All";
        FilterStatus = "All";
        CloseFilterModal();
    }

    #endregion

    #region Modal Helpers

    private void ClearModalFields()
    {
        ModalName = string.Empty;
        ModalCode = string.Empty;
        ModalType = "Warehouse";
        ModalStreetAddress = string.Empty;
        ModalCity = string.Empty;
        ModalStateProvince = string.Empty;
        ModalPostalCode = string.Empty;
        ModalCountry = string.Empty;
        ModalNotes = string.Empty;
        ModalNameError = null;
        ModalError = null;
    }

    private bool ValidateModal()
    {
        ModalNameError = null;
        ModalError = null;
        var isValid = true;

        // Validate name (required)
        if (string.IsNullOrWhiteSpace(ModalName))
        {
            ModalNameError = "Location name is required.";
            isValid = false;
        }

        return isValid;
    }

    #endregion
}

/// <summary>
/// Event args for filter applied events.
/// </summary>
public class LocationsFilterAppliedEventArgs : EventArgs
{
    public string Type { get; }
    public string Status { get; }

    public LocationsFilterAppliedEventArgs(string type, string status)
    {
        Type = type;
        Status = status;
    }
}
