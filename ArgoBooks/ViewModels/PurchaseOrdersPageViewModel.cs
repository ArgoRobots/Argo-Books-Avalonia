using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Helpers;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Purchase Orders page.
/// Displays and manages purchase orders to suppliers.
/// </summary>
public partial class PurchaseOrdersPageViewModel : SortablePageViewModelBase
{
    #region Responsive Header

    /// <summary>
    /// Responsive header helper for adaptive layout.
    /// </summary>
    public Helpers.ResponsiveHeaderHelper ResponsiveHeader { get; } = new();

    #endregion

    #region Statistics

    [ObservableProperty]
    private int _totalOrders;

    [ObservableProperty]
    private int _pendingOrders;

    [ObservableProperty]
    private int _onOrderCount;

    [ObservableProperty]
    private string _totalValue = "$0";

    #endregion

    #region Table Column Widths

    /// <summary>
    /// Column widths manager for the table (shared across page navigations).
    /// </summary>
    public PurchaseOrdersTableColumnWidths ColumnWidths => App.PurchaseOrdersColumnWidths;

    [ObservableProperty]
    private bool _isColumnMenuOpen;

    [ObservableProperty]
    private double _columnMenuX;

    [ObservableProperty]
    private double _columnMenuY;

    [ObservableProperty]
    private bool _showPONumberColumn = ColumnVisibilityHelper.Load("PurchaseOrders", "PONumber", true);

    [ObservableProperty]
    private bool _showDateColumn = ColumnVisibilityHelper.Load("PurchaseOrders", "Date", true);

    [ObservableProperty]
    private bool _showSupplierColumn = ColumnVisibilityHelper.Load("PurchaseOrders", "Supplier", true);

    [ObservableProperty]
    private bool _showItemsColumn = ColumnVisibilityHelper.Load("PurchaseOrders", "Items", true);

    [ObservableProperty]
    private bool _showTotalColumn = ColumnVisibilityHelper.Load("PurchaseOrders", "Total", true);

    [ObservableProperty]
    private bool _showStatusColumn = ColumnVisibilityHelper.Load("PurchaseOrders", "Status", true);

    [ObservableProperty]
    private bool _showExpectedColumn = ColumnVisibilityHelper.Load("PurchaseOrders", "Expected", true);

    partial void OnShowPONumberColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("PONumber", value); ColumnVisibilityHelper.Save("PurchaseOrders", "PONumber", value); }
    partial void OnShowDateColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Date", value); ColumnVisibilityHelper.Save("PurchaseOrders", "Date", value); }
    partial void OnShowSupplierColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Supplier", value); ColumnVisibilityHelper.Save("PurchaseOrders", "Supplier", value); }
    partial void OnShowItemsColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Items", value); ColumnVisibilityHelper.Save("PurchaseOrders", "Items", value); }
    partial void OnShowTotalColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Total", value); ColumnVisibilityHelper.Save("PurchaseOrders", "Total", value); }
    partial void OnShowStatusColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Status", value); ColumnVisibilityHelper.Save("PurchaseOrders", "Status", value); }
    partial void OnShowExpectedColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Expected", value); ColumnVisibilityHelper.Save("PurchaseOrders", "Expected", value); }

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

    [RelayCommand]
    private void ResetColumnVisibility()
    {
        ColumnVisibilityHelper.ResetPage("PurchaseOrders");
        ShowPONumberColumn = true;
        ShowDateColumn = true;
        ShowSupplierColumn = true;
        ShowItemsColumn = true;
        ShowTotalColumn = true;
        ShowStatusColumn = true;
        ShowExpectedColumn = true;
    }

    #endregion

    #region Tabs

    [ObservableProperty]
    private string _activeTab = "All";

    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>
    /// Tab options for filtering.
    /// </summary>
    public ObservableCollection<string> TabOptions { get; } = ["All", "Pending", "Approved", "On Order", "Received"];

    partial void OnActiveTabChanged(string value)
    {
        CurrentPage = 1;
        FilterOrders();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        ActiveTab = value switch
        {
            0 => "All",
            1 => "Pending",
            2 => "Approved",
            3 => "On Order",
            4 => "Received",
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
        FilterOrders();
    }

    #endregion

    #region Orders Collection

    /// <summary>
    /// All orders (unfiltered).
    /// </summary>
    private readonly List<PurchaseOrder> _allOrders = [];

    /// <summary>
    /// Orders for display in the table.
    /// </summary>
    public ObservableCollection<PurchaseOrderDisplayItem> Orders { get; } = [];

    #endregion

    #region Pagination

    [ObservableProperty]
    private string _paginationText = "0 orders";

    /// <inheritdoc />
    protected override void OnSortOrPageChanged() => FilterOrders();

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public PurchaseOrdersPageViewModel()
    {
        // Set default sort to date descending (most recent first)
        SortColumn = "Date";
        SortDirection = SortDirection.Descending;

        LoadOrders();

        // Subscribe to undo/redo state changes to refresh UI
        App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;

        // Subscribe to modal events to refresh when orders are saved
        if (App.PurchaseOrdersModalsViewModel != null)
        {
            App.PurchaseOrdersModalsViewModel.OrderSaved += OnOrderSaved;
            App.PurchaseOrdersModalsViewModel.OrderDeleted += OnOrderDeleted;
            App.PurchaseOrdersModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.PurchaseOrdersModalsViewModel.FiltersCleared += OnFiltersCleared;
        }
    }

    /// <summary>
    /// Handles filters applied event from modals.
    /// </summary>
    private void OnFiltersApplied(object? sender, EventArgs e)
    {
        CurrentPage = 1;
        FilterOrders();
    }

    /// <summary>
    /// Handles filters cleared event from modals.
    /// </summary>
    private void OnFiltersCleared(object? sender, EventArgs e)
    {
        SearchQuery = null;
        CurrentPage = 1;
        FilterOrders();
    }

    /// <summary>
    /// Handles undo/redo state changes by refreshing the orders.
    /// </summary>
    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadOrders();
    }

    /// <summary>
    /// Handles order saved events from modals.
    /// </summary>
    private void OnOrderSaved(object? sender, EventArgs e)
    {
        LoadOrders();
    }

    /// <summary>
    /// Handles order deleted events from modals.
    /// </summary>
    private void OnOrderDeleted(object? sender, EventArgs e)
    {
        LoadOrders();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads orders from the company data.
    /// </summary>
    private void LoadOrders()
    {
        _allOrders.Clear();
        Orders.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.PurchaseOrders == null)
            return;

        _allOrders.AddRange(companyData.PurchaseOrders);

        UpdateStatistics();
        FilterOrders();
    }

    /// <summary>
    /// Updates the statistics based on current data.
    /// </summary>
    private void UpdateStatistics()
    {
        TotalOrders = _allOrders.Count;
        PendingOrders = _allOrders.Count(o => o.Status == PurchaseOrderStatus.Pending);
        OnOrderCount = _allOrders.Count(o => o.Status == PurchaseOrderStatus.OnOrder || o.Status == PurchaseOrderStatus.Sent);
        TotalValue = $"${_allOrders.Sum(o => o.Total):N0}";
    }

    /// <summary>
    /// Refreshes the orders from the data source.
    /// </summary>
    [RelayCommand]
    private void RefreshOrders()
    {
        LoadOrders();
    }

    /// <summary>
    /// Filters orders based on search query, tab, and filters.
    /// </summary>
    private void FilterOrders()
    {
        Orders.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        var suppliers = companyData?.Suppliers ?? [];

        var filtered = _allOrders.ToList();

        // Apply tab filter
        if (ActiveTab != "All")
        {
            var tabStatus = ActiveTab switch
            {
                "Pending" => new[] { PurchaseOrderStatus.Pending },
                "Approved" => new[] { PurchaseOrderStatus.Approved },
                "On Order" => new[] { PurchaseOrderStatus.OnOrder, PurchaseOrderStatus.Sent },
                "Received" => new[] { PurchaseOrderStatus.Received, PurchaseOrderStatus.PartiallyReceived },
                _ => Array.Empty<PurchaseOrderStatus>()
            };

            if (tabStatus.Length > 0)
            {
                filtered = filtered.Where(o => tabStatus.Contains(o.Status)).ToList();
            }
        }

        // Get filter values from modals ViewModel
        var modals = App.PurchaseOrdersModalsViewModel;
        var startDate = modals?.FilterStartDate?.DateTime;
        var endDate = modals?.FilterEndDate?.DateTime;
        var filterSupplier = modals?.FilterSupplier ?? "All";
        var filterStatus = modals?.FilterStatus ?? "All";

        // Apply date range filter
        if (startDate.HasValue)
        {
            filtered = filtered.Where(o => o.OrderDate.Date >= startDate.Value.Date).ToList();
        }
        if (endDate.HasValue)
        {
            filtered = filtered.Where(o => o.OrderDate.Date <= endDate.Value.Date).ToList();
        }

        // Apply supplier filter
        if (!string.IsNullOrEmpty(filterSupplier) && filterSupplier != "All")
        {
            filtered = filtered.Where(o =>
            {
                var supplier = suppliers.FirstOrDefault(s => s.Id == o.SupplierId);
                return supplier?.Name == filterSupplier;
            }).ToList();
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "All")
        {
            var statusEnum = filterStatus switch
            {
                "Draft" => PurchaseOrderStatus.Draft,
                "Pending" => PurchaseOrderStatus.Pending,
                "Approved" => PurchaseOrderStatus.Approved,
                "Sent" => PurchaseOrderStatus.Sent,
                "On Order" => PurchaseOrderStatus.OnOrder,
                "Partially Received" => PurchaseOrderStatus.PartiallyReceived,
                "Received" => PurchaseOrderStatus.Received,
                "Cancelled" => PurchaseOrderStatus.Cancelled,
                _ => (PurchaseOrderStatus?)null
            };

            if (statusEnum.HasValue)
            {
                filtered = filtered.Where(o => o.Status == statusEnum.Value).ToList();
            }
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered
                .Select(o =>
                {
                    var supplier = suppliers.FirstOrDefault(s => s.Id == o.SupplierId);

                    return new
                    {
                        Order = o,
                        IdScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, o.Id),
                        PoScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, o.PoNumber),
                        SupplierScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, supplier?.Name ?? ""),
                        NotesScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, o.Notes)
                    };
                })
                .Where(x => x.IdScore >= 0 || x.PoScore >= 0 || x.SupplierScore >= 0 || x.NotesScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.IdScore, x.PoScore), Math.Max(x.SupplierScore, x.NotesScore)))
                .Select(x => x.Order)
                .ToList();
        }

        // Create display items
        var displayItems = filtered.Select(order =>
        {
            var supplier = suppliers.FirstOrDefault(s => s.Id == order.SupplierId);

            return new PurchaseOrderDisplayItem
            {
                Id = order.Id,
                PoNumber = string.IsNullOrEmpty(order.PoNumber) ? order.Id : order.PoNumber,
                OrderDate = order.OrderDate,
                DateDisplay = order.OrderDate.ToString("MMM dd, yyyy"),
                SupplierId = order.SupplierId,
                SupplierName = supplier?.Name ?? "Unknown Supplier",
                ItemCount = order.LineItems.Count,
                Subtotal = order.Subtotal,
                ShippingCost = order.ShippingCost,
                Total = order.Total,
                TotalDisplay = $"${order.Total:N2}",
                Status = order.Status,
                StatusDisplay = FormatStatus(order.Status),
                ExpectedDeliveryDate = order.ExpectedDeliveryDate,
                ExpectedDisplay = order.ExpectedDeliveryDate.ToString("MMM dd, yyyy"),
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };
        }).ToList();

        // Apply sorting
        if (string.IsNullOrWhiteSpace(SearchQuery) || SortDirection != SortDirection.None)
        {
            displayItems = displayItems.ApplySort(
                SortColumn,
                SortDirection,
                new Dictionary<string, Func<PurchaseOrderDisplayItem, object?>>
                {
                    ["PONumber"] = o => o.PoNumber,
                    ["Date"] = o => o.OrderDate,
                    ["Supplier"] = o => o.SupplierName,
                    ["Items"] = o => o.ItemCount,
                    ["Total"] = o => o.Total,
                    ["Status"] = o => o.StatusDisplay,
                    ["Expected"] = o => o.ExpectedDeliveryDate
                },
                o => o.OrderDate);
        }

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination and add to collection
        var pagedOrders = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedOrders)
        {
            Orders.Add(item);
        }
    }

    private static string FormatStatus(PurchaseOrderStatus status)
    {
        return status switch
        {
            PurchaseOrderStatus.Draft => "Draft",
            PurchaseOrderStatus.Pending => "Pending",
            PurchaseOrderStatus.Approved => "Approved",
            PurchaseOrderStatus.Sent => "Sent",
            PurchaseOrderStatus.OnOrder => "On Order",
            PurchaseOrderStatus.PartiallyReceived => "Partial",
            PurchaseOrderStatus.Received => "Received",
            PurchaseOrderStatus.Cancelled => "Cancelled",
            _ => status.ToString()
        };
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
            totalCount, CurrentPage, PageSize, TotalPages, "order");
    }

    #endregion

    #region Modal Commands

    /// <summary>
    /// Opens the Create Purchase Order modal.
    /// </summary>
    [RelayCommand]
    private void OpenAddModal()
    {
        App.PurchaseOrdersModalsViewModel?.OpenAddModal();
    }

    /// <summary>
    /// Opens the View Order modal.
    /// </summary>
    [RelayCommand]
    private void ViewOrder(PurchaseOrderDisplayItem? item)
    {
        if (item == null) return;
        App.PurchaseOrdersModalsViewModel?.OpenViewModal(item);
    }

    /// <summary>
    /// Opens the Edit Order modal.
    /// </summary>
    [RelayCommand]
    private void EditOrder(PurchaseOrderDisplayItem? item)
    {
        if (item == null) return;
        App.PurchaseOrdersModalsViewModel?.OpenEditModal(item);
    }

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void OpenDeleteConfirm(PurchaseOrderDisplayItem? item)
    {
        if (item == null) return;
        App.PurchaseOrdersModalsViewModel?.OpenDeleteConfirm(item);
    }

    /// <summary>
    /// Approves a purchase order.
    /// </summary>
    [RelayCommand]
    private void ApproveOrder(PurchaseOrderDisplayItem? item)
    {
        if (item == null) return;

        var companyData = App.CompanyManager?.CompanyData;

        var order = companyData?.PurchaseOrders.FirstOrDefault(o => o.Id == item.Id);
        if (order == null) return;

        var oldStatus = order.Status;
        order.Status = PurchaseOrderStatus.Approved;
        order.UpdatedAt = DateTime.UtcNow;
        companyData?.MarkAsModified();

        // Record undo action
        App.UndoRedoManager.RecordAction(new Services.DelegateAction(
            $"Approve order '{item.PoNumber}'",
            () =>
            {
                order.Status = oldStatus;
                companyData?.MarkAsModified();
                LoadOrders();
            },
            () =>
            {
                order.Status = PurchaseOrderStatus.Approved;
                companyData?.MarkAsModified();
                LoadOrders();
            }));

        LoadOrders();
    }

    /// <summary>
    /// Marks an order as received.
    /// </summary>
    [RelayCommand]
    private void ReceiveOrder(PurchaseOrderDisplayItem? item)
    {
        if (item == null) return;
        App.PurchaseOrdersModalsViewModel?.OpenReceiveModal(item);
    }

    #endregion

    #region Filter Modal

    /// <summary>
    /// Opens the filter modal.
    /// </summary>
    [RelayCommand]
    private void OpenFilterModal()
    {
        App.PurchaseOrdersModalsViewModel?.OpenFilterModal();
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
/// Display model for purchase orders in the UI.
/// </summary>
public partial class PurchaseOrderDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _poNumber = string.Empty;

    [ObservableProperty]
    private DateTime _orderDate;

    [ObservableProperty]
    private string _dateDisplay = string.Empty;

    [ObservableProperty]
    private string _supplierId = string.Empty;

    [ObservableProperty]
    private string _supplierName = string.Empty;

    [ObservableProperty]
    private int _itemCount;

    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _shippingCost;

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private string _totalDisplay = string.Empty;

    [ObservableProperty]
    private PurchaseOrderStatus _status;

    [ObservableProperty]
    private string _statusDisplay = string.Empty;

    [ObservableProperty]
    private DateTime _expectedDeliveryDate;

    [ObservableProperty]
    private string _expectedDisplay = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private DateTime _updatedAt;

    /// <summary>
    /// Gets the status badge color based on status.
    /// </summary>
    public string StatusColor => Status switch
    {
        PurchaseOrderStatus.Draft => "#6B7280",
        PurchaseOrderStatus.Pending => "#F59E0B",
        PurchaseOrderStatus.Approved => "#3B82F6",
        PurchaseOrderStatus.Sent => "#8B5CF6",
        PurchaseOrderStatus.OnOrder => "#8B5CF6",
        PurchaseOrderStatus.PartiallyReceived => "#F59E0B",
        PurchaseOrderStatus.Received => "#22C55E",
        PurchaseOrderStatus.Cancelled => "#EF4444",
        _ => "#6B7280"
    };

    /// <summary>
    /// Gets the status badge background color.
    /// </summary>
    public string StatusBackground => Status switch
    {
        PurchaseOrderStatus.Draft => "#F3F4F6",
        PurchaseOrderStatus.Pending => "#FEF3C7",
        PurchaseOrderStatus.Approved => "#DBEAFE",
        PurchaseOrderStatus.Sent => "#EDE9FE",
        PurchaseOrderStatus.OnOrder => "#EDE9FE",
        PurchaseOrderStatus.PartiallyReceived => "#FEF3C7",
        PurchaseOrderStatus.Received => "#DCFCE7",
        PurchaseOrderStatus.Cancelled => "#FEE2E2",
        _ => "#F3F4F6"
    };

    /// <summary>
    /// Items count display.
    /// </summary>
    public string ItemsDisplay => ItemCount == 1 ? "1 item" : $"{ItemCount} items";

    /// <summary>
    /// Whether the order can be approved.
    /// </summary>
    public bool CanApprove => Status == PurchaseOrderStatus.Pending || Status == PurchaseOrderStatus.Draft;

    /// <summary>
    /// Whether the order can be received.
    /// </summary>
    public bool CanReceive => Status == PurchaseOrderStatus.OnOrder || Status == PurchaseOrderStatus.Sent || Status == PurchaseOrderStatus.Approved;

    /// <summary>
    /// Whether the order can be edited.
    /// </summary>
    public bool CanEdit => Status == PurchaseOrderStatus.Draft || Status == PurchaseOrderStatus.Pending;
}
