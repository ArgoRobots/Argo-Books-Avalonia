using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Rental Inventory page.
/// </summary>
public partial class RentalInventoryPageViewModel : ViewModelBase
{
    #region Statistics

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private int _availableItems;

    [ObservableProperty]
    private int _rentedOutItems;

    [ObservableProperty]
    private int _maintenanceItems;

    #endregion

    #region Column Visibility and Widths

    [ObservableProperty]
    private bool _isColumnMenuOpen;

    /// <summary>
    /// Column widths manager for the table.
    /// </summary>
    public RentalInventoryTableColumnWidths ColumnWidths { get; } = new RentalInventoryTableColumnWidths();

    [ObservableProperty]
    private bool _showItemColumn = true;

    [ObservableProperty]
    private bool _showSupplierColumn = true;

    [ObservableProperty]
    private bool _showStatusColumn = true;

    [ObservableProperty]
    private bool _showTotalQtyColumn = true;

    [ObservableProperty]
    private bool _showAvailableColumn = true;

    [ObservableProperty]
    private bool _showRentedColumn = true;

    [ObservableProperty]
    private bool _showDailyRateColumn = true;

    [ObservableProperty]
    private bool _showWeeklyRateColumn = true;

    [ObservableProperty]
    private bool _showDepositColumn = true;

    partial void OnShowItemColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Item", value);
    partial void OnShowSupplierColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Supplier", value);
    partial void OnShowStatusColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Status", value);
    partial void OnShowTotalQtyColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("TotalQty", value);
    partial void OnShowAvailableColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Available", value);
    partial void OnShowRentedColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Rented", value);
    partial void OnShowDailyRateColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("DailyRate", value);
    partial void OnShowWeeklyRateColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("WeeklyRate", value);
    partial void OnShowDepositColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Deposit", value);

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

    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterItems();
    }

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string? _filterSupplier;

    [ObservableProperty]
    private string? _filterDailyRateMin;

    [ObservableProperty]
    private string? _filterDailyRateMax;

    [ObservableProperty]
    private string _filterAvailability = "All";

    #endregion

    #region Sorting

    [ObservableProperty]
    private string _sortColumn = "Name";

    [ObservableProperty]
    private SortDirection _sortDirection = SortDirection.None;

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
        FilterItems();
    }

    #endregion

    #region Items Collection

    private readonly List<RentalItem> _allItems = [];

    public ObservableCollection<RentalItemDisplayItem> Items { get; } = [];

    public ObservableCollection<string> StatusOptions { get; } = ["All", "Available", "In Maintenance", "All Rented"];

    public ObservableCollection<string> AvailabilityOptions { get; } = ["All", "Available Only", "Unavailable Only"];

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
        FilterItems();
    }

    [ObservableProperty]
    private string _paginationText = "0 items";

    public ObservableCollection<int> PageNumbers { get; } = [];

    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages;

    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        FilterItems();
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

    public RentalInventoryPageViewModel()
    {
        LoadItems();

        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }

        if (App.RentalInventoryModalsViewModel != null)
        {
            App.RentalInventoryModalsViewModel.ItemSaved += OnItemSaved;
            App.RentalInventoryModalsViewModel.ItemDeleted += OnItemDeleted;
            App.RentalInventoryModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.RentalInventoryModalsViewModel.FiltersCleared += OnFiltersCleared;
        }
    }

    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadItems();
    }

    private void OnItemSaved(object? sender, EventArgs e)
    {
        LoadItems();
    }

    private void OnItemDeleted(object? sender, EventArgs e)
    {
        LoadItems();
    }

    private void OnFiltersApplied(object? sender, EventArgs e)
    {
        var modals = App.RentalInventoryModalsViewModel;
        if (modals != null)
        {
            FilterStatus = modals.FilterStatus;
            FilterSupplier = modals.FilterSupplier;
            FilterDailyRateMin = modals.FilterDailyRateMin;
            FilterDailyRateMax = modals.FilterDailyRateMax;
            FilterAvailability = modals.FilterAvailability;
        }
        CurrentPage = 1;
        FilterItems();
    }

    private void OnFiltersCleared(object? sender, EventArgs e)
    {
        FilterStatus = "All";
        FilterSupplier = null;
        FilterDailyRateMin = null;
        FilterDailyRateMax = null;
        FilterAvailability = "All";
        SearchQuery = null;
        CurrentPage = 1;
        FilterItems();
    }

    #endregion

    #region Data Loading

    private void LoadItems()
    {
        _allItems.Clear();
        Items.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.RentalInventory == null)
            return;

        _allItems.AddRange(companyData.RentalInventory);
        UpdateStatistics();
        FilterItems();
    }

    private void UpdateStatistics()
    {
        TotalItems = _allItems.Sum(i => i.TotalQuantity);
        AvailableItems = _allItems.Sum(i => i.AvailableQuantity);
        RentedOutItems = _allItems.Sum(i => i.RentedQuantity);
        MaintenanceItems = _allItems.Where(i => i.Status == EntityStatus.Inactive).Sum(i => i.TotalQuantity);
    }

    [RelayCommand]
    private void RefreshItems()
    {
        LoadItems();
    }

    private void FilterItems()
    {
        Items.Clear();

        var filtered = _allItems.ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered
                .Select(i => new
                {
                    Item = i,
                    NameScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, i.Name),
                    IdScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, i.Id)
                })
                .Where(x => x.NameScore >= 0 || x.IdScore >= 0)
                .OrderByDescending(x => Math.Max(x.NameScore, x.IdScore))
                .Select(x => x.Item)
                .ToList();
        }

        // Apply status filter
        if (FilterStatus != "All")
        {
            filtered = FilterStatus switch
            {
                "Available" => filtered.Where(i => i is { AvailableQuantity: > 0, Status: EntityStatus.Active }).ToList(),
                "In Maintenance" => filtered.Where(i => i.Status == EntityStatus.Inactive).ToList(),
                "All Rented" => filtered.Where(i => i is { AvailableQuantity: 0, Status: EntityStatus.Active }).ToList(),
                _ => filtered
            };
        }

        // Apply availability filter
        if (FilterAvailability != "All")
        {
            filtered = FilterAvailability switch
            {
                "Available Only" => filtered.Where(i => i.IsAvailable).ToList(),
                "Unavailable Only" => filtered.Where(i => !i.IsAvailable).ToList(),
                _ => filtered
            };
        }

        // Apply supplier filter
        if (!string.IsNullOrWhiteSpace(FilterSupplier) && FilterSupplier != "All Suppliers")
        {
            var companyData = App.CompanyManager?.CompanyData;
            var supplier = companyData?.Suppliers.FirstOrDefault(s => s.Name == FilterSupplier);
            if (supplier != null)
            {
                filtered = filtered.Where(i => i.SupplierId == supplier.Id).ToList();
            }
        }

        // Apply daily rate filter
        if (decimal.TryParse(FilterDailyRateMin, out var minRate))
        {
            filtered = filtered.Where(i => i.DailyRate >= minRate).ToList();
        }
        if (decimal.TryParse(FilterDailyRateMax, out var maxRate))
        {
            filtered = filtered.Where(i => i.DailyRate <= maxRate).ToList();
        }

        // Create display items
        var companyDataForDisplay = App.CompanyManager?.CompanyData;
        var displayItems = filtered.Select(item =>
        {
            var supplier = companyDataForDisplay?.Suppliers.FirstOrDefault(s => s.Id == item.SupplierId);
            var status = item.Status == EntityStatus.Inactive ? "In Maintenance" :
                         item.AvailableQuantity == 0 ? "All Rented" : "Available";

            return new RentalItemDisplayItem
            {
                Id = item.Id,
                Name = item.Name,
                SupplierName = supplier?.Name ?? "-",
                Status = status,
                TotalQuantity = item.TotalQuantity,
                AvailableQuantity = item.AvailableQuantity,
                RentedQuantity = item.RentedQuantity,
                DailyRate = item.DailyRate,
                WeeklyRate = item.WeeklyRate,
                MonthlyRate = item.MonthlyRate,
                SecurityDeposit = item.SecurityDeposit,
                IsAvailable = item.IsAvailable
            };
        }).ToList();

        // Apply sorting
        if (SortDirection != SortDirection.None)
        {
            displayItems = SortColumn switch
            {
                "Name" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.Name).ToList()
                    : displayItems.OrderByDescending(i => i.Name).ToList(),
                "Supplier" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.SupplierName).ToList()
                    : displayItems.OrderByDescending(i => i.SupplierName).ToList(),
                "Status" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.Status).ToList()
                    : displayItems.OrderByDescending(i => i.Status).ToList(),
                "TotalQty" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.TotalQuantity).ToList()
                    : displayItems.OrderByDescending(i => i.TotalQuantity).ToList(),
                "Available" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.AvailableQuantity).ToList()
                    : displayItems.OrderByDescending(i => i.AvailableQuantity).ToList(),
                "Rented" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.RentedQuantity).ToList()
                    : displayItems.OrderByDescending(i => i.RentedQuantity).ToList(),
                "DailyRate" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.DailyRate).ToList()
                    : displayItems.OrderByDescending(i => i.DailyRate).ToList(),
                "WeeklyRate" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.WeeklyRate).ToList()
                    : displayItems.OrderByDescending(i => i.WeeklyRate).ToList(),
                "Deposit" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(i => i.SecurityDeposit).ToList()
                    : displayItems.OrderByDescending(i => i.SecurityDeposit).ToList(),
                _ => displayItems.OrderBy(i => i.Name).ToList()
            };
        }
        else if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            displayItems = displayItems.OrderBy(i => i.Name).ToList();
        }

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination
        var pagedItems = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedItems)
        {
            Items.Add(item);
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
            PaginationText = "0 items";
            return;
        }

        if (TotalPages <= 1)
        {
            PaginationText = totalCount == 1 ? "1 item" : $"{totalCount} items";
        }
        else
        {
            var start = (CurrentPage - 1) * PageSize + 1;
            var end = Math.Min(CurrentPage * PageSize, totalCount);
            PaginationText = $"{start}-{end} of {totalCount} items";
        }
    }

    #endregion

    #region Modal Commands

    [RelayCommand]
    private void OpenAddModal()
    {
        App.RentalInventoryModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void OpenEditModal(RentalItemDisplayItem? item)
    {
        App.RentalInventoryModalsViewModel?.OpenEditModal(item);
    }

    [RelayCommand]
    private void OpenDeleteConfirm(RentalItemDisplayItem? item)
    {
        App.RentalInventoryModalsViewModel?.OpenDeleteConfirm(item);
    }

    [RelayCommand]
    private void OpenFilterModal()
    {
        App.RentalInventoryModalsViewModel?.OpenFilterModal();
    }

    [RelayCommand]
    private void OpenRentOutModal(RentalItemDisplayItem? item)
    {
        App.RentalInventoryModalsViewModel?.OpenRentOutModal(item);
    }

    #endregion
}

/// <summary>
/// Display model for rental items in the UI.
/// </summary>
public partial class RentalItemDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _supplierName = string.Empty;

    [ObservableProperty]
    private string _status = "Available";

    [ObservableProperty]
    private int _totalQuantity;

    [ObservableProperty]
    private int _availableQuantity;

    [ObservableProperty]
    private int _rentedQuantity;

    [ObservableProperty]
    private decimal _dailyRate;

    [ObservableProperty]
    private decimal _weeklyRate;

    [ObservableProperty]
    private decimal _monthlyRate;

    [ObservableProperty]
    private decimal _securityDeposit;

    [ObservableProperty]
    private bool _isAvailable;

    public string DailyRateFormatted => $"${DailyRate:N2}";
    public string WeeklyRateFormatted => $"${WeeklyRate:N2}";
    public string MonthlyRateFormatted => $"${MonthlyRate:N2}";
    public string DepositFormatted => $"${SecurityDeposit:N2}";
}

/// <summary>
/// Undoable action for adding a rental item.
/// </summary>
public class RentalItemAddAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public RentalItemAddAction(string description, RentalItem _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for editing a rental item.
/// </summary>
public class RentalItemEditAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public RentalItemEditAction(string description, RentalItem _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for deleting a rental item.
/// </summary>
public class RentalItemDeleteAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public RentalItemDeleteAction(string description, RentalItem _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}
