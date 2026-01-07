using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Locations page.
/// Displays warehouse and storage locations.
/// </summary>
public partial class LocationsPageViewModel : SortablePageViewModelBase
{
    #region Statistics

    [ObservableProperty]
    private int _totalLocations;

    [ObservableProperty]
    private int _totalStockItems;

    [ObservableProperty]
    private string _totalInventoryValue = "$0";

    [ObservableProperty]
    private string _averageCapacityUsed = "0%";

    #endregion

    #region Table Column Widths

    /// <summary>
    /// Column widths manager for the table (shared across page navigations).
    /// </summary>
    public LocationsTableColumnWidths ColumnWidths => App.LocationsColumnWidths;

    #endregion

    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterLocations();
    }

    [ObservableProperty]
    private string _filterType = "All";

    [ObservableProperty]
    private string _filterStatus = "All";

    #endregion

    #region Locations Collection

    /// <summary>
    /// All locations (unfiltered).
    /// </summary>
    private readonly List<Location> _allLocations = [];

    /// <summary>
    /// Locations for display in the table.
    /// </summary>
    public ObservableCollection<LocationDisplayItem> Locations { get; } = [];

    /// <summary>
    /// Location type options for filter.
    /// </summary>
    public ObservableCollection<string> TypeOptions { get; } = ["All", "Warehouse", "Storage Facility", "Factory", "Retail Store", "Distribution Center"];

    /// <summary>
    /// Status options for filter.
    /// </summary>
    public ObservableCollection<string> StatusOptions { get; } = ["All", "Active", "Inactive"];

    #endregion

    #region Pagination

    [ObservableProperty]
    private string _paginationText = "0 locations";

    /// <inheritdoc />
    protected override void OnSortOrPageChanged() => FilterLocations();

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public LocationsPageViewModel()
    {
        LoadLocations();

        // Subscribe to undo/redo state changes to refresh UI
        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }

        // Subscribe to modal events to refresh when locations are saved
        if (App.LocationsModalsViewModel != null)
        {
            App.LocationsModalsViewModel.LocationSaved += OnModalLocationSaved;
            App.LocationsModalsViewModel.LocationDeleted += OnModalLocationDeleted;
            App.LocationsModalsViewModel.FiltersApplied += OnFiltersApplied;
        }
    }

    /// <summary>
    /// Handles filter applied events from the modals.
    /// </summary>
    private void OnFiltersApplied(object? sender, LocationsFilterAppliedEventArgs e)
    {
        FilterType = e.Type;
        FilterStatus = e.Status;
        CurrentPage = 1;
        FilterLocations();
    }

    /// <summary>
    /// Handles undo/redo state changes by refreshing the locations.
    /// </summary>
    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadLocations();
    }

    /// <summary>
    /// Handles location saved events from the modals.
    /// </summary>
    private void OnModalLocationSaved(object? sender, EventArgs e)
    {
        LoadLocations();
    }

    /// <summary>
    /// Handles location deleted events from the modals.
    /// </summary>
    private void OnModalLocationDeleted(object? sender, EventArgs e)
    {
        LoadLocations();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads locations from the company data.
    /// </summary>
    private void LoadLocations()
    {
        _allLocations.Clear();
        Locations.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Locations == null)
            return;

        _allLocations.AddRange(companyData.Locations);
        UpdateStatistics();
        FilterLocations();
    }

    /// <summary>
    /// Updates the statistics based on current data.
    /// </summary>
    private void UpdateStatistics()
    {
        var companyData = App.CompanyManager?.CompanyData;

        TotalLocations = _allLocations.Count;

        // Calculate total stock items across all locations
        var inventory = companyData?.Inventory ?? [];
        TotalStockItems = inventory.Sum(i => i.InStock);

        // Calculate total inventory value
        var totalValue = inventory.Sum(i => i.InStock * i.UnitCost);
        TotalInventoryValue = $"${totalValue:N0}";

        // Calculate average capacity used
        if (_allLocations.Count > 0)
        {
            var totalCapacity = _allLocations.Sum(l => l.Capacity);
            var totalUtilization = _allLocations.Sum(l => l.CurrentUtilization);
            var avgPercentage = totalCapacity > 0 ? (double)totalUtilization / totalCapacity * 100 : 0;
            AverageCapacityUsed = $"{avgPercentage:N0}%";
        }
        else
        {
            AverageCapacityUsed = "0%";
        }
    }

    /// <summary>
    /// Refreshes the locations from the data source.
    /// </summary>
    [RelayCommand]
    private void RefreshLocations()
    {
        LoadLocations();
    }

    /// <summary>
    /// Filters locations based on search query and filters.
    /// </summary>
    private void FilterLocations()
    {
        Locations.Clear();

        var filtered = _allLocations.ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered
                .Select(l => new
                {
                    Location = l,
                    NameScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, l.Name),
                    AddressScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, l.Address.City),
                    ContactScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, l.ContactPerson),
                    IdScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, l.Id)
                })
                .Where(x => x.NameScore >= 0 || x.AddressScore >= 0 || x.ContactScore >= 0 || x.IdScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.NameScore, x.AddressScore), Math.Max(x.ContactScore, x.IdScore)))
                .Select(x => x.Location)
                .ToList();
        }

        // Apply type filter (using metadata or naming convention)
        if (FilterType != "All")
        {
            // For now, filter by name pattern until we add a Type field to Location
            filtered = filtered.Where(l => GetLocationType(l) == FilterType).ToList();
        }

        // Apply status filter
        if (FilterStatus != "All")
        {
            var isActive = FilterStatus == "Active";
            // Locations don't have a status field, so we assume all are active for now
            // In a real implementation, you'd add a Status field to Location
            if (!isActive)
            {
                filtered = []; // No inactive locations for now
            }
        }

        // Create display items
        var displayItems = filtered.Select(location =>
        {
            var addressParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(location.Address.Street))
                addressParts.Add(location.Address.Street);
            if (!string.IsNullOrWhiteSpace(location.Address.City))
                addressParts.Add(location.Address.City);
            if (!string.IsNullOrWhiteSpace(location.Address.State))
                addressParts.Add(location.Address.State);
            var addressString = addressParts.Count > 0 ? string.Join(", ", addressParts) : "-";

            return new LocationDisplayItem
            {
                Id = location.Id,
                Name = location.Name,
                Type = GetLocationType(location),
                Address = addressString,
                Manager = string.IsNullOrWhiteSpace(location.ContactPerson) ? "-" : location.ContactPerson,
                Phone = location.Phone,
                Capacity = location.Capacity,
                CurrentUtilization = location.CurrentUtilization,
                UtilizationPercentage = location.UtilizationPercentage,
                IsActive = true, // All locations are active for now
                CreatedAt = location.CreatedAt
            };
        }).ToList();

        // Apply sorting
        if (string.IsNullOrWhiteSpace(SearchQuery) || SortDirection != SortDirection.None)
        {
            displayItems = displayItems.ApplySort(
                SortColumn,
                SortDirection,
                new Dictionary<string, Func<LocationDisplayItem, object?>>
                {
                    ["Location"] = l => l.Name,
                    ["Type"] = l => l.Type,
                    ["Address"] = l => l.Address,
                    ["Manager"] = l => l.Manager,
                    ["Status"] = l => l.IsActive
                },
                l => l.Name);
        }

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination and add to collection
        var pagedLocations = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedLocations)
        {
            Locations.Add(item);
        }
    }

    /// <summary>
    /// Gets the location type based on naming convention.
    /// </summary>
    private static string GetLocationType(Location location)
    {
        var name = location.Name.ToLowerInvariant();
        if (name.Contains("warehouse")) return "Warehouse";
        if (name.Contains("storage")) return "Storage Facility";
        if (name.Contains("factory")) return "Factory";
        if (name.Contains("retail") || name.Contains("store")) return "Retail Store";
        if (name.Contains("distribution")) return "Distribution Center";
        return "Warehouse"; // Default
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
            totalCount, CurrentPage, PageSize, TotalPages, "location");
    }

    #endregion

    #region Modal Commands

    /// <summary>
    /// Opens the Add Location modal.
    /// </summary>
    [RelayCommand]
    private void OpenAddModal()
    {
        App.LocationsModalsViewModel?.OpenAddModal();
    }

    /// <summary>
    /// Opens the Edit Location modal.
    /// </summary>
    [RelayCommand]
    private void OpenEditModal(LocationDisplayItem? item)
    {
        if (item == null) return;
        App.LocationsModalsViewModel?.OpenEditModal(item);
    }

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void OpenDeleteConfirm(LocationDisplayItem? item)
    {
        if (item == null) return;
        App.LocationsModalsViewModel?.OpenDeleteConfirm(item);
    }

    #endregion

    #region Filter Modal

    /// <summary>
    /// Opens the filter modal via the modals ViewModel.
    /// </summary>
    [RelayCommand]
    private void OpenFilterModal()
    {
        App.LocationsModalsViewModel?.OpenFilterModal();
    }

    #endregion
}

/// <summary>
/// Display model for locations in the UI.
/// </summary>
public partial class LocationDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _manager = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private int _capacity;

    [ObservableProperty]
    private int _currentUtilization;

    [ObservableProperty]
    private double _utilizationPercentage;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private DateTime _createdAt;

    /// <summary>
    /// Gets the initials from the location name for avatar display.
    /// </summary>
    public string Initials
    {
        get
        {
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
            if (parts is [{ Length: >= 2 }])
                return parts[0][..2].ToUpperInvariant();
            if (parts is [{ Length: 1 }])
                return parts[0].ToUpperInvariant();
            return "?";
        }
    }

    /// <summary>
    /// Gets the status text.
    /// </summary>
    public string StatusText => IsActive ? "Active" : "Inactive";

    /// <summary>
    /// Gets the status color (green for active).
    /// </summary>
    public string StatusColor => IsActive ? "#22C55E" : "#6B7280";

    /// <summary>
    /// Gets the status background color.
    /// </summary>
    public string StatusBackground => IsActive ? "#DCFCE7" : "#F3F4F6";
}
