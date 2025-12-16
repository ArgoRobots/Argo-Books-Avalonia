using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Suppliers page.
/// </summary>
public partial class SuppliersPageViewModel : ViewModelBase
{
    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        FilterSuppliers();
    }

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string? _filterCountry;

    #endregion

    #region Statistics

    [ObservableProperty]
    private int _totalSuppliers;

    [ObservableProperty]
    private int _activeSuppliers;

    [ObservableProperty]
    private int _totalCountries;

    [ObservableProperty]
    private int _totalProductsSupplied;

    #endregion

    #region Suppliers Collection

    /// <summary>
    /// All suppliers (unfiltered).
    /// </summary>
    private readonly List<Supplier> _allSuppliers = [];

    /// <summary>
    /// Filtered suppliers for display.
    /// </summary>
    public ObservableCollection<SupplierDisplayItem> Suppliers { get; } = [];

    /// <summary>
    /// Available countries for filter dropdown.
    /// </summary>
    public ObservableCollection<string> AvailableCountries { get; } = [];

    /// <summary>
    /// Available status options for filter.
    /// </summary>
    public ObservableCollection<string> StatusOptions { get; } = ["All", "Active", "Inactive"];

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
    private string _modalSupplierName = string.Empty;

    [ObservableProperty]
    private string _modalContactPerson = string.Empty;

    [ObservableProperty]
    private string _modalEmail = string.Empty;

    [ObservableProperty]
    private string _modalPhone = string.Empty;

    [ObservableProperty]
    private string _modalStreet = string.Empty;

    [ObservableProperty]
    private string _modalCity = string.Empty;

    [ObservableProperty]
    private string _modalState = string.Empty;

    [ObservableProperty]
    private string _modalZipCode = string.Empty;

    [ObservableProperty]
    private string _modalCountry = string.Empty;

    [ObservableProperty]
    private string _modalWebsite = string.Empty;

    [ObservableProperty]
    private string _modalPaymentTerms = string.Empty;

    [ObservableProperty]
    private string _modalNotes = string.Empty;

    [ObservableProperty]
    private bool _modalIsActive = true;

    [ObservableProperty]
    private string? _modalError;

    /// <summary>
    /// The supplier being edited (null for add).
    /// </summary>
    private Supplier? _editingSupplier;

    /// <summary>
    /// The supplier being deleted.
    /// </summary>
    private SupplierDisplayItem? _deletingSupplier;

    #endregion

    #region Dropdown Options

    /// <summary>
    /// Common payment terms options.
    /// </summary>
    public ObservableCollection<string> PaymentTermsOptions { get; } =
    [
        "Due on Receipt",
        "Net 15",
        "Net 30",
        "Net 45",
        "Net 60",
        "Net 90"
    ];

    /// <summary>
    /// Common countries for dropdown.
    /// </summary>
    public ObservableCollection<string> CountryOptions { get; } =
    [
        "United States",
        "Canada",
        "United Kingdom",
        "Germany",
        "France",
        "Australia",
        "Japan",
        "China",
        "India",
        "Mexico",
        "Brazil",
        "Italy",
        "Spain",
        "Netherlands",
        "South Korea",
        "Singapore"
    ];

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public SuppliersPageViewModel()
    {
        LoadSuppliers();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads suppliers from the company data.
    /// </summary>
    private void LoadSuppliers()
    {
        _allSuppliers.Clear();
        Suppliers.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Suppliers == null)
            return;

        _allSuppliers.AddRange(companyData.Suppliers);
        UpdateStatistics();
        UpdateAvailableCountries();
        FilterSuppliers();
    }

    /// <summary>
    /// Updates the statistics based on current data.
    /// </summary>
    private void UpdateStatistics()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        TotalSuppliers = _allSuppliers.Count;

        // Count active suppliers (those with at least one product or used in purchases)
        var suppliersWithProducts = companyData.Products
            .Where(p => !string.IsNullOrEmpty(p.SupplierId))
            .Select(p => p.SupplierId)
            .Distinct()
            .ToHashSet();

        ActiveSuppliers = _allSuppliers.Count(s => suppliersWithProducts.Contains(s.Id));

        // Count unique countries
        TotalCountries = _allSuppliers
            .Select(s => s.Address.Country)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        // Count products supplied
        TotalProductsSupplied = companyData.Products
            .Count(p => !string.IsNullOrEmpty(p.SupplierId));
    }

    /// <summary>
    /// Updates the list of available countries for filtering.
    /// </summary>
    private void UpdateAvailableCountries()
    {
        AvailableCountries.Clear();
        AvailableCountries.Add("All Countries");

        var countries = _allSuppliers
            .Select(s => s.Address.Country)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c);

        foreach (var country in countries)
        {
            AvailableCountries.Add(country);
        }
    }

    /// <summary>
    /// Refreshes the suppliers from the data source.
    /// </summary>
    [RelayCommand]
    private void RefreshSuppliers()
    {
        LoadSuppliers();
    }

    /// <summary>
    /// Filters suppliers based on search query and filters.
    /// </summary>
    private void FilterSuppliers()
    {
        Suppliers.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var filtered = _allSuppliers.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered
                .Select(s => new
                {
                    Supplier = s,
                    NameScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, s.Name),
                    EmailScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, s.Email),
                    ContactScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, s.ContactPerson)
                })
                .Where(x => x.NameScore >= 0 || x.EmailScore >= 0 || x.ContactScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.NameScore, x.EmailScore), x.ContactScore))
                .Select(x => x.Supplier);
        }

        // Apply country filter
        if (!string.IsNullOrWhiteSpace(FilterCountry) && FilterCountry != "All Countries")
        {
            filtered = filtered.Where(s =>
                s.Address.Country.Equals(FilterCountry, StringComparison.OrdinalIgnoreCase));
        }

        // Apply status filter
        if (FilterStatus != "All")
        {
            var suppliersWithProducts = companyData.Products
                .Where(p => !string.IsNullOrEmpty(p.SupplierId))
                .Select(p => p.SupplierId)
                .Distinct()
                .ToHashSet();

            filtered = FilterStatus == "Active"
                ? filtered.Where(s => suppliersWithProducts.Contains(s.Id))
                : filtered.Where(s => !suppliersWithProducts.Contains(s.Id));
        }

        // Create display items
        foreach (var supplier in filtered.OrderBy(s => s.Name))
        {
            var productCount = companyData.Products.Count(p => p.SupplierId == supplier.Id);
            var isActive = productCount > 0;

            Suppliers.Add(new SupplierDisplayItem
            {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Email = supplier.Email,
                Phone = supplier.Phone,
                Country = supplier.Address.Country,
                ProductCount = productCount,
                IsActive = isActive,
                Initials = GetInitials(supplier.Name)
            });
        }
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
            return $"{words[0][0]}{words[1][0]}".ToUpperInvariant();

        return name.Length >= 2
            ? name[..2].ToUpperInvariant()
            : name.ToUpperInvariant();
    }

    #endregion

    #region Add Supplier

    /// <summary>
    /// Opens the Add Supplier modal.
    /// </summary>
    [RelayCommand]
    private void OpenAddModal()
    {
        _editingSupplier = null;
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
    /// Saves a new supplier.
    /// </summary>
    [RelayCommand]
    private void SaveNewSupplier()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Generate new ID
        companyData.IdCounters.Supplier++;
        var newId = $"SUP-{companyData.IdCounters.Supplier:D3}";

        var newSupplier = new Supplier
        {
            Id = newId,
            Name = ModalSupplierName.Trim(),
            ContactPerson = ModalContactPerson.Trim(),
            Email = ModalEmail.Trim(),
            Phone = ModalPhone.Trim(),
            Address = new Address
            {
                Street = ModalStreet.Trim(),
                City = ModalCity.Trim(),
                State = ModalState.Trim(),
                ZipCode = ModalZipCode.Trim(),
                Country = ModalCountry.Trim()
            },
            Website = string.IsNullOrWhiteSpace(ModalWebsite) ? null : ModalWebsite.Trim(),
            PaymentTerms = ModalPaymentTerms.Trim(),
            Notes = ModalNotes.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        companyData.Suppliers.Add(newSupplier);
        companyData.MarkAsModified();

        // Record undo action
        var supplierToUndo = newSupplier;
        App.UndoRedoManager?.RecordAction(new SupplierAddAction(
            $"Add supplier '{newSupplier.Name}'",
            supplierToUndo,
            () =>
            {
                companyData.Suppliers.Remove(supplierToUndo);
                companyData.MarkAsModified();
                LoadSuppliers();
            },
            () =>
            {
                companyData.Suppliers.Add(supplierToUndo);
                companyData.MarkAsModified();
                LoadSuppliers();
            }));

        // Reload and close
        LoadSuppliers();
        CloseAddModal();
    }

    #endregion

    #region Edit Supplier

    /// <summary>
    /// Opens the Edit Supplier modal.
    /// </summary>
    [RelayCommand]
    private void OpenEditModal(SupplierDisplayItem? item)
    {
        if (item == null)
            return;

        var supplier = _allSuppliers.FirstOrDefault(s => s.Id == item.Id);
        if (supplier == null)
            return;

        _editingSupplier = supplier;

        // Populate fields
        ModalSupplierName = supplier.Name;
        ModalContactPerson = supplier.ContactPerson;
        ModalEmail = supplier.Email;
        ModalPhone = supplier.Phone;
        ModalStreet = supplier.Address.Street;
        ModalCity = supplier.Address.City;
        ModalState = supplier.Address.State;
        ModalZipCode = supplier.Address.ZipCode;
        ModalCountry = supplier.Address.Country;
        ModalWebsite = supplier.Website ?? string.Empty;
        ModalPaymentTerms = supplier.PaymentTerms;
        ModalNotes = supplier.Notes;

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
        _editingSupplier = null;
        ClearModalFields();
    }

    /// <summary>
    /// Saves changes to an existing supplier.
    /// </summary>
    [RelayCommand]
    private void SaveEditedSupplier()
    {
        if (!ValidateModal() || _editingSupplier == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Store old values for undo
        var oldName = _editingSupplier.Name;
        var oldContactPerson = _editingSupplier.ContactPerson;
        var oldEmail = _editingSupplier.Email;
        var oldPhone = _editingSupplier.Phone;
        var oldAddress = new Address
        {
            Street = _editingSupplier.Address.Street,
            City = _editingSupplier.Address.City,
            State = _editingSupplier.Address.State,
            ZipCode = _editingSupplier.Address.ZipCode,
            Country = _editingSupplier.Address.Country
        };
        var oldWebsite = _editingSupplier.Website;
        var oldPaymentTerms = _editingSupplier.PaymentTerms;
        var oldNotes = _editingSupplier.Notes;

        // Store new values
        var newName = ModalSupplierName.Trim();
        var newContactPerson = ModalContactPerson.Trim();
        var newEmail = ModalEmail.Trim();
        var newPhone = ModalPhone.Trim();
        var newAddress = new Address
        {
            Street = ModalStreet.Trim(),
            City = ModalCity.Trim(),
            State = ModalState.Trim(),
            ZipCode = ModalZipCode.Trim(),
            Country = ModalCountry.Trim()
        };
        var newWebsite = string.IsNullOrWhiteSpace(ModalWebsite) ? null : ModalWebsite.Trim();
        var newPaymentTerms = ModalPaymentTerms.Trim();
        var newNotes = ModalNotes.Trim();

        // Update the supplier
        var supplierToEdit = _editingSupplier;
        supplierToEdit.Name = newName;
        supplierToEdit.ContactPerson = newContactPerson;
        supplierToEdit.Email = newEmail;
        supplierToEdit.Phone = newPhone;
        supplierToEdit.Address = newAddress;
        supplierToEdit.Website = newWebsite;
        supplierToEdit.PaymentTerms = newPaymentTerms;
        supplierToEdit.Notes = newNotes;
        supplierToEdit.UpdatedAt = DateTime.UtcNow;

        companyData.MarkAsModified();

        // Record undo action
        App.UndoRedoManager?.RecordAction(new SupplierEditAction(
            $"Edit supplier '{newName}'",
            supplierToEdit,
            () =>
            {
                supplierToEdit.Name = oldName;
                supplierToEdit.ContactPerson = oldContactPerson;
                supplierToEdit.Email = oldEmail;
                supplierToEdit.Phone = oldPhone;
                supplierToEdit.Address = oldAddress;
                supplierToEdit.Website = oldWebsite;
                supplierToEdit.PaymentTerms = oldPaymentTerms;
                supplierToEdit.Notes = oldNotes;
                companyData.MarkAsModified();
                LoadSuppliers();
            },
            () =>
            {
                supplierToEdit.Name = newName;
                supplierToEdit.ContactPerson = newContactPerson;
                supplierToEdit.Email = newEmail;
                supplierToEdit.Phone = newPhone;
                supplierToEdit.Address = newAddress;
                supplierToEdit.Website = newWebsite;
                supplierToEdit.PaymentTerms = newPaymentTerms;
                supplierToEdit.Notes = newNotes;
                companyData.MarkAsModified();
                LoadSuppliers();
            }));

        // Reload and close
        LoadSuppliers();
        CloseEditModal();
    }

    #endregion

    #region Delete Supplier

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void OpenDeleteConfirm(SupplierDisplayItem? item)
    {
        if (item == null)
            return;

        _deletingSupplier = item;
        OnPropertyChanged(nameof(DeletingSupplierName));
        OnPropertyChanged(nameof(DeletingSupplierProductCount));
        IsDeleteConfirmOpen = true;
    }

    /// <summary>
    /// Closes the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingSupplier = null;
    }

    /// <summary>
    /// Confirms and deletes the supplier.
    /// </summary>
    [RelayCommand]
    private void ConfirmDelete()
    {
        if (_deletingSupplier == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var supplier = companyData.Suppliers.FirstOrDefault(s => s.Id == _deletingSupplier.Id);
        if (supplier != null)
        {
            // Clear supplier references from products
            var affectedProducts = companyData.Products
                .Where(p => p.SupplierId == supplier.Id)
                .ToList();

            foreach (var product in affectedProducts)
            {
                product.SupplierId = null;
            }

            var deletedSupplier = supplier;
            var productSupplierMappings = affectedProducts.ToDictionary(p => p.Id, _ => supplier.Id);

            companyData.Suppliers.Remove(supplier);
            companyData.MarkAsModified();

            // Record undo action
            App.UndoRedoManager?.RecordAction(new SupplierDeleteAction(
                $"Delete supplier '{deletedSupplier.Name}'",
                deletedSupplier,
                () =>
                {
                    // Undo: restore supplier and product references
                    companyData.Suppliers.Add(deletedSupplier);
                    foreach (var kvp in productSupplierMappings)
                    {
                        var product = companyData.Products.FirstOrDefault(p => p.Id == kvp.Key);
                        if (product != null)
                        {
                            product.SupplierId = kvp.Value;
                        }
                    }
                    companyData.MarkAsModified();
                    LoadSuppliers();
                },
                () =>
                {
                    // Redo: delete again
                    foreach (var kvp in productSupplierMappings)
                    {
                        var product = companyData.Products.FirstOrDefault(p => p.Id == kvp.Key);
                        if (product != null)
                        {
                            product.SupplierId = null;
                        }
                    }
                    companyData.Suppliers.Remove(deletedSupplier);
                    companyData.MarkAsModified();
                    LoadSuppliers();
                }));
        }

        LoadSuppliers();
        CloseDeleteConfirm();
    }

    /// <summary>
    /// Gets the name of the supplier being deleted (for display in confirmation).
    /// </summary>
    public string DeletingSupplierName => _deletingSupplier?.Name ?? string.Empty;

    /// <summary>
    /// Gets the product count of the supplier being deleted.
    /// </summary>
    public int DeletingSupplierProductCount => _deletingSupplier?.ProductCount ?? 0;

    #endregion

    #region Filter Modal

    /// <summary>
    /// Opens the filter modal.
    /// </summary>
    [RelayCommand]
    private void OpenFilterModal()
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
        FilterSuppliers();
        CloseFilterModal();
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        FilterStatus = "All";
        FilterCountry = null;
        SearchQuery = null;
        FilterSuppliers();
        CloseFilterModal();
    }

    #endregion

    #region Modal Helpers

    private void ClearModalFields()
    {
        ModalSupplierName = string.Empty;
        ModalContactPerson = string.Empty;
        ModalEmail = string.Empty;
        ModalPhone = string.Empty;
        ModalStreet = string.Empty;
        ModalCity = string.Empty;
        ModalState = string.Empty;
        ModalZipCode = string.Empty;
        ModalCountry = string.Empty;
        ModalWebsite = string.Empty;
        ModalPaymentTerms = string.Empty;
        ModalNotes = string.Empty;
        ModalIsActive = true;
        ModalError = null;
    }

    private bool ValidateModal()
    {
        ModalError = null;

        if (string.IsNullOrWhiteSpace(ModalSupplierName))
        {
            ModalError = "Supplier name is required.";
            return false;
        }

        // Check for duplicate names
        var existingWithSameName = _allSuppliers.Any(s =>
            s.Name.Equals(ModalSupplierName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            (_editingSupplier == null || s.Id != _editingSupplier.Id));

        if (existingWithSameName)
        {
            ModalError = "A supplier with this name already exists.";
            return false;
        }

        // Validate email format if provided
        if (!string.IsNullOrWhiteSpace(ModalEmail) &&
            !ModalEmail.Contains('@'))
        {
            ModalError = "Please enter a valid email address.";
            return false;
        }

        return true;
    }

    #endregion
}

/// <summary>
/// Display model for suppliers in the UI.
/// </summary>
public partial class SupplierDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _contactPerson = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _country = string.Empty;

    [ObservableProperty]
    private int _productCount;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private string _initials = string.Empty;

    /// <summary>
    /// Status text for display.
    /// </summary>
    public string StatusText => IsActive ? "Active" : "Inactive";
}

/// <summary>
/// Undoable action for adding a supplier.
/// </summary>
public class SupplierAddAction : IUndoableAction
{
    private readonly Supplier _supplier;
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public SupplierAddAction(string description, Supplier supplier, Action undoAction, Action redoAction)
    {
        Description = description;
        _supplier = supplier;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for editing a supplier.
/// </summary>
public class SupplierEditAction : IUndoableAction
{
    private readonly Supplier _supplier;
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public SupplierEditAction(string description, Supplier supplier, Action undoAction, Action redoAction)
    {
        Description = description;
        _supplier = supplier;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for deleting a supplier.
/// </summary>
public class SupplierDeleteAction : IUndoableAction
{
    private readonly Supplier _supplier;
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public SupplierDeleteAction(string description, Supplier supplier, Action undoAction, Action redoAction)
    {
        Description = description;
        _supplier = supplier;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}
