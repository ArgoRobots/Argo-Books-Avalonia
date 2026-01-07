using ArgoBooks.Services;
using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
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
    private string _modalCompanyName = string.Empty;

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
            CompanyName = string.IsNullOrWhiteSpace(ModalCompanyName) ? null : ModalCompanyName.Trim(),
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
        App.UndoRedoManager?.RecordAction(new DelegateAction(
            $"Add customer '{newCustomer.Name}'",
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
        ModalCompanyName = customer.CompanyName ?? string.Empty;
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
        var oldCompanyName = _editingCustomer.CompanyName;
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
        var newCompanyName = string.IsNullOrWhiteSpace(ModalCompanyName) ? null : ModalCompanyName.Trim();
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

        // Check if anything actually changed
        var hasChanges = oldName != newName ||
                         oldCompanyName != newCompanyName ||
                         oldEmail != newEmail ||
                         oldPhone != newPhone ||
                         oldAddress.Street != newAddress.Street ||
                         oldAddress.City != newAddress.City ||
                         oldAddress.State != newAddress.State ||
                         oldAddress.ZipCode != newAddress.ZipCode ||
                         oldAddress.Country != newAddress.Country ||
                         oldNotes != newNotes ||
                         oldStatus != newStatus;

        // If nothing changed, just close the modal without recording an action
        if (!hasChanges)
        {
            CloseEditModal();
            return;
        }

        var customerToEdit = _editingCustomer;
        customerToEdit.Name = newName;
        customerToEdit.CompanyName = newCompanyName;
        customerToEdit.Email = newEmail;
        customerToEdit.Phone = newPhone;
        customerToEdit.Address = newAddress;
        customerToEdit.Notes = newNotes;
        customerToEdit.Status = newStatus;
        customerToEdit.UpdatedAt = DateTime.UtcNow;

        companyData.MarkAsModified();

        App.UndoRedoManager?.RecordAction(new DelegateAction(
            $"Edit customer '{newName}'",
            () =>
            {
                customerToEdit.Name = oldName;
                customerToEdit.CompanyName = oldCompanyName;
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
                customerToEdit.CompanyName = newCompanyName;
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

    public async void OpenDeleteConfirm(CustomerDisplayItem? item)
    {
        if (item == null)
            return;

        var dialog = App.ConfirmationDialog;
        if (dialog == null)
            return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Delete Customer",
            Message = $"Are you sure you want to delete this customer?\n\n{item.Name}",
            PrimaryButtonText = "Delete",
            CancelButtonText = "Cancel",
            IsPrimaryDestructive = true
        });

        if (result != ConfirmationResult.Primary)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var customer = companyData.Customers.FirstOrDefault(c => c.Id == item.Id);
        if (customer != null)
        {
            var deletedCustomer = customer;
            companyData.Customers.Remove(customer);
            companyData.MarkAsModified();

            App.UndoRedoManager?.RecordAction(new DelegateAction(
                $"Delete customer '{deletedCustomer.Name}'",
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
    }

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
        LoadCustomerHistory(item.Id);
        IsHistoryModalOpen = true;
    }

    /// <summary>
    /// Loads the transaction history for a customer.
    /// </summary>
    private void LoadCustomerHistory(string customerId)
    {
        CustomerHistory.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var historyItems = new List<CustomerHistoryItem>();

        // Add invoices
        var invoices = companyData.Invoices?.Where(i => i.CustomerId == customerId) ?? [];
        foreach (var invoice in invoices)
        {
            historyItems.Add(new CustomerHistoryItem
            {
                Date = invoice.IssueDate,
                Type = "Invoice",
                Description = $"Invoice #{invoice.InvoiceNumber}",
                Amount = invoice.Total,
                Status = invoice.Status.ToString()
            });
        }

        // Add payments
        var payments = companyData.Payments?.Where(p => p.CustomerId == customerId) ?? [];
        foreach (var payment in payments)
        {
            historyItems.Add(new CustomerHistoryItem
            {
                Date = payment.Date,
                Type = "Payment",
                Description = $"Payment - {payment.PaymentMethod}",
                Amount = -payment.Amount, // Negative because it reduces balance
                Status = "Completed"
            });
        }

        // Add rentals
        var rentals = companyData.Rentals?.Where(r => r.CustomerId == customerId) ?? [];
        foreach (var rental in rentals)
        {
            var item = companyData.RentalInventory?.FirstOrDefault(p => p.Id == rental.RentalItemId);
            historyItems.Add(new CustomerHistoryItem
            {
                Date = rental.StartDate,
                Type = "Rental",
                Description = $"Rental - {item?.Name ?? "Unknown Item"}",
                Amount = rental.TotalCost ?? 0,
                Status = rental.Status.ToString()
            });
        }

        // Add returns
        var returns = companyData.Returns?.Where(r => r.CustomerId == customerId) ?? [];
        foreach (var returnItem in returns)
        {
            var firstItem = returnItem.Items?.FirstOrDefault();
            var product = firstItem != null ? companyData.Products?.FirstOrDefault(p => p.Id == firstItem.ProductId) : null;
            historyItems.Add(new CustomerHistoryItem
            {
                Date = returnItem.ReturnDate,
                Type = "Return",
                Description = $"Return - {product?.Name ?? "Unknown Product"}",
                Amount = -returnItem.RefundAmount,
                Status = returnItem.Status.ToString()
            });
        }

        // Apply filters
        var filtered = ApplyHistoryFiltersInternal(historyItems);

        // Sort by date descending and add to collection
        foreach (var historyItem in filtered.OrderByDescending(h => h.Date))
        {
            CustomerHistory.Add(historyItem);
        }
    }

    /// <summary>
    /// Applies the history filters to the given items.
    /// </summary>
    private List<CustomerHistoryItem> ApplyHistoryFiltersInternal(List<CustomerHistoryItem> items)
    {
        var filtered = items.AsEnumerable();

        // Filter by type
        if (HistoryFilterType != "All")
        {
            filtered = filtered.Where(h => h.Type == HistoryFilterType);
        }

        // Filter by status
        if (HistoryFilterStatus != "All")
        {
            filtered = filtered.Where(h => h.Status == HistoryFilterStatus);
        }

        // Filter by date range
        if (HistoryFilterDateFrom.HasValue)
        {
            filtered = filtered.Where(h => h.Date.Date >= HistoryFilterDateFrom.Value.Date);
        }
        if (HistoryFilterDateTo.HasValue)
        {
            filtered = filtered.Where(h => h.Date.Date <= HistoryFilterDateTo.Value.Date);
        }

        // Filter by amount range
        if (decimal.TryParse(HistoryFilterAmountMin, out var minAmount))
        {
            filtered = filtered.Where(h => Math.Abs(h.Amount) >= minAmount);
        }
        if (decimal.TryParse(HistoryFilterAmountMax, out var maxAmount))
        {
            filtered = filtered.Where(h => Math.Abs(h.Amount) <= maxAmount);
        }

        return filtered.ToList();
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
        if (_historyCustomer != null)
        {
            LoadCustomerHistory(_historyCustomer.Id);
        }
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
        if (_historyCustomer != null)
        {
            LoadCustomerHistory(_historyCustomer.Id);
        }
        CloseHistoryFilterModal();
    }

    #endregion

    #region Modal Helpers

    private void ClearModalFields()
    {
        ModalFirstName = string.Empty;
        ModalLastName = string.Empty;
        ModalCompanyName = string.Empty;
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
