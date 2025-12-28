using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Invoices page.
/// </summary>
public partial class InvoicesPageViewModel : ViewModelBase
{
    #region Statistics

    [ObservableProperty]
    private string _totalOutstanding = "$0.00";

    [ObservableProperty]
    private string _paidThisMonth = "$0.00";

    [ObservableProperty]
    private string _overdueAmount = "$0.00";

    [ObservableProperty]
    private int _dueThisWeekCount;

    #endregion

    #region Tab Navigation

    [ObservableProperty]
    private string _selectedTab = "All";

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                var tab = value switch
                {
                    0 => "All",
                    1 => "Drafts",
                    2 => "Recurring",
                    _ => "All"
                };
                SelectTabInternal(tab);
            }
        }
    }

    [ObservableProperty]
    private bool _isAllInvoicesTab = true;

    [ObservableProperty]
    private bool _isDraftsTab;

    [ObservableProperty]
    private bool _isRecurringTab;

    [ObservableProperty]
    private string _emptyStateTitle = "No invoices found";

    [ObservableProperty]
    private string _emptyStateMessage = "Create your first invoice to start tracking your billing.";

    [RelayCommand]
    private void SelectTab(string tab)
    {
        SelectTabInternal(tab);
    }

    private void SelectTabInternal(string tab)
    {
        SelectedTab = tab;
        IsAllInvoicesTab = tab == "All";
        IsDraftsTab = tab == "Drafts";
        IsRecurringTab = tab == "Recurring";

        // Update empty state messages based on tab
        EmptyStateTitle = tab switch
        {
            "Drafts" => "No Draft Invoices",
            "Recurring" => "No Recurring Invoices",
            _ => "No invoices found"
        };

        EmptyStateMessage = tab switch
        {
            "Drafts" => "Draft invoices you're working on will appear here.",
            "Recurring" => "Set up recurring invoices to automate your billing.",
            _ => "Create your first invoice to start tracking your billing."
        };

        CurrentPage = 1;
        FilterInvoices();
    }

    #endregion

    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterInvoices();
    }

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string? _filterCustomerId;

    [ObservableProperty]
    private string? _filterAmountMin;

    [ObservableProperty]
    private string? _filterAmountMax;

    [ObservableProperty]
    private DateTimeOffset? _filterIssueDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterIssueDateTo;

    [ObservableProperty]
    private DateTimeOffset? _filterDueDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterDueDateTo;

    #endregion

    #region Column Visibility and Widths

    [ObservableProperty]
    private bool _isColumnMenuOpen;

    /// <summary>
    /// Column widths manager for the table.
    /// </summary>
    public InvoicesTableColumnWidths ColumnWidths { get; } = new InvoicesTableColumnWidths();

    [ObservableProperty]
    private bool _showIdColumn = true;

    [ObservableProperty]
    private bool _showCustomerColumn = true;

    [ObservableProperty]
    private bool _showIssueDateColumn = true;

    [ObservableProperty]
    private bool _showDueDateColumn = true;

    [ObservableProperty]
    private bool _showAmountColumn = true;

    [ObservableProperty]
    private bool _showStatusColumn = true;

    partial void OnShowIdColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Id", value);
    partial void OnShowCustomerColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Customer", value);
    partial void OnShowIssueDateColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("IssueDate", value);
    partial void OnShowDueDateColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("DueDate", value);
    partial void OnShowAmountColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Amount", value);
    partial void OnShowStatusColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Status", value);

    [RelayCommand]
    private void ToggleColumnMenu()
    {
        IsColumnMenuOpen = !IsColumnMenuOpen;
    }

    [RelayCommand]
    private void CloseColumnMenu()
    {
        IsColumnMenuOpen = false;
    }

    #endregion

    #region Sorting

    [ObservableProperty]
    private string _sortColumn = "IssueDate";

    [ObservableProperty]
    private SortDirection _sortDirection = SortDirection.Descending;

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
        FilterInvoices();
    }

    #endregion

    #region Invoices Collection

    private readonly List<Invoice> _allInvoices = [];

    public ObservableCollection<InvoiceDisplayItem> Invoices { get; } = [];

    public ObservableCollection<string> StatusOptions { get; } = ["All", "Draft", "Pending", "Sent", "Partial", "Paid", "Overdue", "Cancelled"];

    public ObservableCollection<CustomerOption> CustomerOptions { get; } = [];

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
        FilterInvoices();
    }

    [ObservableProperty]
    private string _paginationText = "0 invoices";

    public ObservableCollection<int> PageNumbers { get; } = [];

    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages;

    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        FilterInvoices();
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

    #region Constructor

    public InvoicesPageViewModel()
    {
        LoadInvoices();
        LoadCustomerOptions();

        // Subscribe to undo/redo state changes to refresh UI
        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }

        // Subscribe to invoice modal events to refresh data
        if (App.InvoiceModalsViewModel != null)
        {
            App.InvoiceModalsViewModel.InvoiceSaved += OnInvoiceSaved;
            App.InvoiceModalsViewModel.InvoiceDeleted += OnInvoiceDeleted;
            App.InvoiceModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.InvoiceModalsViewModel.FiltersCleared += OnFiltersCleared;
        }
    }

    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadInvoices();
    }

    private void OnInvoiceSaved(object? sender, EventArgs e)
    {
        LoadInvoices();
    }

    private void OnInvoiceDeleted(object? sender, EventArgs e)
    {
        LoadInvoices();
    }

    private void OnFiltersApplied(object? sender, EventArgs e)
    {
        var modals = App.InvoiceModalsViewModel;
        if (modals != null)
        {
            FilterStatus = modals.FilterStatus;
            FilterCustomerId = modals.FilterCustomerId;
            FilterAmountMin = modals.FilterAmountMin;
            FilterAmountMax = modals.FilterAmountMax;
            FilterIssueDateFrom = modals.FilterIssueDateFrom;
            FilterIssueDateTo = modals.FilterIssueDateTo;
            FilterDueDateFrom = modals.FilterDueDateFrom;
            FilterDueDateTo = modals.FilterDueDateTo;
        }
        CurrentPage = 1;
        FilterInvoices();
    }

    private void OnFiltersCleared(object? sender, EventArgs e)
    {
        FilterStatus = "All";
        FilterCustomerId = null;
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterIssueDateFrom = null;
        FilterIssueDateTo = null;
        FilterDueDateFrom = null;
        FilterDueDateTo = null;
        SearchQuery = null;
        CurrentPage = 1;
        FilterInvoices();
    }

    #endregion

    #region Data Loading

    private void LoadInvoices()
    {
        _allInvoices.Clear();
        Invoices.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Invoices == null)
            return;

        _allInvoices.AddRange(companyData.Invoices);
        UpdateStatistics();
        FilterInvoices();
    }

    private void LoadCustomerOptions()
    {
        CustomerOptions.Clear();
        CustomerOptions.Add(new CustomerOption { Id = string.Empty, Name = "All Customers" });

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Customers == null)
            return;

        foreach (var customer in companyData.Customers.OrderBy(c => c.Name))
        {
            CustomerOptions.Add(new CustomerOption { Id = customer.Id, Name = customer.Name });
        }
    }

    private void UpdateStatistics()
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfWeek = now.AddDays(7);

        // Total outstanding (unpaid invoices)
        var outstanding = _allInvoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .Sum(i => i.Balance);
        TotalOutstanding = $"${outstanding:N2}";

        // Paid this month
        var paidThisMonth = _allInvoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.UpdatedAt >= startOfMonth)
            .Sum(i => i.Total);
        PaidThisMonth = $"${paidThisMonth:N2}";

        // Overdue amount
        var overdue = _allInvoices
            .Where(i => i.IsOverdue || i.Status == InvoiceStatus.Overdue)
            .Sum(i => i.Balance);
        OverdueAmount = $"${overdue:N2}";

        // Due this week
        DueThisWeekCount = _allInvoices
            .Count(i => i.DueDate >= now.Date && i.DueDate <= endOfWeek &&
                       i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled);
    }

    [RelayCommand]
    private void RefreshInvoices()
    {
        LoadInvoices();
        LoadCustomerOptions();
    }

    private void FilterInvoices()
    {
        Invoices.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        var filtered = _allInvoices.ToList();

        // Apply tab filter
        filtered = SelectedTab switch
        {
            "Drafts" => filtered.Where(i => i.Status == InvoiceStatus.Draft).ToList(),
            "Recurring" => filtered.Where(i => !string.IsNullOrEmpty(i.RecurringInvoiceId)).ToList(),
            _ => filtered
        };

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered
                .Select(i => new
                {
                    Invoice = i,
                    IdScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, i.Id),
                    CustomerScore = LevenshteinDistance.ComputeSearchScore(SearchQuery,
                        companyData?.GetCustomer(i.CustomerId)?.Name ?? ""),
                    NumberScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, i.InvoiceNumber)
                })
                .Where(x => x.IdScore >= 0 || x.CustomerScore >= 0 || x.NumberScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.IdScore, x.CustomerScore), x.NumberScore))
                .Select(x => x.Invoice)
                .ToList();
        }

        // Apply status filter
        if (FilterStatus != "All")
        {
            var status = Enum.Parse<InvoiceStatus>(FilterStatus);
            filtered = filtered.Where(i => i.Status == status ||
                (FilterStatus == "Overdue" && i.IsOverdue)).ToList();
        }

        // Apply customer filter
        if (!string.IsNullOrEmpty(FilterCustomerId))
        {
            filtered = filtered.Where(i => i.CustomerId == FilterCustomerId).ToList();
        }

        // Apply amount filter
        if (decimal.TryParse(FilterAmountMin, out var minAmount))
        {
            filtered = filtered.Where(i => i.Total >= minAmount).ToList();
        }
        if (decimal.TryParse(FilterAmountMax, out var maxAmount))
        {
            filtered = filtered.Where(i => i.Total <= maxAmount).ToList();
        }

        // Apply issue date filter
        if (FilterIssueDateFrom.HasValue)
        {
            filtered = filtered.Where(i => i.IssueDate >= FilterIssueDateFrom.Value.DateTime).ToList();
        }
        if (FilterIssueDateTo.HasValue)
        {
            filtered = filtered.Where(i => i.IssueDate <= FilterIssueDateTo.Value.DateTime).ToList();
        }

        // Apply due date filter
        if (FilterDueDateFrom.HasValue)
        {
            filtered = filtered.Where(i => i.DueDate >= FilterDueDateFrom.Value.DateTime).ToList();
        }
        if (FilterDueDateTo.HasValue)
        {
            filtered = filtered.Where(i => i.DueDate <= FilterDueDateTo.Value.DateTime).ToList();
        }

        // Create display items
        var displayItems = filtered.Select(invoice =>
        {
            var customer = companyData?.GetCustomer(invoice.CustomerId);
            var statusDisplay = GetStatusDisplay(invoice);

            return new InvoiceDisplayItem
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerId = invoice.CustomerId,
                CustomerName = customer?.Name ?? "Unknown Customer",
                CustomerInitials = GetInitials(customer?.Name ?? "?"),
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                Subtotal = invoice.Subtotal,
                TaxAmount = invoice.TaxAmount,
                Total = invoice.Total,
                AmountPaid = invoice.AmountPaid,
                Balance = invoice.Balance,
                Status = invoice.Status,
                StatusDisplay = statusDisplay,
                Notes = invoice.Notes,
                IsRecurring = !string.IsNullOrEmpty(invoice.RecurringInvoiceId)
            };
        }).ToList();

        // Apply sorting
        if (SortDirection != SortDirection.None)
        {
            displayItems = SortColumn switch
            {
                "Id" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.Id).ToList()
                    : displayItems.OrderByDescending(i => i.Id).ToList(),
                "Customer" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.CustomerName).ToList()
                    : displayItems.OrderByDescending(i => i.CustomerName).ToList(),
                "IssueDate" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.IssueDate).ToList()
                    : displayItems.OrderByDescending(i => i.IssueDate).ToList(),
                "DueDate" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.DueDate).ToList()
                    : displayItems.OrderByDescending(i => i.DueDate).ToList(),
                "Amount" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.Total).ToList()
                    : displayItems.OrderByDescending(i => i.Total).ToList(),
                "Status" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.StatusDisplay).ToList()
                    : displayItems.OrderByDescending(i => i.StatusDisplay).ToList(),
                _ => displayItems.OrderByDescending(i => i.IssueDate).ToList()
            };
        }
        else if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            displayItems = displayItems.OrderByDescending(i => i.IssueDate).ToList();
        }

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination and add to collection
        var pagedInvoices = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedInvoices)
        {
            Invoices.Add(item);
        }
    }

    private static string GetStatusDisplay(Invoice invoice)
    {
        if (invoice.IsOverdue && invoice.Status != InvoiceStatus.Paid && invoice.Status != InvoiceStatus.Cancelled)
            return "Overdue";

        return invoice.Status switch
        {
            InvoiceStatus.Draft => "Draft",
            InvoiceStatus.Pending => "Pending",
            InvoiceStatus.Sent => "Sent",
            InvoiceStatus.Viewed => "Viewed",
            InvoiceStatus.Partial => "Partial",
            InvoiceStatus.Paid => "Paid",
            InvoiceStatus.Overdue => "Overdue",
            InvoiceStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "?";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        if (parts is [{ Length: >= 1 }])
            return parts[0][0].ToString().ToUpper();
        return "?";
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
            PaginationText = "0 invoices";
            return;
        }

        if (TotalPages <= 1)
        {
            PaginationText = totalCount == 1 ? "1 invoice" : $"{totalCount} invoices";
        }
        else
        {
            var start = (CurrentPage - 1) * PageSize + 1;
            var end = Math.Min(CurrentPage * PageSize, totalCount);
            PaginationText = $"{start}-{end} of {totalCount} invoices";
        }
    }

    #endregion

    #region Modal Commands

    [RelayCommand]
    private void OpenCreateModal()
    {
        App.InvoiceModalsViewModel?.OpenCreateModal();
    }

    [RelayCommand]
    private void OpenEditModal(InvoiceDisplayItem? item)
    {
        App.InvoiceModalsViewModel?.OpenEditModal(item);
    }

    [RelayCommand]
    private void OpenDeleteConfirm(InvoiceDisplayItem? item)
    {
        App.InvoiceModalsViewModel?.OpenDeleteConfirm(item);
    }

    [RelayCommand]
    private void OpenFilterModal()
    {
        App.InvoiceModalsViewModel?.OpenFilterModal();
    }

    [RelayCommand]
    private void OpenHistoryModal(InvoiceDisplayItem? item)
    {
        App.InvoiceModalsViewModel?.OpenHistoryModal(item);
    }

    #endregion
}

/// <summary>
/// Display model for invoices in the UI.
/// </summary>
public partial class InvoiceDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

    [ObservableProperty]
    private string _customerId = string.Empty;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _customerInitials = string.Empty;

    [ObservableProperty]
    private DateTime _issueDate;

    [ObservableProperty]
    private DateTime _dueDate;

    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _taxAmount;

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private decimal _amountPaid;

    [ObservableProperty]
    private decimal _balance;

    [ObservableProperty]
    private InvoiceStatus _status;

    [ObservableProperty]
    private string _statusDisplay = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private bool _isRecurring;

    public string IssueDateFormatted => IssueDate.ToString("MMM d, yyyy");
    public string DueDateFormatted => DueDate.ToString("MMM d, yyyy");
    public string TotalFormatted => $"${Total:N2}";
    public string BalanceFormatted => $"${Balance:N2}";
}

/// <summary>
/// Customer option for dropdown.
/// </summary>
public class CustomerOption
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}

/// <summary>
/// Undoable action for adding an invoice.
/// </summary>
public class InvoiceAddAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public InvoiceAddAction(string description, Invoice _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for editing an invoice.
/// </summary>
public class InvoiceEditAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public InvoiceEditAction(string description, Invoice _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for deleting an invoice.
/// </summary>
public class InvoiceDeleteAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public InvoiceDeleteAction(string description, Invoice _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}
