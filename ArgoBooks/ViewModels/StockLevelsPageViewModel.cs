using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Stock Levels page.
/// Displays inventory levels for products across locations.
/// </summary>
public partial class StockLevelsPageViewModel : SortablePageViewModelBase
{
    #region Table Column Widths

    /// <summary>
    /// Column widths manager for the table (shared across page navigations).
    /// </summary>
    public StockLevelsTableColumnWidths ColumnWidths => App.StockLevelsColumnWidths;

    #endregion

    #region Tab Selection

    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>
    /// Gets whether the All Items tab is selected.
    /// </summary>
    public bool IsAllItemsTabSelected => SelectedTabIndex == 0;

    /// <summary>
    /// Gets whether the Low Stock tab is selected.
    /// </summary>
    public bool IsLowStockTabSelected => SelectedTabIndex == 1;

    /// <summary>
    /// Gets whether the Out of Stock tab is selected.
    /// </summary>
    public bool IsOutOfStockTabSelected => SelectedTabIndex == 2;

    /// <summary>
    /// Gets whether the Overstock tab is selected.
    /// </summary>
    public bool IsOverstockTabSelected => SelectedTabIndex == 3;

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsAllItemsTabSelected));
        OnPropertyChanged(nameof(IsLowStockTabSelected));
        OnPropertyChanged(nameof(IsOutOfStockTabSelected));
        OnPropertyChanged(nameof(IsOverstockTabSelected));
        CurrentPage = 1;
        FilterItems();
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
    private string _filterCategory = "All";

    [ObservableProperty]
    private string _filterLocation = "All";

    [ObservableProperty]
    private string _filterStatus = "All";

    #endregion

    #region Statistics

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private int _inStockCount;

    [ObservableProperty]
    private int _lowStockCount;

    [ObservableProperty]
    private int _outOfStockCount;

    [ObservableProperty]
    private int _overstockCount;

    #endregion

    #region Data Collections

    /// <summary>
    /// All inventory items (unfiltered).
    /// </summary>
    private readonly List<InventoryItem> _allItems = [];

    /// <summary>
    /// Gets whether there are any inventory items.
    /// </summary>
    public bool HasInventoryItems => _allItems.Count > 0;

    /// <summary>
    /// Filtered display items for the current view.
    /// </summary>
    public ObservableCollection<StockLevelDisplayItem> DisplayItems { get; } = [];

    /// <summary>
    /// Available categories for filter dropdown.
    /// </summary>
    public ObservableCollection<string> AvailableCategories { get; } = ["All"];

    /// <summary>
    /// Available locations for filter dropdown.
    /// </summary>
    public ObservableCollection<string> AvailableLocations { get; } = ["All"];

    /// <summary>
    /// Available status options for filter dropdown.
    /// </summary>
    public ObservableCollection<string> StatusOptions { get; } = ["All", "In Stock", "Low Stock", "Out of Stock", "Overstock"];

    #endregion

    #region Pagination

    [ObservableProperty]
    private string _paginationText = "0 items";

    /// <inheritdoc />
    protected override void OnSortOrPageChanged() => FilterItems();

    #endregion


    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public StockLevelsPageViewModel()
    {
        LoadItems();

        // Subscribe to undo/redo state changes to refresh UI
        App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;

        // Subscribe to modal events to refresh when items are saved
        if (App.StockLevelsModalsViewModel != null)
        {
            App.StockLevelsModalsViewModel.ItemSaved += OnModalItemSaved;
            App.StockLevelsModalsViewModel.FiltersApplied += OnFiltersApplied;
        }
    }

    /// <summary>
    /// Handles filter applied events from the modals.
    /// </summary>
    private void OnFiltersApplied(object? sender, FilterAppliedEventArgs e)
    {
        FilterCategory = e.Category;
        FilterLocation = e.Location;
        FilterStatus = e.Status;
        CurrentPage = 1;
        FilterItems();
    }

    /// <summary>
    /// Handles item saved events from the modals.
    /// </summary>
    private void OnModalItemSaved(object? sender, EventArgs e)
    {
        LoadItems();
    }

    /// <summary>
    /// Handles undo/redo state changes by refreshing the items.
    /// </summary>
    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadItems();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads inventory items from the company data.
    /// </summary>
    private void LoadItems()
    {
        _allItems.Clear();
        DisplayItems.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Load inventory items
        if (companyData.Inventory != null)
        {
            _allItems.AddRange(companyData.Inventory);
        }

        UpdateStatistics();
        UpdateDropdownOptions();
        FilterItems();

        // Notify that HasInventoryItems may have changed
        OnPropertyChanged(nameof(HasInventoryItems));
        OpenBulkAdjustModalCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Updates the statistics based on current data.
    /// </summary>
    private void UpdateStatistics()
    {
        TotalItems = _allItems.Sum(i => i.InStock);
        InStockCount = _allItems.Count(i => i.CalculateStatus() == InventoryStatus.InStock);
        LowStockCount = _allItems.Count(i => i.CalculateStatus() == InventoryStatus.LowStock);
        OutOfStockCount = _allItems.Count(i => i.CalculateStatus() == InventoryStatus.OutOfStock);
        OverstockCount = _allItems.Count(i => i.CalculateStatus() == InventoryStatus.Overstock);
    }

    /// <summary>
    /// Updates the dropdown options from company data.
    /// </summary>
    private void UpdateDropdownOptions()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Update categories
        AvailableCategories.Clear();
        AvailableCategories.Add("All");

        if (companyData.Categories != null)
        {
            var categories = companyData.Categories
                .Where(c => c.ItemType == "Product")
                .Select(c => c.Name)
                .Distinct()
                .OrderBy(c => c);

            foreach (var category in categories)
            {
                AvailableCategories.Add(category);
            }
        }

        // Update locations
        AvailableLocations.Clear();
        AvailableLocations.Add("All");

        if (companyData.Locations != null)
        {
            var locations = companyData.Locations
                .Select(l => l.Name)
                .Distinct()
                .OrderBy(l => l);

            foreach (var location in locations)
            {
                AvailableLocations.Add(location);
            }
        }
    }

    /// <summary>
    /// Refreshes the items from the data source.
    /// </summary>
    [RelayCommand]
    private void RefreshItems()
    {
        LoadItems();
    }

    /// <summary>
    /// Filters items based on current tab, search query, and filters.
    /// </summary>
    private void FilterItems()
    {
        DisplayItems.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var filtered = _allItems.ToList();

        // Apply tab filter
        filtered = SelectedTabIndex switch
        {
            1 => filtered.Where(i => i.CalculateStatus() == InventoryStatus.LowStock).ToList(),
            2 => filtered.Where(i => i.CalculateStatus() == InventoryStatus.OutOfStock).ToList(),
            3 => filtered.Where(i => i.CalculateStatus() == InventoryStatus.Overstock).ToList(),
            _ => filtered
        };

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var searchProducts = companyData.Products ?? [];
            filtered = filtered
                .Select(i => new
                {
                    Item = i,
                    Product = searchProducts.FirstOrDefault(p => p.Id == i.ProductId),
                    SkuScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, i.Sku)
                })
                .Where(x => x.Product != null)
                .Select(x => new
                {
                    x.Item,
                    x.Product,
                    x.SkuScore,
                    NameScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, x.Product!.Name)
                })
                .Where(x => x.NameScore >= 0 || x.SkuScore >= 0)
                .OrderByDescending(x => Math.Max(x.NameScore, x.SkuScore))
                .Select(x => x.Item)
                .ToList();
        }

        // Apply category filter
        if (FilterCategory != "All")
        {
            var categoryProducts = (companyData.Categories ?? [])
                .Where(c => c.Name == FilterCategory)
                .SelectMany(c => (companyData.Products ?? []).Where(p => p.CategoryId == c.Id))
                .Select(p => p.Id)
                .ToHashSet();

            filtered = filtered.Where(i => categoryProducts.Contains(i.ProductId)).ToList();
        }

        // Apply location filter
        if (FilterLocation != "All")
        {
            var locationId = (companyData.Locations ?? [])
                .FirstOrDefault(l => l.Name == FilterLocation)?.Id;

            if (locationId != null)
            {
                filtered = filtered.Where(i => i.LocationId == locationId).ToList();
            }
        }

        // Apply status filter (from filter modal, not tab)
        if (FilterStatus != "All")
        {
            var targetStatus = FilterStatus switch
            {
                "In Stock" => InventoryStatus.InStock,
                "Low Stock" => InventoryStatus.LowStock,
                "Out of Stock" => InventoryStatus.OutOfStock,
                "Overstock" => InventoryStatus.Overstock,
                _ => (InventoryStatus?)null
            };

            if (targetStatus.HasValue)
            {
                filtered = filtered.Where(i => i.CalculateStatus() == targetStatus.Value).ToList();
            }
        }

        // Create display items
        var products = companyData.Products ?? [];
        var locations = companyData.Locations ?? [];
        var categories = companyData.Categories ?? [];

        var displayItems = filtered.Select(item =>
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            var location = locations.FirstOrDefault(l => l.Id == item.LocationId);
            var category = product != null ? categories.FirstOrDefault(c => c.Id == product.CategoryId) : null;
            var status = item.CalculateStatus();

            return new StockLevelDisplayItem
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = product?.Name ?? "Unknown Product",
                Sku = item.Sku,
                CategoryName = category?.Name ?? "-",
                LocationName = location?.Name ?? "Default",
                InStock = item.InStock,
                Reserved = item.Reserved,
                Available = item.Available,
                ReorderPoint = item.ReorderPoint,
                Status = status,
                StatusText = GetStatusText(status),
                StatusColor = GetStatusColor(status),
                StatusBackground = GetStatusBackground(status),
                LastUpdated = item.LastUpdated
            };
        }).ToList();

        // Apply sorting
        if (string.IsNullOrWhiteSpace(SearchQuery) || SortDirection != SortDirection.None)
        {
            displayItems = displayItems.ApplySort(
                SortColumn,
                SortDirection,
                new Dictionary<string, Func<StockLevelDisplayItem, object?>>
                {
                    ["Product"] = i => i.ProductName,
                    ["Sku"] = i => i.Sku,
                    ["Category"] = i => i.CategoryName,
                    ["Location"] = i => i.LocationName,
                    ["InStock"] = i => i.InStock,
                    ["Reserved"] = i => i.Reserved,
                    ["Available"] = i => i.Available,
                    ["ReorderPoint"] = i => i.ReorderPoint,
                    ["Status"] = i => i.Status
                },
                i => i.ProductName);
        }

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination and add to collection
        var pagedItems = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedItems)
        {
            DisplayItems.Add(item);
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

    private static string GetStatusText(InventoryStatus status) => status switch
    {
        InventoryStatus.InStock => "In Stock",
        InventoryStatus.LowStock => "Low Stock",
        InventoryStatus.OutOfStock => "Out of Stock",
        InventoryStatus.Overstock => "Overstock",
        _ => "Unknown"
    };

    private static string GetStatusColor(InventoryStatus status) => status switch
    {
        InventoryStatus.InStock => "#22C55E",
        InventoryStatus.LowStock => "#F59E0B",
        InventoryStatus.OutOfStock => "#EF4444",
        InventoryStatus.Overstock => "#8B5CF6",
        _ => "#6B7280"
    };

    private static string GetStatusBackground(InventoryStatus status) => status switch
    {
        InventoryStatus.InStock => "#DCFCE7",
        InventoryStatus.LowStock => "#FEF3C7",
        InventoryStatus.OutOfStock => "#FEE2E2",
        InventoryStatus.Overstock => "#EDE9FE",
        _ => "#F3F4F6"
    };

    #endregion

    #region Adjust Stock

    /// <summary>
    /// Opens the adjust stock modal for an item.
    /// </summary>
    [RelayCommand]
    private void OpenAdjustStockModal(StockLevelDisplayItem? item)
    {
        if (item == null) return;
        App.StockLevelsModalsViewModel?.OpenAdjustStockModal(item.Id, item.ProductName, item.InStock);
    }

    #endregion

    #region Filter Modal

    /// <summary>
    /// Opens the filter modal via the modals ViewModel.
    /// </summary>
    [RelayCommand]
    private void OpenFilterModal()
    {
        App.StockLevelsModalsViewModel?.OpenFilterModal(
            AvailableCategories,
            AvailableLocations,
            FilterCategory,
            FilterLocation,
            FilterStatus);
    }

    #endregion

    #region Add Item Modal

    /// <summary>
    /// Opens the add item modal.
    /// </summary>
    [RelayCommand]
    private void OpenAddItemModal()
    {
        App.StockLevelsModalsViewModel?.OpenAddItemModalCommand.Execute(null);
    }

    #endregion

    #region Bulk Adjust Modal

    /// <summary>
    /// Opens a bulk adjust stock modal (placeholder - uses first selected item for now).
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasInventoryItems))]
    private void OpenBulkAdjustModal()
    {
        // For now, open adjust for first item
        var firstItem = DisplayItems.FirstOrDefault();
        if (firstItem != null)
        {
            App.StockLevelsModalsViewModel?.OpenAdjustStockModal(firstItem.Id, firstItem.ProductName, firstItem.InStock);
        }
    }

    #endregion
}

/// <summary>
/// Display model for stock level items in the UI.
/// </summary>
public partial class StockLevelDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _productId = string.Empty;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private string _sku = string.Empty;

    [ObservableProperty]
    private string _categoryName = string.Empty;

    [ObservableProperty]
    private string _locationName = string.Empty;

    [ObservableProperty]
    private int _inStock;

    [ObservableProperty]
    private int _reserved;

    [ObservableProperty]
    private int _available;

    [ObservableProperty]
    private int _reorderPoint;

    [ObservableProperty]
    private InventoryStatus _status;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _statusColor = string.Empty;

    [ObservableProperty]
    private string _statusBackground = string.Empty;

    [ObservableProperty]
    private DateTime _lastUpdated;
}
