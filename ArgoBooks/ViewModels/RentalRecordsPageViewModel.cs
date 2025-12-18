using System.Collections.ObjectModel;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Rental Records page.
/// </summary>
public partial class RentalRecordsPageViewModel : ViewModelBase
{
    #region Statistics

    [ObservableProperty]
    private int _totalRentals;

    [ObservableProperty]
    private int _activeRentals;

    [ObservableProperty]
    private int _overdueRentals;

    [ObservableProperty]
    private decimal _totalRevenue;

    public string TotalRevenueFormatted => $"${TotalRevenue:N2}";

    #endregion

    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterRecords();
    }

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string? _filterCustomer;

    [ObservableProperty]
    private string? _filterItem;

    [ObservableProperty]
    private DateTime? _filterStartDateFrom;

    [ObservableProperty]
    private DateTime? _filterStartDateTo;

    [ObservableProperty]
    private DateTime? _filterDueDateFrom;

    [ObservableProperty]
    private DateTime? _filterDueDateTo;

    #endregion

    #region Sorting

    [ObservableProperty]
    private string _sortColumn = "StartDate";

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
        FilterRecords();
    }

    #endregion

    #region Records Collection

    private readonly List<RentalRecord> _allRecords = [];

    public ObservableCollection<RentalRecordDisplayItem> Records { get; } = [];

    public ObservableCollection<string> StatusOptions { get; } = ["All", "Active", "Returned", "Overdue", "Cancelled"];

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
        FilterRecords();
    }

    [ObservableProperty]
    private string _paginationText = "0 records";

    public ObservableCollection<int> PageNumbers { get; } = [];

    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages;

    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        FilterRecords();
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

    public RentalRecordsPageViewModel()
    {
        LoadRecords();

        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }

        if (App.RentalRecordsModalsViewModel != null)
        {
            App.RentalRecordsModalsViewModel.RecordSaved += OnRecordSaved;
            App.RentalRecordsModalsViewModel.RecordDeleted += OnRecordDeleted;
            App.RentalRecordsModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.RentalRecordsModalsViewModel.FiltersCleared += OnFiltersCleared;
            App.RentalRecordsModalsViewModel.RecordReturned += OnRecordReturned;
        }

        if (App.RentalInventoryModalsViewModel != null)
        {
            App.RentalInventoryModalsViewModel.RentalCreated += OnRentalCreated;
        }
    }

    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadRecords();
    }

    private void OnRecordSaved(object? sender, EventArgs e)
    {
        LoadRecords();
    }

    private void OnRecordDeleted(object? sender, EventArgs e)
    {
        LoadRecords();
    }

    private void OnRecordReturned(object? sender, EventArgs e)
    {
        LoadRecords();
    }

    private void OnRentalCreated(object? sender, EventArgs e)
    {
        LoadRecords();
    }

    private void OnFiltersApplied(object? sender, EventArgs e)
    {
        var modals = App.RentalRecordsModalsViewModel;
        if (modals != null)
        {
            FilterStatus = modals.FilterStatus;
            FilterCustomer = modals.FilterCustomer;
            FilterItem = modals.FilterItem;
            FilterStartDateFrom = modals.FilterStartDateFrom;
            FilterStartDateTo = modals.FilterStartDateTo;
            FilterDueDateFrom = modals.FilterDueDateFrom;
            FilterDueDateTo = modals.FilterDueDateTo;
        }
        CurrentPage = 1;
        FilterRecords();
    }

    private void OnFiltersCleared(object? sender, EventArgs e)
    {
        FilterStatus = "All";
        FilterCustomer = null;
        FilterItem = null;
        FilterStartDateFrom = null;
        FilterStartDateTo = null;
        FilterDueDateFrom = null;
        FilterDueDateTo = null;
        SearchQuery = null;
        CurrentPage = 1;
        FilterRecords();
    }

    #endregion

    #region Data Loading

    private void LoadRecords()
    {
        _allRecords.Clear();
        Records.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Rentals == null)
            return;

        // Update overdue status for active rentals
        foreach (var rental in companyData.Rentals.Where(r => r.Status == RentalStatus.Active))
        {
            if (rental.IsOverdue)
            {
                rental.Status = RentalStatus.Overdue;
            }
        }

        _allRecords.AddRange(companyData.Rentals);
        UpdateStatistics();
        FilterRecords();
    }

    private void UpdateStatistics()
    {
        TotalRentals = _allRecords.Count;
        ActiveRentals = _allRecords.Count(r => r.Status == RentalStatus.Active);
        OverdueRentals = _allRecords.Count(r => r.Status == RentalStatus.Overdue);
        TotalRevenue = _allRecords.Where(r => r.Status == RentalStatus.Returned).Sum(r => r.TotalCost ?? 0);
        OnPropertyChanged(nameof(TotalRevenueFormatted));
    }

    [RelayCommand]
    private void RefreshRecords()
    {
        LoadRecords();
    }

    private void FilterRecords()
    {
        Records.Clear();

        var filtered = _allRecords.ToList();
        var companyData = App.CompanyManager?.CompanyData;

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered
                .Select(r =>
                {
                    var item = companyData?.RentalInventory.FirstOrDefault(i => i.Id == r.RentalItemId);
                    var customer = companyData?.Customers.FirstOrDefault(c => c.Id == r.CustomerId);
                    return new
                    {
                        Record = r,
                        IdScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, r.Id),
                        ItemScore = item != null ? LevenshteinDistance.ComputeSearchScore(SearchQuery, item.Name) : -1,
                        CustomerScore = customer != null ? LevenshteinDistance.ComputeSearchScore(SearchQuery, customer.Name) : -1
                    };
                })
                .Where(x => x.IdScore >= 0 || x.ItemScore >= 0 || x.CustomerScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.IdScore, x.ItemScore), x.CustomerScore))
                .Select(x => x.Record)
                .ToList();
        }

        // Apply status filter
        if (FilterStatus != "All")
        {
            var statusEnum = FilterStatus switch
            {
                "Active" => RentalStatus.Active,
                "Returned" => RentalStatus.Returned,
                "Overdue" => RentalStatus.Overdue,
                "Cancelled" => RentalStatus.Cancelled,
                _ => (RentalStatus?)null
            };
            if (statusEnum.HasValue)
            {
                filtered = filtered.Where(r => r.Status == statusEnum.Value).ToList();
            }
        }

        // Apply customer filter
        if (!string.IsNullOrWhiteSpace(FilterCustomer) && FilterCustomer != "All Customers")
        {
            var customer = companyData?.Customers.FirstOrDefault(c => c.Name == FilterCustomer);
            if (customer != null)
            {
                filtered = filtered.Where(r => r.CustomerId == customer.Id).ToList();
            }
        }

        // Apply item filter
        if (!string.IsNullOrWhiteSpace(FilterItem) && FilterItem != "All Items")
        {
            var item = companyData?.RentalInventory.FirstOrDefault(i => i.Name == FilterItem);
            if (item != null)
            {
                filtered = filtered.Where(r => r.RentalItemId == item.Id).ToList();
            }
        }

        // Apply date filters
        if (FilterStartDateFrom.HasValue)
        {
            filtered = filtered.Where(r => r.StartDate >= FilterStartDateFrom.Value).ToList();
        }
        if (FilterStartDateTo.HasValue)
        {
            filtered = filtered.Where(r => r.StartDate <= FilterStartDateTo.Value).ToList();
        }
        if (FilterDueDateFrom.HasValue)
        {
            filtered = filtered.Where(r => r.DueDate >= FilterDueDateFrom.Value).ToList();
        }
        if (FilterDueDateTo.HasValue)
        {
            filtered = filtered.Where(r => r.DueDate <= FilterDueDateTo.Value).ToList();
        }

        // Create display items
        var displayItems = filtered.Select(record =>
        {
            var item = companyData?.RentalInventory.FirstOrDefault(i => i.Id == record.RentalItemId);
            var customer = companyData?.Customers.FirstOrDefault(c => c.Id == record.CustomerId);

            return new RentalRecordDisplayItem
            {
                Id = record.Id,
                ItemName = item?.Name ?? "Unknown Item",
                ItemId = record.RentalItemId,
                CustomerName = customer?.Name ?? "Unknown Customer",
                CustomerId = record.CustomerId,
                Quantity = record.Quantity,
                RateType = record.RateType.ToString(),
                RateAmount = record.RateAmount,
                SecurityDeposit = record.SecurityDeposit,
                StartDate = record.StartDate,
                DueDate = record.DueDate,
                ReturnDate = record.ReturnDate,
                Status = record.Status.ToString(),
                TotalCost = record.TotalCost ?? 0,
                DaysOverdue = record.DaysOverdue,
                IsActive = record.Status == RentalStatus.Active || record.Status == RentalStatus.Overdue
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
                "Item" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.ItemName).ToList()
                    : displayItems.OrderByDescending(r => r.ItemName).ToList(),
                "Customer" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.CustomerName).ToList()
                    : displayItems.OrderByDescending(r => r.CustomerName).ToList(),
                "Quantity" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.Quantity).ToList()
                    : displayItems.OrderByDescending(r => r.Quantity).ToList(),
                "Rate" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.RateAmount).ToList()
                    : displayItems.OrderByDescending(r => r.RateAmount).ToList(),
                "StartDate" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.StartDate).ToList()
                    : displayItems.OrderByDescending(r => r.StartDate).ToList(),
                "DueDate" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.DueDate).ToList()
                    : displayItems.OrderByDescending(r => r.DueDate).ToList(),
                "Status" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.Status).ToList()
                    : displayItems.OrderByDescending(r => r.Status).ToList(),
                "Total" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(r => r.TotalCost).ToList()
                    : displayItems.OrderByDescending(r => r.TotalCost).ToList(),
                _ => displayItems.OrderByDescending(r => r.StartDate).ToList()
            };
        }
        else if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            displayItems = displayItems.OrderByDescending(r => r.StartDate).ToList();
        }

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination
        var pagedRecords = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var record in pagedRecords)
        {
            Records.Add(record);
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
            PaginationText = "0 records";
            return;
        }

        if (TotalPages <= 1)
        {
            PaginationText = totalCount == 1 ? "1 record" : $"{totalCount} records";
        }
        else
        {
            var start = (CurrentPage - 1) * PageSize + 1;
            var end = Math.Min(CurrentPage * PageSize, totalCount);
            PaginationText = $"{start}-{end} of {totalCount} records";
        }
    }

    #endregion

    #region Modal Commands

    [RelayCommand]
    private void OpenAddModal()
    {
        App.RentalRecordsModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void OpenEditModal(RentalRecordDisplayItem? record)
    {
        App.RentalRecordsModalsViewModel?.OpenEditModal(record);
    }

    [RelayCommand]
    private void OpenDeleteConfirm(RentalRecordDisplayItem? record)
    {
        App.RentalRecordsModalsViewModel?.OpenDeleteConfirm(record);
    }

    [RelayCommand]
    private void OpenFilterModal()
    {
        App.RentalRecordsModalsViewModel?.OpenFilterModal();
    }

    [RelayCommand]
    private void OpenReturnModal(RentalRecordDisplayItem? record)
    {
        App.RentalRecordsModalsViewModel?.OpenReturnModal(record);
    }

    [RelayCommand]
    private void OpenViewModal(RentalRecordDisplayItem? record)
    {
        App.RentalRecordsModalsViewModel?.OpenViewModal(record);
    }

    #endregion
}

/// <summary>
/// Display model for rental records in the UI.
/// </summary>
public partial class RentalRecordDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private string _itemId = string.Empty;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _customerId = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private string _rateType = "Daily";

    [ObservableProperty]
    private decimal _rateAmount;

    [ObservableProperty]
    private decimal _securityDeposit;

    [ObservableProperty]
    private DateTime _startDate;

    [ObservableProperty]
    private DateTime _dueDate;

    [ObservableProperty]
    private DateTime? _returnDate;

    [ObservableProperty]
    private string _status = "Active";

    [ObservableProperty]
    private decimal _totalCost;

    [ObservableProperty]
    private int _daysOverdue;

    [ObservableProperty]
    private bool _isActive;

    public string StartDateFormatted => StartDate.ToString("MMM d, yyyy");
    public string DueDateFormatted => DueDate.ToString("MMM d, yyyy");
    public string ReturnDateFormatted => ReturnDate?.ToString("MMM d, yyyy") ?? "-";
    public string RateFormatted => $"${RateAmount:N2}/{RateType}";
    public string TotalCostFormatted => $"${TotalCost:N2}";
    public string DepositFormatted => $"${SecurityDeposit:N2}";
    public string DaysOverdueText => DaysOverdue > 0 ? $"{DaysOverdue} days" : "-";
}

/// <summary>
/// Undoable action for editing a rental record.
/// </summary>
public class RentalRecordEditAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public RentalRecordEditAction(string description, RentalRecord _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for deleting a rental record.
/// </summary>
public class RentalRecordDeleteAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public RentalRecordDeleteAction(string description, RentalRecord _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for returning a rental.
/// </summary>
public class RentalReturnAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public RentalReturnAction(string description, RentalRecord _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}
