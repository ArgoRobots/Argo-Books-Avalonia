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
/// ViewModel for the Revenue page displaying sale/revenue transactions.
/// </summary>
public partial class RevenuePageViewModel : ViewModelBase
{
    #region Statistics

    [ObservableProperty]
    private string _totalMonthlyRevenue = "$0.00";

    [ObservableProperty]
    private int _salesCount;

    [ObservableProperty]
    private int _uniqueCustomers;

    [ObservableProperty]
    private int _returnsCount;

    #endregion

    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterRevenue();
    }

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string? _filterCustomerId;

    [ObservableProperty]
    private string? _filterCategoryId;

    [ObservableProperty]
    private string? _filterAmountMin;

    [ObservableProperty]
    private string? _filterAmountMax;

    [ObservableProperty]
    private DateTimeOffset? _filterDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterDateTo;

    #endregion

    #region Sorting

    [ObservableProperty]
    private string _sortColumn = "Date";

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
        FilterRevenue();
    }

    #endregion

    #region Revenue Collection

    private readonly List<Sale> _allRevenue = [];

    public ObservableCollection<RevenueDisplayItem> Revenue { get; } = [];

    public ObservableCollection<string> StatusOptions { get; } = ["All", "Completed", "Pending", "Partial Return", "Returned", "Cancelled"];

    public ObservableCollection<CustomerOption> CustomerOptions { get; } = [];

    public ObservableCollection<CategoryOption> CategoryOptions { get; } = [];

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
        FilterRevenue();
    }

    [ObservableProperty]
    private string _paginationText = "0 sales";

    public ObservableCollection<int> PageNumbers { get; } = [];

    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages;

    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        FilterRevenue();
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

    public RevenuePageViewModel()
    {
        LoadRevenue();
        LoadDropdownOptions();

        // Subscribe to undo/redo state changes to refresh UI
        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }

        // Subscribe to revenue modal events to refresh data
        if (App.RevenueModalsViewModel != null)
        {
            App.RevenueModalsViewModel.RevenueSaved += OnRevenueSaved;
            App.RevenueModalsViewModel.RevenueDeleted += OnRevenueDeleted;
            App.RevenueModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.RevenueModalsViewModel.FiltersCleared += OnFiltersCleared;
        }
    }

    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadRevenue();
    }

    private void OnRevenueSaved(object? sender, EventArgs e)
    {
        LoadRevenue();
    }

    private void OnRevenueDeleted(object? sender, EventArgs e)
    {
        LoadRevenue();
    }

    private void OnFiltersApplied(object? sender, EventArgs e)
    {
        var modals = App.RevenueModalsViewModel;
        if (modals != null)
        {
            FilterStatus = modals.FilterStatus;
            FilterCustomerId = modals.FilterCustomerId;
            FilterCategoryId = modals.FilterCategoryId;
            FilterAmountMin = modals.FilterAmountMin;
            FilterAmountMax = modals.FilterAmountMax;
            FilterDateFrom = modals.FilterDateFrom;
            FilterDateTo = modals.FilterDateTo;
        }
        CurrentPage = 1;
        FilterRevenue();
    }

    private void OnFiltersCleared(object? sender, EventArgs e)
    {
        FilterStatus = "All";
        FilterCustomerId = null;
        FilterCategoryId = null;
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterDateFrom = null;
        FilterDateTo = null;
        SearchQuery = null;
        CurrentPage = 1;
        FilterRevenue();
    }

    #endregion

    #region Data Loading

    private void LoadRevenue()
    {
        _allRevenue.Clear();
        Revenue.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Sales == null)
            return;

        _allRevenue.AddRange(companyData.Sales);
        UpdateStatistics();
        FilterRevenue();
    }

    private void LoadDropdownOptions()
    {
        CustomerOptions.Clear();
        CustomerOptions.Add(new CustomerOption { Id = null, Name = "All Customers" });

        CategoryOptions.Clear();
        CategoryOptions.Add(new CategoryOption { Id = null, Name = "All Categories" });

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Load customers
        foreach (var customer in companyData.Customers.OrderBy(c => c.Name))
        {
            CustomerOptions.Add(new CustomerOption { Id = customer.Id, Name = customer.Name });
        }

        // Load sales categories
        var salesCategories = companyData.Categories
            .Where(c => c.Type == CategoryType.Sales)
            .OrderBy(c => c.Name);

        foreach (var category in salesCategories)
        {
            CategoryOptions.Add(new CategoryOption { Id = category.Id, Name = category.Name });
        }
    }

    private void UpdateStatistics()
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        // Total monthly revenue
        var monthlyTotal = _allRevenue
            .Where(s => s.Date >= startOfMonth)
            .Sum(s => s.Total);
        TotalMonthlyRevenue = $"${monthlyTotal:N2}";

        // Sales count
        SalesCount = _allRevenue.Count;

        // Unique customers
        UniqueCustomers = _allRevenue
            .Where(s => !string.IsNullOrEmpty(s.CustomerId))
            .Select(s => s.CustomerId)
            .Distinct()
            .Count();

        // Returns count
        var companyData = App.CompanyManager?.CompanyData;
        ReturnsCount = companyData?.Returns?.Count(r =>
            _allRevenue.Any(s => s.Id == r.OriginalTransactionId)) ?? 0;
    }

    [RelayCommand]
    private void RefreshRevenue()
    {
        LoadRevenue();
        LoadDropdownOptions();
    }

    private void FilterRevenue()
    {
        Revenue.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        var filtered = _allRevenue.ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered
                .Select(s => new
                {
                    Sale = s,
                    IdScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, s.Id),
                    DescScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, s.Description),
                    CustomerScore = LevenshteinDistance.ComputeSearchScore(SearchQuery,
                        companyData?.GetCustomer(s.CustomerId ?? "")?.Name ?? "")
                })
                .Where(x => x.IdScore >= 0 || x.DescScore >= 0 || x.CustomerScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.IdScore, x.DescScore), x.CustomerScore))
                .Select(x => x.Sale)
                .ToList();
        }

        // Apply status filter
        if (FilterStatus != "All")
        {
            filtered = filtered.Where(s => GetStatusDisplay(s, companyData) == FilterStatus).ToList();
        }

        // Apply customer filter
        if (!string.IsNullOrEmpty(FilterCustomerId))
        {
            filtered = filtered.Where(s => s.CustomerId == FilterCustomerId).ToList();
        }

        // Apply category filter
        if (!string.IsNullOrEmpty(FilterCategoryId))
        {
            filtered = filtered.Where(s => s.CategoryId == FilterCategoryId).ToList();
        }

        // Apply amount filter
        if (decimal.TryParse(FilterAmountMin, out var minAmount))
        {
            filtered = filtered.Where(s => s.Total >= minAmount).ToList();
        }
        if (decimal.TryParse(FilterAmountMax, out var maxAmount))
        {
            filtered = filtered.Where(s => s.Total <= maxAmount).ToList();
        }

        // Apply date filter
        if (FilterDateFrom.HasValue)
        {
            filtered = filtered.Where(s => s.Date >= FilterDateFrom.Value.DateTime).ToList();
        }
        if (FilterDateTo.HasValue)
        {
            filtered = filtered.Where(s => s.Date <= FilterDateTo.Value.DateTime).ToList();
        }

        // Create display items
        var displayItems = filtered.Select(sale =>
        {
            var customer = companyData?.GetCustomer(sale.CustomerId ?? "");
            var category = companyData?.GetCategory(sale.CategoryId ?? "");
            var accountant = companyData?.GetAccountant(sale.AccountantId ?? "");
            var statusDisplay = GetStatusDisplay(sale, companyData);

            return new RevenueDisplayItem
            {
                Id = sale.Id,
                AccountantName = accountant?.Name ?? "System",
                CustomerName = customer?.Name ?? "-",
                ProductDescription = sale.Description,
                CategoryName = category?.Name ?? "-",
                Date = sale.Date,
                Total = sale.Total,
                StatusDisplay = statusDisplay,
                Notes = sale.Notes,
                CustomerId = sale.CustomerId,
                CategoryId = sale.CategoryId,
                Amount = sale.Amount,
                TaxAmount = sale.TaxAmount,
                PaymentMethod = sale.PaymentMethod
            };
        }).ToList();

        // Apply sorting
        if (SortDirection != SortDirection.None)
        {
            displayItems = SortColumn switch
            {
                "Id" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.Id).ToList()
                    : displayItems.OrderByDescending(r => r.Id).ToList(),
                "Accountant" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.AccountantName).ToList()
                    : displayItems.OrderByDescending(r => r.AccountantName).ToList(),
                "Customer" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.CustomerName).ToList()
                    : displayItems.OrderByDescending(r => r.CustomerName).ToList(),
                "Product" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.ProductDescription).ToList()
                    : displayItems.OrderByDescending(r => r.ProductDescription).ToList(),
                "Category" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.CategoryName).ToList()
                    : displayItems.OrderByDescending(r => r.CategoryName).ToList(),
                "Date" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.Date).ToList()
                    : displayItems.OrderByDescending(r => r.Date).ToList(),
                "Total" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.Total).ToList()
                    : displayItems.OrderByDescending(r => r.Total).ToList(),
                "Status" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.StatusDisplay).ToList()
                    : displayItems.OrderByDescending(r => r.StatusDisplay).ToList(),
                _ => displayItems.OrderByDescending(r => r.Date).ToList()
            };
        }
        else if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            displayItems = displayItems.OrderByDescending(r => r.Date).ToList();
        }

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination and add to collection
        var pagedRevenue = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedRevenue)
        {
            Revenue.Add(item);
        }
    }

    private static string GetStatusDisplay(Sale sale, Core.Data.CompanyData? companyData)
    {
        // Check for returns related to this sale
        var relatedReturn = companyData?.Returns?.FirstOrDefault(r => r.OriginalTransactionId == sale.Id);

        if (relatedReturn != null && relatedReturn.Status == Core.Enums.ReturnStatus.Completed)
        {
            return "Returned";
        }

        // Default status
        return "Completed";
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
            PaginationText = "0 sales";
            return;
        }

        if (TotalPages <= 1)
        {
            PaginationText = totalCount == 1 ? "1 sale" : $"{totalCount} sales";
        }
        else
        {
            var start = (CurrentPage - 1) * PageSize + 1;
            var end = Math.Min(CurrentPage * PageSize, totalCount);
            PaginationText = $"{start}-{end} of {totalCount} sales";
        }
    }

    #endregion

    #region Modal Commands

    [RelayCommand]
    private void OpenAddModal()
    {
        App.RevenueModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void OpenEditModal(RevenueDisplayItem? item)
    {
        App.RevenueModalsViewModel?.OpenEditModal(item);
    }

    [RelayCommand]
    private void OpenDeleteConfirm(RevenueDisplayItem? item)
    {
        App.RevenueModalsViewModel?.OpenDeleteConfirm(item);
    }

    [RelayCommand]
    private void OpenFilterModal()
    {
        App.RevenueModalsViewModel?.OpenFilterModal();
    }

    #endregion
}

/// <summary>
/// Display model for revenue/sales in the UI.
/// </summary>
public partial class RevenueDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _accountantName = string.Empty;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _productDescription = string.Empty;

    [ObservableProperty]
    private string _categoryName = string.Empty;

    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private string _statusDisplay = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string? _customerId;

    [ObservableProperty]
    private string? _categoryId;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private decimal _taxAmount;

    [ObservableProperty]
    private PaymentMethod _paymentMethod;

    public string DateFormatted => Date.ToString("MMM d, yyyy");
    public string TotalFormatted => $"${Total:N2}";

    public string CustomerInitials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CustomerName) || CustomerName == "-")
                return "?";

            var parts = CustomerName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();

            return parts.Length > 0 && parts[0].Length > 0
                ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
                : "?";
        }
    }

    public bool IsReturned => StatusDisplay == "Returned";
    public bool IsPartialReturn => StatusDisplay == "Partial Return";
}

/// <summary>
/// Undoable action for adding revenue/sale.
/// </summary>
public class RevenueAddAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public RevenueAddAction(string description, Sale _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for editing revenue/sale.
/// </summary>
public class RevenueEditAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public RevenueEditAction(string description, Sale _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for deleting revenue/sale.
/// </summary>
public class RevenueDeleteAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public RevenueDeleteAction(string description, Sale _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}
