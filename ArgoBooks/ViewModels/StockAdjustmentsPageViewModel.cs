using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Stock Adjustments page.
/// Displays history of all stock level adjustments.
/// </summary>
public partial class StockAdjustmentsPageViewModel : SortablePageViewModelBase
{
    #region Statistics

    [ObservableProperty]
    private int _totalAdjustments;

    [ObservableProperty]
    private int _totalAdded;

    [ObservableProperty]
    private int _totalRemoved;

    [ObservableProperty]
    private int _netChange;

    #endregion

    #region Table Column Widths

    /// <summary>
    /// Column widths manager for the table (shared across page navigations).
    /// </summary>
    public StockAdjustmentsTableColumnWidths ColumnWidths => App.StockAdjustmentsColumnWidths;

    #endregion

    #region Tabs

    [ObservableProperty]
    private string _activeTab = "All";

    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>
    /// Tab options for filtering.
    /// </summary>
    public ObservableCollection<string> TabOptions { get; } = ["All", "Add", "Remove", "Set"];

    partial void OnActiveTabChanged(string value)
    {
        CurrentPage = 1;
        FilterAdjustments();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        ActiveTab = value switch
        {
            0 => "All",
            1 => "Add",
            2 => "Remove",
            3 => "Set",
            _ => "All"
        };
    }

    #endregion

    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterAdjustments();
    }

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private string _filterLocation = "All";

    [ObservableProperty]
    private string _filterProduct = "All";

    #endregion

    #region Adjustments Collection

    /// <summary>
    /// All adjustments (unfiltered).
    /// </summary>
    private readonly List<StockAdjustment> _allAdjustments = [];

    /// <summary>
    /// Adjustments for display in the table.
    /// </summary>
    public ObservableCollection<StockAdjustmentDisplayItem> Adjustments { get; } = [];

    /// <summary>
    /// Location options for filter.
    /// </summary>
    public ObservableCollection<string> LocationOptions { get; } = ["All"];

    /// <summary>
    /// Product options for filter.
    /// </summary>
    public ObservableCollection<string> ProductOptions { get; } = ["All"];

    #endregion

    #region Pagination

    [ObservableProperty]
    private string _paginationText = "0 adjustments";

    /// <inheritdoc />
    protected override void OnSortOrPageChanged() => FilterAdjustments();

    #endregion

    #region Modal State

    [ObservableProperty]
    private bool _isFilterModalOpen;

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public StockAdjustmentsPageViewModel()
    {
        // Set default sort to date descending (most recent first)
        SortColumn = "Date";
        SortDirection = SortDirection.Descending;

        LoadAdjustments();

        // Subscribe to undo/redo state changes to refresh UI
        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }

        // Subscribe to modal events to refresh when adjustments are made
        if (App.StockLevelsModalsViewModel != null)
        {
            App.StockLevelsModalsViewModel.ItemSaved += OnAdjustmentMade;
        }

        // Subscribe to stock adjustments modal events
        if (App.StockAdjustmentsModalsViewModel != null)
        {
            App.StockAdjustmentsModalsViewModel.AdjustmentSaved += OnAdjustmentMade;
            App.StockAdjustmentsModalsViewModel.AdjustmentDeleted += OnAdjustmentMade;
        }
    }

    /// <summary>
    /// Handles undo/redo state changes by refreshing the adjustments.
    /// </summary>
    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadAdjustments();
    }

    /// <summary>
    /// Handles adjustment events from modals.
    /// </summary>
    private void OnAdjustmentMade(object? sender, EventArgs e)
    {
        LoadAdjustments();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads adjustments from the company data.
    /// </summary>
    private void LoadAdjustments()
    {
        _allAdjustments.Clear();
        Adjustments.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.StockAdjustments == null)
            return;

        _allAdjustments.AddRange(companyData.StockAdjustments);

        // Load filter options
        LoadFilterOptions();
        UpdateStatistics();
        FilterAdjustments();
    }

    /// <summary>
    /// Loads filter options from the data.
    /// </summary>
    private void LoadFilterOptions()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        // Update location options
        LocationOptions.Clear();
        LocationOptions.Add("All");
        var locations = companyData.Locations?.Select(l => l.Name).Distinct().OrderBy(n => n).ToList() ?? new List<string>();
        foreach (var location in locations)
        {
            LocationOptions.Add(location);
        }

        // Update product options
        ProductOptions.Clear();
        ProductOptions.Add("All");
        var products = companyData.Products?.Select(p => p.Name).Distinct().OrderBy(n => n).ToList() ?? new List<string>();
        foreach (var product in products)
        {
            ProductOptions.Add(product);
        }
    }

    /// <summary>
    /// Updates the statistics based on current data.
    /// </summary>
    private void UpdateStatistics()
    {
        TotalAdjustments = _allAdjustments.Count;

        var adds = _allAdjustments.Where(a => a.AdjustmentType == AdjustmentType.Add);
        var removes = _allAdjustments.Where(a => a.AdjustmentType == AdjustmentType.Remove);

        TotalAdded = adds.Sum(a => a.Quantity);
        TotalRemoved = removes.Sum(a => a.Quantity);
        NetChange = TotalAdded - TotalRemoved;
    }

    /// <summary>
    /// Refreshes the adjustments from the data source.
    /// </summary>
    [RelayCommand]
    private void RefreshAdjustments()
    {
        LoadAdjustments();
    }

    /// <summary>
    /// Filters adjustments based on search query, tab, and filters.
    /// </summary>
    private void FilterAdjustments()
    {
        Adjustments.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        var inventory = companyData?.Inventory ?? [];
        var products = companyData?.Products ?? [];
        var locations = companyData?.Locations ?? [];

        var filtered = _allAdjustments.ToList();

        // Apply tab filter
        if (ActiveTab != "All")
        {
            var tabType = ActiveTab switch
            {
                "Add" => AdjustmentType.Add,
                "Remove" => AdjustmentType.Remove,
                "Set" => AdjustmentType.Set,
                _ => (AdjustmentType?)null
            };

            if (tabType.HasValue)
            {
                filtered = filtered.Where(a => a.AdjustmentType == tabType.Value).ToList();
            }
        }

        // Apply date range filter
        if (StartDate.HasValue)
        {
            filtered = filtered.Where(a => a.Timestamp.Date >= StartDate.Value.Date).ToList();
        }
        if (EndDate.HasValue)
        {
            filtered = filtered.Where(a => a.Timestamp.Date <= EndDate.Value.Date).ToList();
        }

        // Apply location filter
        if (FilterLocation != "All")
        {
            filtered = filtered.Where(a =>
            {
                var invItem = inventory.FirstOrDefault(i => i.Id == a.InventoryItemId);
                if (invItem == null) return false;
                var location = locations.FirstOrDefault(l => l.Id == invItem.LocationId);
                return location?.Name == FilterLocation;
            }).ToList();
        }

        // Apply product filter
        if (FilterProduct != "All")
        {
            filtered = filtered.Where(a =>
            {
                var invItem = inventory.FirstOrDefault(i => i.Id == a.InventoryItemId);
                if (invItem == null) return false;
                var product = products.FirstOrDefault(p => p.Id == invItem.ProductId);
                return product?.Name == FilterProduct;
            }).ToList();
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered
                .Select(a =>
                {
                    var invItem = inventory.FirstOrDefault(i => i.Id == a.InventoryItemId);
                    var product = invItem != null ? products.FirstOrDefault(p => p.Id == invItem.ProductId) : null;
                    var location = invItem != null ? locations.FirstOrDefault(l => l.Id == invItem.LocationId) : null;

                    return new
                    {
                        Adjustment = a,
                        IdScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, a.Id),
                        ProductScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, product?.Name ?? ""),
                        LocationScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, location?.Name ?? ""),
                        ReasonScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, a.Reason),
                        RefScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, a.ReferenceNumber ?? "")
                    };
                })
                .Where(x => x.IdScore >= 0 || x.ProductScore >= 0 || x.LocationScore >= 0 || x.ReasonScore >= 0 || x.RefScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.IdScore, x.ProductScore), Math.Max(Math.Max(x.LocationScore, x.ReasonScore), x.RefScore)))
                .Select(x => x.Adjustment)
                .ToList();
        }

        // Create display items
        var displayItems = filtered.Select(adjustment =>
        {
            var invItem = inventory.FirstOrDefault(i => i.Id == adjustment.InventoryItemId);
            var product = invItem != null ? products.FirstOrDefault(p => p.Id == invItem.ProductId) : null;
            var location = invItem != null ? locations.FirstOrDefault(l => l.Id == invItem.LocationId) : null;

            return new StockAdjustmentDisplayItem
            {
                Id = adjustment.Id,
                Date = adjustment.Timestamp,
                DateDisplay = adjustment.Timestamp.ToString("MMM dd, yyyy"),
                TimeDisplay = adjustment.Timestamp.ToString("HH:mm"),
                Reference = adjustment.ReferenceNumber ?? "-",
                ProductId = invItem?.ProductId ?? "",
                ProductName = product?.Name ?? "Unknown Product",
                ProductSku = invItem?.Sku ?? product?.Sku ?? "",
                LocationId = invItem?.LocationId ?? "",
                LocationName = location?.Name ?? "Unknown Location",
                AdjustmentType = adjustment.AdjustmentType,
                TypeDisplay = adjustment.AdjustmentType.ToString(),
                Quantity = adjustment.Quantity,
                PreviousStock = adjustment.PreviousStock,
                NewStock = adjustment.NewStock,
                Reason = string.IsNullOrWhiteSpace(adjustment.Reason) ? "-" : adjustment.Reason,
                UserId = adjustment.UserId ?? "",
                UserDisplay = string.IsNullOrWhiteSpace(adjustment.UserId) ? "System" : adjustment.UserId
            };
        }).ToList();

        // Apply sorting
        if (string.IsNullOrWhiteSpace(SearchQuery) || SortDirection != SortDirection.None)
        {
            displayItems = displayItems.ApplySort(
                SortColumn,
                SortDirection,
                new Dictionary<string, Func<StockAdjustmentDisplayItem, object?>>
                {
                    ["Date"] = a => a.Date,
                    ["Reference"] = a => a.Reference,
                    ["Product"] = a => a.ProductName,
                    ["Location"] = a => a.LocationName,
                    ["Type"] = a => a.TypeDisplay,
                    ["Quantity"] = a => a.Quantity,
                    ["Previous"] = a => a.PreviousStock,
                    ["New"] = a => a.NewStock,
                    ["Reason"] = a => a.Reason,
                    ["User"] = a => a.UserDisplay
                },
                a => a.Date);
        }

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination and add to collection
        var pagedAdjustments = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedAdjustments)
        {
            Adjustments.Add(item);
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
            totalCount, CurrentPage, PageSize, TotalPages, "adjustment");
    }

    #endregion

    #region Modal Commands

    /// <summary>
    /// Opens the Add Adjustment modal.
    /// </summary>
    [RelayCommand]
    private void OpenAddModal()
    {
        App.StockAdjustmentsModalsViewModel?.OpenAddModal();
    }

    /// <summary>
    /// Opens the View Adjustment modal.
    /// </summary>
    [RelayCommand]
    private void ViewAdjustment(StockAdjustmentDisplayItem? item)
    {
        if (item == null) return;
        App.StockAdjustmentsModalsViewModel?.OpenViewModal(item);
    }

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void OpenDeleteConfirm(StockAdjustmentDisplayItem? item)
    {
        if (item == null) return;
        App.StockAdjustmentsModalsViewModel?.OpenDeleteConfirm(item);
    }

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
        CurrentPage = 1;
        FilterAdjustments();
        CloseFilterModal();
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        FilterLocation = "All";
        FilterProduct = "All";
        StartDate = null;
        EndDate = null;
        SearchQuery = null;
        ActiveTab = "All";
        CurrentPage = 1;
        FilterAdjustments();
        CloseFilterModal();
    }

    #endregion

    #region Tab Commands

    /// <summary>
    /// Switches to the specified tab.
    /// </summary>
    [RelayCommand]
    private void SwitchTab(string tab)
    {
        ActiveTab = tab;
    }

    #endregion
}

/// <summary>
/// Display model for stock adjustments in the UI.
/// </summary>
public partial class StockAdjustmentDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private string _dateDisplay = string.Empty;

    [ObservableProperty]
    private string _timeDisplay = string.Empty;

    [ObservableProperty]
    private string _reference = string.Empty;

    [ObservableProperty]
    private string _productId = string.Empty;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private string _productSku = string.Empty;

    [ObservableProperty]
    private string _locationId = string.Empty;

    [ObservableProperty]
    private string _locationName = string.Empty;

    [ObservableProperty]
    private AdjustmentType _adjustmentType;

    [ObservableProperty]
    private string _typeDisplay = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private int _previousStock;

    [ObservableProperty]
    private int _newStock;

    [ObservableProperty]
    private string _reason = string.Empty;

    [ObservableProperty]
    private string _userId = string.Empty;

    [ObservableProperty]
    private string _userDisplay = string.Empty;

    /// <summary>
    /// Gets the type badge color based on adjustment type.
    /// </summary>
    public string TypeColor => AdjustmentType switch
    {
        AdjustmentType.Add => "#22C55E",
        AdjustmentType.Remove => "#EF4444",
        AdjustmentType.Set => "#3B82F6",
        _ => "#6B7280"
    };

    /// <summary>
    /// Gets the type badge background color based on adjustment type.
    /// </summary>
    public string TypeBackground => AdjustmentType switch
    {
        AdjustmentType.Add => "#DCFCE7",
        AdjustmentType.Remove => "#FEE2E2",
        AdjustmentType.Set => "#DBEAFE",
        _ => "#F3F4F6"
    };

    /// <summary>
    /// Gets the quantity change display with sign.
    /// </summary>
    public string QuantityDisplay => AdjustmentType switch
    {
        AdjustmentType.Add => $"+{Quantity}",
        AdjustmentType.Remove => $"-{Quantity}",
        AdjustmentType.Set => Quantity.ToString(),
        _ => Quantity.ToString()
    };

    /// <summary>
    /// Gets the quantity display color.
    /// </summary>
    public string QuantityColor => AdjustmentType switch
    {
        AdjustmentType.Add => "#22C55E",
        AdjustmentType.Remove => "#EF4444",
        _ => "#6B7280"
    };
}
