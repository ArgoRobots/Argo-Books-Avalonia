using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Customers page.
/// </summary>
public partial class CustomersPageViewModel : ViewModelBase
{
    #region Statistics

    [ObservableProperty]
    private int _totalCustomers;

    [ObservableProperty]
    private int _activeCustomers;

    [ObservableProperty]
    private int _overduePayments;

    [ObservableProperty]
    private int _bannedCustomers;

    #endregion

    #region Table Column Widths

    /// <summary>
    /// Column widths manager for the table.
    /// </summary>
    public CustomersTableColumnWidths ColumnWidths { get; } = new CustomersTableColumnWidths();

    #endregion

    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterCustomers();
    }

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

    #region Sorting

    [ObservableProperty]
    private string _sortColumn = "Name";

    [ObservableProperty]
    private SortDirection _sortDirection = SortDirection.None;

    /// <summary>
    /// Sorts the customers list by the specified column.
    /// </summary>
    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column)
        {
            SortDirection = SortDirection switch
            {
                SortDirection.None => SortDirection.Ascending,
                SortDirection.Ascending => SortDirection.Descending,
                SortDirection.Descending => SortDirection.None,
                _ => SortDirection.Ascending
            };
        }
        else
        {
            SortColumn = column;
            SortDirection = SortDirection.Ascending;
        }
        FilterCustomers();
    }

    #endregion

    #region Customers Collection

    /// <summary>
    /// All customers (unfiltered).
    /// </summary>
    private readonly List<Customer> _allCustomers = [];

    /// <summary>
    /// Customers for display in the table.
    /// </summary>
    public ObservableCollection<CustomerDisplayItem> Customers { get; } = [];

    /// <summary>
    /// Payment status options for filter.
    /// </summary>
    public ObservableCollection<string> PaymentStatusOptions { get; } = ["All", "Current", "Overdue", "Delinquent"];

    /// <summary>
    /// Customer status options for filter.
    /// </summary>
    public ObservableCollection<string> CustomerStatusOptions { get; } = ["All", "Active", "Inactive", "Banned"];

    #endregion

    #region Pagination

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _pageSize = 10;

    public ObservableCollection<int> PageSizeOptions { get; } = [10, 25, 50, 100];

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        FilterCustomers();
    }

    [ObservableProperty]
    private string _paginationText = "0 customers";

    public ObservableCollection<int> PageNumbers { get; } = [];

    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages;

    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        FilterCustomers();
    }

    [RelayCommand]
    private void GoToPreviousPage()
    {
        if (CanGoToPreviousPage)
            CurrentPage--;
    }

    [RelayCommand]
    private void GoToNextPage()
    {
        if (CanGoToNextPage)
            CurrentPage++;
    }

    [RelayCommand]
    private void GoToPage(int page)
    {
        if (page >= 1 && page <= TotalPages)
            CurrentPage = page;
    }

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

    #region Customer History

    [ObservableProperty]
    private string _historyCustomerName = string.Empty;

    /// <summary>
    /// Transaction history for the selected customer.
    /// </summary>
    public ObservableCollection<CustomerHistoryItem> CustomerHistory { get; } = [];

    /// <summary>
    /// Transaction type filter for history.
    /// </summary>
    [ObservableProperty]
    private string _historyFilterType = "All";

    /// <summary>
    /// Status filter for history.
    /// </summary>
    [ObservableProperty]
    private string _historyFilterStatus = "All";

    /// <summary>
    /// Date from filter for history.
    /// </summary>
    [ObservableProperty]
    private DateTime? _historyFilterDateFrom;

    /// <summary>
    /// Date to filter for history.
    /// </summary>
    [ObservableProperty]
    private DateTime? _historyFilterDateTo;

    /// <summary>
    /// Amount min filter for history.
    /// </summary>
    [ObservableProperty]
    private string? _historyFilterAmountMin;

    /// <summary>
    /// Amount max filter for history.
    /// </summary>
    [ObservableProperty]
    private string? _historyFilterAmountMax;

    /// <summary>
    /// Transaction type options for history filter.
    /// </summary>
    public ObservableCollection<string> HistoryTypeOptions { get; } = ["All", "Rental", "Purchase", "Return", "Payment"];

    /// <summary>
    /// Status options for history filter.
    /// </summary>
    public ObservableCollection<string> HistoryStatusOptions { get; } = ["All", "Completed", "Pending", "Overdue", "Refunded"];

    #endregion

    #region Dropdown Options

    /// <summary>
    /// Status options for edit modal.
    /// </summary>
    public ObservableCollection<string> StatusOptions { get; } = ["Active", "Inactive", "Banned"];

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public CustomersPageViewModel()
    {
        LoadCustomers();

        // Subscribe to undo/redo state changes to refresh UI
        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }

        // Subscribe to customer modal events to refresh data
        if (App.CustomerModalsViewModel != null)
        {
            App.CustomerModalsViewModel.CustomerSaved += OnCustomerSaved;
            App.CustomerModalsViewModel.CustomerDeleted += OnCustomerDeleted;
            App.CustomerModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.CustomerModalsViewModel.FiltersCleared += OnFiltersCleared;
        }
    }

    /// <summary>
    /// Handles undo/redo state changes by refreshing the customers.
    /// </summary>
    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadCustomers();
    }

    /// <summary>
    /// Handles customer saved event from modals.
    /// </summary>
    private void OnCustomerSaved(object? sender, EventArgs e)
    {
        LoadCustomers();
    }

    /// <summary>
    /// Handles customer deleted event from modals.
    /// </summary>
    private void OnCustomerDeleted(object? sender, EventArgs e)
    {
        LoadCustomers();
    }

    /// <summary>
    /// Handles filters applied event from modals.
    /// </summary>
    private void OnFiltersApplied(object? sender, EventArgs e)
    {
        // Copy filter values from shared ViewModel
        var modals = App.CustomerModalsViewModel;
        if (modals != null)
        {
            FilterPaymentStatus = modals.FilterPaymentStatus;
            FilterCustomerStatus = modals.FilterCustomerStatus;
            FilterOutstandingMin = modals.FilterOutstandingMin;
            FilterOutstandingMax = modals.FilterOutstandingMax;
            FilterLastRentalFrom = modals.FilterLastRentalFrom;
            FilterLastRentalTo = modals.FilterLastRentalTo;
        }
        CurrentPage = 1;
        FilterCustomers();
    }

    /// <summary>
    /// Handles filters cleared event from modals.
    /// </summary>
    private void OnFiltersCleared(object? sender, EventArgs e)
    {
        FilterPaymentStatus = "All";
        FilterCustomerStatus = "All";
        FilterOutstandingMin = null;
        FilterOutstandingMax = null;
        FilterLastRentalFrom = null;
        FilterLastRentalTo = null;
        SearchQuery = null;
        CurrentPage = 1;
        FilterCustomers();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads customers from the company data.
    /// </summary>
    private void LoadCustomers()
    {
        _allCustomers.Clear();
        Customers.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Customers == null)
            return;

        _allCustomers.AddRange(companyData.Customers);
        UpdateStatistics();
        FilterCustomers();
    }

    /// <summary>
    /// Updates the statistics based on current data.
    /// </summary>
    private void UpdateStatistics()
    {
        TotalCustomers = _allCustomers.Count;
        ActiveCustomers = _allCustomers.Count(c => c.Status == EntityStatus.Active);

        // Count customers with overdue payments (those with outstanding balance and overdue status)
        // In a real scenario, this would be based on invoice/rental status
        OverduePayments = _allCustomers.Count(c => c.TotalPurchases > 0 && c.Status == EntityStatus.Active);

        BannedCustomers = _allCustomers.Count(c => c.Status == EntityStatus.Archived);
    }

    /// <summary>
    /// Refreshes the customers from the data source.
    /// </summary>
    [RelayCommand]
    private void RefreshCustomers()
    {
        LoadCustomers();
    }

    /// <summary>
    /// Filters customers based on search query and filters.
    /// </summary>
    private void FilterCustomers()
    {
        Customers.Clear();

        var filtered = _allCustomers.ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered
                .Select(c => new
                {
                    Customer = c,
                    NameScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, c.Name),
                    EmailScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, c.Email),
                    PhoneScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, c.Phone),
                    IdScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, c.Id)
                })
                .Where(x => x.NameScore >= 0 || x.EmailScore >= 0 || x.PhoneScore >= 0 || x.IdScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.NameScore, x.EmailScore), Math.Max(x.PhoneScore, x.IdScore)))
                .Select(x => x.Customer)
                .ToList();
        }

        // Apply payment status filter
        if (FilterPaymentStatus != "All")
        {
            // In a real scenario, this would be based on actual payment data
            // For now, we'll simulate based on total purchases
            filtered = FilterPaymentStatus switch
            {
                "Current" => filtered.Where(c => c.TotalPurchases == 0).ToList(),
                "Overdue" => filtered.Where(c => c.TotalPurchases > 0 && c.TotalPurchases < 500).ToList(),
                "Delinquent" => filtered.Where(c => c.TotalPurchases >= 500).ToList(),
                _ => filtered
            };
        }

        // Apply customer status filter
        if (FilterCustomerStatus != "All")
        {
            var status = FilterCustomerStatus switch
            {
                "Active" => EntityStatus.Active,
                "Inactive" => EntityStatus.Inactive,
                "Banned" => EntityStatus.Archived,
                _ => EntityStatus.Active
            };
            filtered = filtered.Where(c => c.Status == status).ToList();
        }

        // Apply outstanding amount filter
        if (decimal.TryParse(FilterOutstandingMin, out var minAmount))
        {
            filtered = filtered.Where(c => c.TotalPurchases >= minAmount).ToList();
        }
        if (decimal.TryParse(FilterOutstandingMax, out var maxAmount))
        {
            filtered = filtered.Where(c => c.TotalPurchases <= maxAmount).ToList();
        }

        // Apply last rental date filter
        if (FilterLastRentalFrom.HasValue)
        {
            filtered = filtered.Where(c => c.LastTransactionDate >= FilterLastRentalFrom.Value).ToList();
        }
        if (FilterLastRentalTo.HasValue)
        {
            filtered = filtered.Where(c => c.LastTransactionDate <= FilterLastRentalTo.Value).ToList();
        }

        // Create display items
        var displayItems = filtered.Select(customer =>
        {
            // Determine payment status based on total purchases
            var paymentStatus = customer.TotalPurchases switch
            {
                0 => "Current",
                < 500 => "Overdue",
                _ => "Delinquent"
            };

            // Get address as string
            var addressParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(customer.Address.Street))
                addressParts.Add(customer.Address.Street);
            if (!string.IsNullOrWhiteSpace(customer.Address.City))
                addressParts.Add(customer.Address.City);
            if (!string.IsNullOrWhiteSpace(customer.Address.State))
                addressParts.Add(customer.Address.State);
            var addressString = addressParts.Count > 0 ? string.Join(", ", addressParts) : "-";

            return new CustomerDisplayItem
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = string.IsNullOrWhiteSpace(customer.Email) ? "-" : customer.Email,
                Phone = string.IsNullOrWhiteSpace(customer.Phone) ? "-" : customer.Phone,
                Address = addressString,
                PaymentStatus = paymentStatus,
                Outstanding = customer.TotalPurchases,
                LastRental = customer.LastTransactionDate,
                Status = customer.Status
            };
        }).ToList();

        // Apply sorting
        if (SortDirection != SortDirection.None)
        {
            displayItems = SortColumn switch
            {
                "Name" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(c => c.Name).ToList()
                    : displayItems.OrderByDescending(c => c.Name).ToList(),
                "Email" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(c => c.Email).ToList()
                    : displayItems.OrderByDescending(c => c.Email).ToList(),
                "Phone" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(c => c.Phone).ToList()
                    : displayItems.OrderByDescending(c => c.Phone).ToList(),
                "Address" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(c => c.Address).ToList()
                    : displayItems.OrderByDescending(c => c.Address).ToList(),
                "PaymentStatus" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(c => c.PaymentStatus).ToList()
                    : displayItems.OrderByDescending(c => c.PaymentStatus).ToList(),
                "Outstanding" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(c => c.Outstanding).ToList()
                    : displayItems.OrderByDescending(c => c.Outstanding).ToList(),
                "LastRental" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(c => c.LastRental).ToList()
                    : displayItems.OrderByDescending(c => c.LastRental).ToList(),
                _ => displayItems.OrderBy(c => c.Name).ToList()
            };
        }
        else if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            // Default sort by name when not searching
            displayItems = displayItems.OrderBy(c => c.Name).ToList();
        }

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination and add to collection
        var pagedCustomers = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedCustomers)
        {
            Customers.Add(item);
        }
    }

    private void UpdatePageNumbers()
    {
        PageNumbers.Clear();
        var startPage = Math.Max(1, CurrentPage - 2);
        var endPage = Math.Min(TotalPages, startPage + 4);
        startPage = Math.Max(1, endPage - 4);

        for (var i = startPage; i <= endPage; i++)
        {
            PageNumbers.Add(i);
        }
    }

    private void UpdatePaginationText(int totalCount)
    {
        if (totalCount == 0)
        {
            PaginationText = "0 customers";
            return;
        }

        // For single page, just show count; for multiple pages, show range
        if (TotalPages <= 1)
        {
            PaginationText = totalCount == 1 ? "1 customer" : $"{totalCount} customers";
        }
        else
        {
            var start = (CurrentPage - 1) * PageSize + 1;
            var end = Math.Min(CurrentPage * PageSize, totalCount);
            PaginationText = $"{start}-{end} of {totalCount} customers";
        }
    }

    #endregion

    #region Add Customer

    /// <summary>
    /// Opens the Add Customer modal.
    /// </summary>
    [RelayCommand]
    private void OpenAddModal()
    {
        App.CustomerModalsViewModel?.OpenAddModal();
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
    /// Saves a new customer.
    /// </summary>
    [RelayCommand]
    private void SaveNewCustomer()
    {
        if (!ValidateModal())
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Generate new ID
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

        // Record undo action
        var customerToUndo = newCustomer;
        App.UndoRedoManager?.RecordAction(new CustomerAddAction(
            $"Add customer '{newCustomer.Name}'",
            customerToUndo,
            () =>
            {
                companyData.Customers.Remove(customerToUndo);
                companyData.MarkAsModified();
                LoadCustomers();
            },
            () =>
            {
                companyData.Customers.Add(customerToUndo);
                companyData.MarkAsModified();
                LoadCustomers();
            }));

        // Reload and close
        LoadCustomers();
        CloseAddModal();
    }

    #endregion

    #region Edit Customer

    /// <summary>
    /// Opens the Edit Customer modal.
    /// </summary>
    [RelayCommand]
    private void OpenEditModal(CustomerDisplayItem? item)
    {
        App.CustomerModalsViewModel?.OpenEditModal(item);
    }

    /// <summary>
    /// Closes the Edit modal.
    /// </summary>
    [RelayCommand]
    private void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingCustomer = null;
        ClearModalFields();
    }

    /// <summary>
    /// Saves changes to an existing customer.
    /// </summary>
    [RelayCommand]
    private void SaveEditedCustomer()
    {
        if (!ValidateModal() || _editingCustomer == null)
            return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Store old values for undo
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

        // Store new values
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

        // Update the customer
        var customerToEdit = _editingCustomer;
        customerToEdit.Name = newName;
        customerToEdit.Email = newEmail;
        customerToEdit.Phone = newPhone;
        customerToEdit.Address = newAddress;
        customerToEdit.Notes = newNotes;
        customerToEdit.Status = newStatus;
        customerToEdit.UpdatedAt = DateTime.UtcNow;

        companyData.MarkAsModified();

        // Record undo action
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
                LoadCustomers();
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
                LoadCustomers();
            }));

        // Reload and close
        LoadCustomers();
        CloseEditModal();
    }

    #endregion

    #region Delete Customer

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void OpenDeleteConfirm(CustomerDisplayItem? item)
    {
        App.CustomerModalsViewModel?.OpenDeleteConfirm(item);
    }

    /// <summary>
    /// Closes the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingCustomer = null;
    }

    /// <summary>
    /// Confirms and deletes the customer.
    /// </summary>
    [RelayCommand]
    private void ConfirmDelete()
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

            // Record undo action
            App.UndoRedoManager?.RecordAction(new CustomerDeleteAction(
                $"Delete customer '{deletedCustomer.Name}'",
                deletedCustomer,
                () =>
                {
                    companyData.Customers.Add(deletedCustomer);
                    companyData.MarkAsModified();
                    LoadCustomers();
                },
                () =>
                {
                    companyData.Customers.Remove(deletedCustomer);
                    companyData.MarkAsModified();
                    LoadCustomers();
                }));
        }

        LoadCustomers();
        CloseDeleteConfirm();
    }

    /// <summary>
    /// Gets the name of the customer being deleted (for display in confirmation).
    /// </summary>
    public string DeletingCustomerName => _deletingCustomer?.Name ?? string.Empty;

    #endregion

    #region Filter Modal

    /// <summary>
    /// Opens the filter modal.
    /// </summary>
    [RelayCommand]
    private void OpenFilterModal()
    {
        App.CustomerModalsViewModel?.OpenFilterModal();
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
        CurrentPage = 1;
        FilterCustomers();
        CloseFilterModal();
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        FilterPaymentStatus = "All";
        FilterCustomerStatus = "All";
        FilterOutstandingMin = null;
        FilterOutstandingMax = null;
        FilterLastRentalFrom = null;
        FilterLastRentalTo = null;
        SearchQuery = null;
        CurrentPage = 1;
        FilterCustomers();
        CloseFilterModal();
    }

    #endregion

    #region Customer History Modal

    /// <summary>
    /// Opens the customer history modal.
    /// </summary>
    [RelayCommand]
    private void OpenHistoryModal(CustomerDisplayItem? item)
    {
        App.CustomerModalsViewModel?.OpenHistoryModal(item);
    }

    /// <summary>
    /// Closes the history modal.
    /// </summary>
    [RelayCommand]
    private void CloseHistoryModal()
    {
        IsHistoryModalOpen = false;
        _historyCustomer = null;
        CustomerHistory.Clear();
    }

    /// <summary>
    /// Opens the history filter modal.
    /// </summary>
    [RelayCommand]
    private void OpenHistoryFilterModal()
    {
        IsHistoryFilterModalOpen = true;
    }

    /// <summary>
    /// Closes the history filter modal.
    /// </summary>
    [RelayCommand]
    private void CloseHistoryFilterModal()
    {
        IsHistoryFilterModalOpen = false;
    }

    /// <summary>
    /// Applies history filters.
    /// </summary>
    [RelayCommand]
    private void ApplyHistoryFilters()
    {
        if (_historyCustomer != null)
        {
            LoadCustomerHistory(_historyCustomer.Id);
        }
        CloseHistoryFilterModal();
    }

    /// <summary>
    /// Clears history filters.
    /// </summary>
    [RelayCommand]
    private void ClearHistoryFilters()
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

    /// <summary>
    /// Loads transaction history for a customer.
    /// </summary>
    private void LoadCustomerHistory(string customerId)
    {
        CustomerHistory.Clear();

        // TODO: In a real implementation, this would load from Sales/Rentals/etc.
        // For now, the history will be empty until transaction data is available.
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

        // Validate first name (required)
        if (string.IsNullOrWhiteSpace(ModalFirstName))
        {
            ModalFirstNameError = "First name is required.";
            isValid = false;
        }

        // Validate last name (required)
        if (string.IsNullOrWhiteSpace(ModalLastName))
        {
            ModalLastNameError = "Last name is required.";
            isValid = false;
        }

        // Validate email format if provided
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

/// <summary>
/// Display model for customers in the UI.
/// </summary>
public partial class CustomerDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _paymentStatus = "Current";

    [ObservableProperty]
    private decimal _outstanding;

    [ObservableProperty]
    private DateTime? _lastRental;

    [ObservableProperty]
    private EntityStatus _status = EntityStatus.Active;

    /// <summary>
    /// Gets the initials from the customer name for avatar display.
    /// </summary>
    public string Initials
    {
        get
        {
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
            if (parts.Length == 1 && parts[0].Length >= 2)
                return parts[0][..2].ToUpperInvariant();
            if (parts.Length == 1 && parts[0].Length == 1)
                return parts[0].ToUpperInvariant();
            return "?";
        }
    }

    /// <summary>
    /// Gets the formatted outstanding amount.
    /// </summary>
    public string OutstandingFormatted => Outstanding == 0 ? "$0.00" : $"${Outstanding:N2}";

    /// <summary>
    /// Gets the formatted last rental date.
    /// </summary>
    public string LastRentalFormatted => LastRental?.ToString("MMM d, yyyy") ?? "-";
}

/// <summary>
/// Display model for customer transaction history.
/// </summary>
public class CustomerHistoryItem
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;

    public string DateFormatted => Date.ToString("MMM d, yyyy");
    public string AmountFormatted => Amount < 0 ? $"-${Math.Abs(Amount):N2}" : $"${Amount:N2}";
}

/// <summary>
/// Undoable action for adding a customer.
/// </summary>
public class CustomerAddAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public CustomerAddAction(string description, Customer _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for editing a customer.
/// </summary>
public class CustomerEditAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public CustomerEditAction(string description, Customer _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for deleting a customer.
/// </summary>
public class CustomerDeleteAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public CustomerDeleteAction(string description, Customer _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}
