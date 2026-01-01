using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Lost/Damaged page displaying lost and damaged inventory records.
/// </summary>
public partial class LostDamagedPageViewModel : ViewModelBase
{
    #region Table Column Widths

    /// <summary>
    /// Column widths manager for the table (shared across page navigations).
    /// </summary>
    public LostDamagedTableColumnWidths ColumnWidths => App.LostDamagedColumnWidths;

    #endregion

    #region Statistics

    [ObservableProperty]
    private int _totalLostDamaged;

    [ObservableProperty]
    private int _lostItems;

    [ObservableProperty]
    private int _damagedItems;

    [ObservableProperty]
    private string _totalLossValue = "$0.00";

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
    private string _filterType = "All";

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string _filterReason = "All";

    [ObservableProperty]
    private DateTimeOffset? _filterDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterDateTo;

    [ObservableProperty]
    private bool _isFilterModalOpen;

    public ObservableCollection<string> TypeOptions { get; } = ["All", "Lost", "Damaged"];
    public ObservableCollection<string> StatusOptions { get; } = ["All", "Written Off", "Insurance Claim", "Recovered", "Pending"];
    public ObservableCollection<string> ReasonOptions { get; } = ["All", "Damaged", "Lost", "Stolen", "Expired", "Other"];

    #endregion

    #region Items Collection

    private readonly List<LostDamaged> _allItems = [];

    public ObservableCollection<LostDamagedDisplayItem> Items { get; } = [];

    #endregion

    #region Pagination

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _pageSize = 10;

    public ObservableCollection<int> PageSizeOptions { get; } = [5, 10, 15, 25, 50];

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

    public LostDamagedPageViewModel()
    {
        LoadItems();

        // Subscribe to undo/redo state changes to refresh UI
        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }
    }

    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadItems();
    }

    #endregion

    #region Data Loading

    private void LoadItems()
    {
        _allItems.Clear();
        Items.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.LostDamaged == null)
            return;

        _allItems.AddRange(companyData.LostDamaged);
        UpdateStatistics();
        FilterItems();
    }

    private void UpdateStatistics()
    {
        TotalLostDamaged = _allItems.Count;
        LostItems = _allItems.Count(item => item.Reason == LostDamagedReason.Lost || item.Reason == LostDamagedReason.Stolen);
        DamagedItems = _allItems.Count(item => item.Reason == LostDamagedReason.Damaged || item.Reason == LostDamagedReason.Expired);
        var totalValue = _allItems.Sum(item => item.ValueLost);
        TotalLossValue = $"${totalValue:N2}";
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
            var query = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(item =>
                item.Id.ToLowerInvariant().Contains(query) ||
                GetProductName(item.ProductId).ToLowerInvariant().Contains(query) ||
                item.Notes.ToLowerInvariant().Contains(query)
            ).ToList();
        }

        // Apply type filter
        if (FilterType != "All")
        {
            filtered = FilterType switch
            {
                "Lost" => filtered.Where(item =>
                    item.Reason == LostDamagedReason.Lost ||
                    item.Reason == LostDamagedReason.Stolen).ToList(),
                "Damaged" => filtered.Where(item =>
                    item.Reason == LostDamagedReason.Damaged ||
                    item.Reason == LostDamagedReason.Expired ||
                    item.Reason == LostDamagedReason.Other).ToList(),
                _ => filtered
            };
        }

        // Apply reason filter
        if (FilterReason != "All")
        {
            var reason = Enum.TryParse<LostDamagedReason>(FilterReason, out var r) ? r : LostDamagedReason.Other;
            filtered = filtered.Where(item => item.Reason == reason).ToList();
        }

        // Apply date filter
        if (FilterDateFrom.HasValue)
        {
            filtered = filtered.Where(item => item.DateDiscovered >= FilterDateFrom.Value.DateTime).ToList();
        }
        if (FilterDateTo.HasValue)
        {
            filtered = filtered.Where(item => item.DateDiscovered <= FilterDateTo.Value.DateTime).ToList();
        }

        // Sort by date descending (newest first)
        filtered = filtered.OrderByDescending(item => item.DateDiscovered).ToList();

        // Create display items
        var displayItems = filtered.Select(CreateDisplayItem).ToList();

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
            Items.Add(item);
        }
    }

    private LostDamagedDisplayItem CreateDisplayItem(LostDamaged item)
    {
        var productName = GetProductName(item.ProductId);
        var staffName = GetStaffName(item);
        var itemType = GetItemType(item.Reason);
        var status = GetItemStatus(item);

        return new LostDamagedDisplayItem
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = productName,
            ItemType = itemType,
            DateDiscovered = item.DateDiscovered,
            Reason = item.Reason.ToString(),
            StaffName = staffName,
            ValueLost = item.ValueLost,
            Status = status,
            Notes = item.Notes,
            Quantity = item.Quantity,
            InsuranceClaim = item.InsuranceClaim
        };
    }

    private string GetProductName(string productId)
    {
        var companyData = App.CompanyManager?.CompanyData;
        var product = companyData?.GetProduct(productId);
        return product?.Name ?? "Unknown Product";
    }

    private string GetStaffName(LostDamaged item)
    {
        // For now, return a placeholder since we don't have a specific field for who reported it
        // In a real implementation, this would come from a "ReportedBy" field
        return "Staff Member";
    }

    private static string GetItemType(LostDamagedReason reason)
    {
        return reason switch
        {
            LostDamagedReason.Lost or LostDamagedReason.Stolen => "Lost",
            LostDamagedReason.Damaged or LostDamagedReason.Expired or LostDamagedReason.Other => "Damaged",
            _ => "Unknown"
        };
    }

    private static string GetItemStatus(LostDamaged item)
    {
        if (item.InsuranceClaim)
            return "Insurance Claim";

        // Default status logic based on age of record
        var daysSinceDiscovery = (DateTime.UtcNow - item.DateDiscovered).TotalDays;
        if (daysSinceDiscovery > 30)
            return "Written Off";

        return "Pending";
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
        PaginationText = PaginationTextHelper.FormatPaginationText(
            totalCount, CurrentPage, PageSize, TotalPages, "item");
    }

    #endregion

    #region Filter Modal Commands

    [RelayCommand]
    private void OpenFilterModal()
    {
        IsFilterModalOpen = true;
    }

    [RelayCommand]
    private void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        CurrentPage = 1;
        FilterItems();
        IsFilterModalOpen = false;
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterType = "All";
        FilterStatus = "All";
        FilterReason = "All";
        FilterDateFrom = null;
        FilterDateTo = null;
        SearchQuery = null;
        CurrentPage = 1;
        FilterItems();
        IsFilterModalOpen = false;
    }

    #endregion

    #region View Details Modal

    [ObservableProperty]
    private bool _isViewDetailsModalOpen;

    [ObservableProperty]
    private string _viewDetailsId = string.Empty;

    [ObservableProperty]
    private string _viewDetailsProduct = string.Empty;

    [ObservableProperty]
    private string _viewDetailsType = string.Empty;

    [ObservableProperty]
    private string _viewDetailsReason = string.Empty;

    [ObservableProperty]
    private string _viewDetailsNotes = string.Empty;

    [ObservableProperty]
    private string _viewDetailsDate = string.Empty;

    [ObservableProperty]
    private string _viewDetailsValue = string.Empty;

    [ObservableProperty]
    private string _viewDetailsQuantity = string.Empty;

    #endregion

    #region Undo Item Modal

    private LostDamagedDisplayItem? _undoItem;

    [ObservableProperty]
    private bool _isUndoItemModalOpen;

    [ObservableProperty]
    private string _undoItemDescription = string.Empty;

    [ObservableProperty]
    private string _undoItemReason = string.Empty;

    #endregion

    #region Action Commands

    [RelayCommand]
    private void ViewItemDetails(LostDamagedDisplayItem? item)
    {
        if (item == null) return;

        ViewDetailsId = item.Id;
        ViewDetailsProduct = item.ProductName;
        ViewDetailsType = item.ItemType;
        ViewDetailsReason = item.Reason;
        ViewDetailsNotes = string.IsNullOrWhiteSpace(item.Notes) ? "No notes provided" : item.Notes;
        ViewDetailsDate = item.DateFormatted;
        ViewDetailsValue = item.ValueLostFormatted;
        ViewDetailsQuantity = item.QuantityFormatted;
        IsViewDetailsModalOpen = true;
    }

    [RelayCommand]
    private void CloseViewDetailsModal()
    {
        IsViewDetailsModalOpen = false;
    }

    [RelayCommand]
    private void UndoItem(LostDamagedDisplayItem? item)
    {
        if (item == null) return;

        _undoItem = item;
        UndoItemDescription = $"{item.Id} - {item.ProductName}";
        UndoItemReason = string.Empty;
        IsUndoItemModalOpen = true;
    }

    [RelayCommand]
    private void CloseUndoItemModal()
    {
        IsUndoItemModalOpen = false;
        _undoItem = null;
        UndoItemReason = string.Empty;
    }

    [RelayCommand]
    private void ConfirmUndoItem()
    {
        if (_undoItem == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
        {
            CloseUndoItemModal();
            return;
        }

        var lostDamagedRecord = companyData.LostDamaged.FirstOrDefault(ld => ld.Id == _undoItem.Id);
        if (lostDamagedRecord != null)
        {
            companyData.LostDamaged.Remove(lostDamagedRecord);
            App.CompanyManager?.MarkAsChanged();
        }

        CloseUndoItemModal();
        LoadItems();
    }

    #endregion
}

/// <summary>
/// Display model for lost/damaged items in the UI.
/// </summary>
public partial class LostDamagedDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _productId = string.Empty;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private string _itemType = string.Empty;

    [ObservableProperty]
    private DateTime _dateDiscovered;

    [ObservableProperty]
    private string _reason = string.Empty;

    [ObservableProperty]
    private string _staffName = string.Empty;

    [ObservableProperty]
    private decimal _valueLost;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private bool _insuranceClaim;

    // Computed properties for display
    public string DateFormatted => DateDiscovered.ToString("MMM d, yyyy");
    public string ValueLostFormatted => $"${ValueLost:N2}";
    public string QuantityFormatted => $"{Quantity} unit(s)";

    public bool IsLost => ItemType == "Lost";
    public bool IsDamaged => ItemType == "Damaged";

    public string TypeBadgeBackground => ItemType switch
    {
        "Lost" => "#FEF3C7",
        "Damaged" => "#FEE2E2",
        _ => "#F3F4F6"
    };

    public string TypeBadgeForeground => ItemType switch
    {
        "Lost" => "#D97706",
        "Damaged" => "#DC2626",
        _ => "#6B7280"
    };

    public string StatusBadgeBackground => Status switch
    {
        "Written Off" => "#F3F4F6",
        "Insurance Claim" => "#DBEAFE",
        "Recovered" => "#DCFCE7",
        "Pending" => "#FEF3C7",
        _ => "#F3F4F6"
    };

    public string StatusBadgeForeground => Status switch
    {
        "Written Off" => "#6B7280",
        "Insurance Claim" => "#2563EB",
        "Recovered" => "#16A34A",
        "Pending" => "#D97706",
        _ => "#6B7280"
    };
}
