using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Rental Inventory page.
/// </summary>
public partial class RentalInventoryPageViewModel : SortablePageViewModelBase
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

    [ObservableProperty]
    private double _columnMenuX;

    [ObservableProperty]
    private double _columnMenuY;

    /// <summary>
    /// Column widths manager for the table (shared across page navigations).
    /// </summary>
    public RentalInventoryTableColumnWidths ColumnWidths => App.RentalInventoryColumnWidths;

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

    #region Items Collection

    private readonly List<RentalItem> _allItems = [];

    public ObservableCollection<RentalItemDisplayItem> Items { get; } = [];

    public ObservableCollection<string> StatusOptions { get; } = ["All", "Available", "In Maintenance", "All Rented"];

    public ObservableCollection<string> AvailabilityOptions { get; } = ["All", "Available Only", "Unavailable Only"];

    #endregion

    #region Pagination

    [ObservableProperty]
    private string _paginationText = "0 items";

    /// <inheritdoc />
    protected override void OnSortOrPageChanged() => FilterItems();

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

        // Apply sorting (only if not searching, since search has its own relevance sorting)
        if (string.IsNullOrWhiteSpace(SearchQuery) || SortDirection != SortDirection.None)
        {
            displayItems = displayItems.ApplySort(
                SortColumn,
                SortDirection,
                new Dictionary<string, Func<RentalItemDisplayItem, object?>>
                {
                    ["Name"] = i => i.Name,
                    ["Supplier"] = i => i.SupplierName,
                    ["Status"] = i => i.Status,
                    ["TotalQty"] = i => i.TotalQuantity,
                    ["Available"] = i => i.AvailableQuantity,
                    ["Rented"] = i => i.RentedQuantity,
                    ["DailyRate"] = i => i.DailyRate,
                    ["WeeklyRate"] = i => i.WeeklyRate,
                    ["Deposit"] = i => i.SecurityDeposit
                },
                i => i.Name);
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

    protected override void UpdatePageNumbers()
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
        PaginationText = PaginationTextHelper.FormatPaginationText(
            totalCount, CurrentPage, PageSize, TotalPages, "item");
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
