using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for customer modals, shared between CustomersPage and AppShell.
/// </summary>
public partial class CustomerModalsViewModel : ObservableObject
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
    private bool _isHistoryModalOpen;

    [ObservableProperty]
    private bool _isHistoryFilterModalOpen;

    #endregion

    #region Modal Form Fields

    [ObservableProperty]
    private string _modalFirstName = string.Empty;

    [ObservableProperty]
    private string _modalLastName = string.Empty;

    [ObservableProperty]
    private string _modalEmail = string.Empty;

    [ObservableProperty]
    private string _modalPhone = string.Empty;

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
    private string _modalStatus = "Active";

    [ObservableProperty]
    private string? _modalFirstNameError;

    [ObservableProperty]
    private string? _modalLastNameError;

    [ObservableProperty]
    private string? _modalEmailError;

    /// <summary>
    /// The customer being edited (null for add).
    /// </summary>
    private Customer? _editingCustomer;

    /// <summary>
    /// The customer being deleted.
    /// </summary>
    private CustomerDisplayItem? _deletingCustomer;

    /// <summary>
    /// The customer whose history is being viewed.
    /// </summary>
    private CustomerDisplayItem? _historyCustomer;

    #endregion

    #region Filter Fields

    [ObservableProperty]
    private string _filterPaymentStatus = "All";

    [ObservableProperty]
    private string _filterCustomerStatus = "All";

    [ObservableProperty]
    private string? _filterOutstandingMin;

    [ObservableProperty]
    private string? _filterOutstandingMax;

    [ObservableProperty]
    private DateTime? _filterLastRentalFrom;

    [ObservableProperty]
    private DateTime? _filterLastRentalTo;

    #endregion

    #region Customer History

    [ObservableProperty]
    private string _historyCustomerName = string.Empty;

    public ObservableCollection<CustomerHistoryItem> CustomerHistory { get; } = [];

    [ObservableProperty]
    private string _historyFilterType = "All";

    [ObservableProperty]
    private string _historyFilterStatus = "All";

    [ObservableProperty]
    private DateTime? _historyFilterDateFrom;

    [ObservableProperty]
    private DateTime? _historyFilterDateTo;

    [ObservableProperty]
    private string? _historyFilterAmountMin;

    [ObservableProperty]
    private string? _historyFilterAmountMax;

    public ObservableCollection<string> HistoryTypeOptions { get; } = ["All", "Rental", "Purchase", "Return", "Payment"];
    public ObservableCollection<string> HistoryStatusOptions { get; } = ["All", "Completed", "Pending", "Overdue", "Refunded"];

    #endregion

    #region Dropdown Options

    public ObservableCollection<string> StatusOptions { get; } = ["Active", "Inactive", "Banned"];
    public ObservableCollection<string> PaymentStatusOptions { get; } = ["All", "Current", "Overdue", "Delinquent"];
    public ObservableCollection<string> CustomerStatusOptions { get; } = ["All", "Active", "Inactive", "Banned"];

    #endregion

    #region Events

    /// <summary>
    /// Fired when a customer is saved (added or edited).
    /// </summary>
    public event EventHandler? CustomerSaved;

    /// <summary>
    /// Fired when a customer is deleted.
    /// </summary>
    public event EventHandler? CustomerDeleted;

    /// <summary>
    /// Fired when filters are applied.
    /// </summary>
    public event EventHandler? FiltersApplied;

    /// <summary>
    /// Fired when filters are cleared.
    /// </summary>
    public event EventHandler? FiltersCleared;

    #endregion

    #region Add Customer

    [RelayCommand]
    public void OpenAddModal()
    {
        _editingCustomer = null;
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
    public void SaveNewCustomer()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        companyData.IdCounters.Customer++;
        var newId = $"CUS-{companyData.IdCounters.Customer:D3}";

        var newCustomer = new Customer
        {
            Id = newId,
            Name = $"{ModalFirstName.Trim()} {ModalLastName.Trim()}".Trim(),
            Email = ModalEmail.Trim(),
            Phone = ModalPhone.Trim(),
            Address = new Core.Models.Common.Address
            {
                Street = ModalStreetAddress.Trim(),
                City = ModalCity.Trim(),
                State = ModalStateProvince.Trim(),
                ZipCode = ModalZipCode.Trim(),
                Country = ModalCountry.Trim()
            },
            Notes = ModalNotes.Trim(),
            Status = EntityStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        companyData.Customers.Add(newCustomer);
        companyData.MarkAsModified();

        var customerToUndo = newCustomer;
        App.UndoRedoManager?.RecordAction(new CustomerAddAction(
            $"Add customer '{newCustomer.Name}'",
            customerToUndo,
            () =>
            {
                companyData.Customers.Remove(customerToUndo);
                companyData.MarkAsModified();
                CustomerSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.Customers.Add(customerToUndo);
                companyData.MarkAsModified();
                CustomerSaved?.Invoke(this, EventArgs.Empty);
            }));

        CustomerSaved?.Invoke(this, EventArgs.Empty);
        CloseAddModal();
    }

    #endregion

    #region Edit Customer

    public void OpenEditModal(CustomerDisplayItem? item)
    {
        if (item == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        var customer = companyData?.Customers.FirstOrDefault(c => c.Id == item.Id);
        if (customer == null)
            return;

        _editingCustomer = customer;

        var nameParts = customer.Name.Split(' ', 2);
        ModalFirstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
        ModalLastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
        ModalEmail = customer.Email;
        ModalPhone = customer.Phone;
        ModalStreetAddress = customer.Address.Street;
        ModalCity = customer.Address.City;
        ModalStateProvince = customer.Address.State;
        ModalZipCode = customer.Address.ZipCode;
        ModalCountry = customer.Address.Country;
        ModalNotes = customer.Notes;
        ModalStatus = customer.Status switch
        {
            EntityStatus.Active => "Active",
            EntityStatus.Inactive => "Inactive",
            EntityStatus.Archived => "Banned",
            _ => "Active"
        };

        ClearModalErrors();
        IsEditModalOpen = true;
    }

    [RelayCommand]
    public void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingCustomer = null;
        ClearModalFields();
    }

    [RelayCommand]
    public void SaveEditedCustomer()
    {
        if (!ValidateModal() || _editingCustomer == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var oldName = _editingCustomer.Name;
        var oldEmail = _editingCustomer.Email;
        var oldPhone = _editingCustomer.Phone;
        var oldAddress = new Core.Models.Common.Address
        {
            Street = _editingCustomer.Address.Street,
            City = _editingCustomer.Address.City,
            State = _editingCustomer.Address.State,
            ZipCode = _editingCustomer.Address.ZipCode,
            Country = _editingCustomer.Address.Country
        };
        var oldNotes = _editingCustomer.Notes;
        var oldStatus = _editingCustomer.Status;

        var newName = $"{ModalFirstName.Trim()} {ModalLastName.Trim()}".Trim();
        var newEmail = ModalEmail.Trim();
        var newPhone = ModalPhone.Trim();
        var newAddress = new Core.Models.Common.Address
        {
            Street = ModalStreetAddress.Trim(),
            City = ModalCity.Trim(),
            State = ModalStateProvince.Trim(),
            ZipCode = ModalZipCode.Trim(),
            Country = ModalCountry.Trim()
        };
        var newNotes = ModalNotes.Trim();
        var newStatus = ModalStatus switch
        {
            "Active" => EntityStatus.Active,
            "Inactive" => EntityStatus.Inactive,
            "Banned" => EntityStatus.Archived,
            _ => EntityStatus.Active
        };

        var customerToEdit = _editingCustomer;
        customerToEdit.Name = newName;
        customerToEdit.Email = newEmail;
        customerToEdit.Phone = newPhone;
        customerToEdit.Address = newAddress;
        customerToEdit.Notes = newNotes;
        customerToEdit.Status = newStatus;
        customerToEdit.UpdatedAt = DateTime.UtcNow;

        companyData.MarkAsModified();

        App.UndoRedoManager?.RecordAction(new CustomerEditAction(
            $"Edit customer '{newName}'",
            customerToEdit,
            () =>
            {
                customerToEdit.Name = oldName;
                customerToEdit.Email = oldEmail;
                customerToEdit.Phone = oldPhone;
                customerToEdit.Address = oldAddress;
                customerToEdit.Notes = oldNotes;
                customerToEdit.Status = oldStatus;
                companyData.MarkAsModified();
                CustomerSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                customerToEdit.Name = newName;
                customerToEdit.Email = newEmail;
                customerToEdit.Phone = newPhone;
                customerToEdit.Address = newAddress;
                customerToEdit.Notes = newNotes;
                customerToEdit.Status = newStatus;
                companyData.MarkAsModified();
                CustomerSaved?.Invoke(this, EventArgs.Empty);
            }));

        CustomerSaved?.Invoke(this, EventArgs.Empty);
        CloseEditModal();
    }

    #endregion

    #region Delete Customer

    public void OpenDeleteConfirm(CustomerDisplayItem? item)
    {
        if (item == null)
            return;

        _deletingCustomer = item;
        OnPropertyChanged(nameof(DeletingCustomerName));
        IsDeleteConfirmOpen = true;
    }

    [RelayCommand]
    public void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingCustomer = null;
    }

    [RelayCommand]
    public void ConfirmDelete()
    {
        if (_deletingCustomer == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var customer = companyData.Customers.FirstOrDefault(c => c.Id == _deletingCustomer.Id);
        if (customer != null)
        {
            var deletedCustomer = customer;
            companyData.Customers.Remove(customer);
            companyData.MarkAsModified();

            App.UndoRedoManager?.RecordAction(new CustomerDeleteAction(
                $"Delete customer '{deletedCustomer.Name}'",
                deletedCustomer,
                () =>
                {
                    companyData.Customers.Add(deletedCustomer);
                    companyData.MarkAsModified();
                    CustomerDeleted?.Invoke(this, EventArgs.Empty);
                },
                () =>
                {
                    companyData.Customers.Remove(deletedCustomer);
                    companyData.MarkAsModified();
                    CustomerDeleted?.Invoke(this, EventArgs.Empty);
                }));
        }

        CustomerDeleted?.Invoke(this, EventArgs.Empty);
        CloseDeleteConfirm();
    }

    public string DeletingCustomerName => _deletingCustomer?.Name ?? string.Empty;

    #endregion

    #region Filter Modal

    [RelayCommand]
    public void OpenFilterModal()
    {
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
        FilterPaymentStatus = "All";
        FilterCustomerStatus = "All";
        FilterOutstandingMin = null;
        FilterOutstandingMax = null;
        FilterLastRentalFrom = null;
        FilterLastRentalTo = null;
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    #endregion

    #region Customer History Modal

    public void OpenHistoryModal(CustomerDisplayItem? item)
    {
        if (item == null)
            return;

        _historyCustomer = item;
        HistoryCustomerName = item.Name;
        CustomerHistory.Clear();
        IsHistoryModalOpen = true;
    }

    [RelayCommand]
    public void CloseHistoryModal()
    {
        IsHistoryModalOpen = false;
        _historyCustomer = null;
        CustomerHistory.Clear();
    }

    [RelayCommand]
    public void OpenHistoryFilterModal()
    {
        IsHistoryFilterModalOpen = true;
    }

    [RelayCommand]
    public void CloseHistoryFilterModal()
    {
        IsHistoryFilterModalOpen = false;
    }

    [RelayCommand]
    public void ApplyHistoryFilters()
    {
        CloseHistoryFilterModal();
    }

    [RelayCommand]
    public void ClearHistoryFilters()
    {
        HistoryFilterType = "All";
        HistoryFilterStatus = "All";
        HistoryFilterDateFrom = null;
        HistoryFilterDateTo = null;
        HistoryFilterAmountMin = null;
        HistoryFilterAmountMax = null;
        CloseHistoryFilterModal();
    }

    #endregion

    #region Modal Helpers

    private void ClearModalFields()
    {
        ModalFirstName = string.Empty;
        ModalLastName = string.Empty;
        ModalEmail = string.Empty;
        ModalPhone = string.Empty;
        ModalStreetAddress = string.Empty;
        ModalCity = string.Empty;
        ModalStateProvince = string.Empty;
        ModalZipCode = string.Empty;
        ModalCountry = string.Empty;
        ModalNotes = string.Empty;
        ModalStatus = "Active";
        ClearModalErrors();
    }

    private void ClearModalErrors()
    {
        ModalFirstNameError = null;
        ModalLastNameError = null;
        ModalEmailError = null;
    }

    private bool ValidateModal()
    {
        ClearModalErrors();
        var isValid = true;

        if (string.IsNullOrWhiteSpace(ModalFirstName))
        {
            ModalFirstNameError = "First name is required.";
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(ModalLastName))
        {
            ModalLastNameError = "Last name is required.";
            isValid = false;
        }

        if (!string.IsNullOrWhiteSpace(ModalEmail))
        {
            if (!ModalEmail.Contains('@') || !ModalEmail.Contains('.'))
            {
                ModalEmailError = "Please enter a valid email address.";
                isValid = false;
            }
        }

        return isValid;
    }

    #endregion
}
