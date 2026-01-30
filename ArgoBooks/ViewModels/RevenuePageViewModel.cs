using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Revenue page displaying revenue transactions.
/// </summary>
public partial class RevenuePageViewModel : SortablePageViewModelBase
{
    public Helpers.ResponsiveHeaderHelper ResponsiveHeader { get; } = new();

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

    #region Column Visibility

    /// <summary>
    /// Column widths manager for the table (shared across page navigations).
    /// </summary>
    public RevenueTableColumnWidths ColumnWidths => App.RevenueColumnWidths;

    [ObservableProperty]
    private bool _isColumnMenuOpen;

    [ObservableProperty]
    private double _columnMenuX;

    [ObservableProperty]
    private double _columnMenuY;

    [ObservableProperty]
    private bool _showIdColumn = true;

    [ObservableProperty]
    private bool _showAccountantColumn; // No Accountant column in Revenue UI

    [ObservableProperty]
    private bool _showCustomerColumn = true;

    [ObservableProperty]
    private bool _showProductColumn = true;

    [ObservableProperty]
    private bool _showDateColumn = true;

    [ObservableProperty]
    private bool _showQuantityColumn;

    [ObservableProperty]
    private bool _showUnitPriceColumn;

    [ObservableProperty]
    private bool _showAmountColumn;

    [ObservableProperty]
    private bool _showTaxColumn;

    [ObservableProperty]
    private bool _showShippingColumn;

    [ObservableProperty]
    private bool _showDiscountColumn;

    [ObservableProperty]
    private bool _showTotalColumn = true;

    [ObservableProperty]
    private bool _showStatusColumn = true;

    [ObservableProperty]
    private bool _showReceiptColumn = true;

    partial void OnShowIdColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Id", value);
    partial void OnShowAccountantColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Accountant", value);
    partial void OnShowCustomerColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Customer", value);
    partial void OnShowProductColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Product", value);
    partial void OnShowDateColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Date", value);
    partial void OnShowQuantityColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Quantity", value);
    partial void OnShowUnitPriceColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("UnitPrice", value);
    partial void OnShowAmountColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Amount", value);
    partial void OnShowTaxColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Tax", value);
    partial void OnShowShippingColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Shipping", value);
    partial void OnShowDiscountColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Discount", value);
    partial void OnShowTotalColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Total", value);
    partial void OnShowReceiptColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Receipt", value);
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

    #region Revenue Collection

    private readonly List<Revenue> _allRevenue = [];

    public ObservableCollection<RevenueDisplayItem> Revenue { get; } = [];

    #endregion

    #region Pagination

    [ObservableProperty]
    private string _paginationText = "0 sales";

    /// <inheritdoc />
    protected override void OnSortOrPageChanged() => FilterRevenue();

    #endregion

    #region Constructor

    public RevenuePageViewModel()
    {
        // Set default sort values for revenue
        SortColumn = "Date";
        SortDirection = SortDirection.Descending;

        InitializeColumnVisibility();
        LoadRevenue();

        // Subscribe to undo/redo state changes to refresh UI
        App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;

        // Subscribe to revenue modal events to refresh data
        if (App.RevenueModalsViewModel != null)
        {
            App.RevenueModalsViewModel.RevenueSaved += OnRevenueSaved;
            App.RevenueModalsViewModel.RevenueDeleted += OnRevenueDeleted;
            App.RevenueModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.RevenueModalsViewModel.FiltersCleared += OnFiltersCleared;
        }

        // Subscribe to date format changes to refresh date display
        DateFormatService.DateFormatChanged += (_, _) => FilterRevenue();

        // Subscribe to currency changes to refresh currency display
        CurrencyService.CurrencyChanged += (_, _) =>
        {
            UpdateStatistics();
            FilterRevenue();
        };
    }

    private void InitializeColumnVisibility()
    {
        // Set initial visibility for columns
        ColumnWidths.SetColumnVisibility("Id", ShowIdColumn);
        ColumnWidths.SetColumnVisibility("Accountant", ShowAccountantColumn);
        ColumnWidths.SetColumnVisibility("Customer", ShowCustomerColumn);
        ColumnWidths.SetColumnVisibility("Product", ShowProductColumn);
        ColumnWidths.SetColumnVisibility("Date", ShowDateColumn);
        ColumnWidths.SetColumnVisibility("Quantity", ShowQuantityColumn);
        ColumnWidths.SetColumnVisibility("UnitPrice", ShowUnitPriceColumn);
        ColumnWidths.SetColumnVisibility("Amount", ShowAmountColumn);
        ColumnWidths.SetColumnVisibility("Tax", ShowTaxColumn);
        ColumnWidths.SetColumnVisibility("Shipping", ShowShippingColumn);
        ColumnWidths.SetColumnVisibility("Discount", ShowDiscountColumn);
        ColumnWidths.SetColumnVisibility("Total", ShowTotalColumn);
        ColumnWidths.SetColumnVisibility("Receipt", ShowReceiptColumn);
        ColumnWidths.SetColumnVisibility("Status", ShowStatusColumn);
        ColumnWidths.SetColumnVisibility("Actions", true);

        ColumnWidths.RecalculateWidths();
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
        if (companyData?.Revenues == null)
            return;

        _allRevenue.AddRange(companyData.Revenues);
        UpdateStatistics();
        FilterRevenue();
    }

    private void UpdateStatistics()
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        // Total monthly revenue (in USD, then convert to display currency)
        var monthlyTotalUSD = _allRevenue
            .Where(s => s.Date >= startOfMonth)
            .Sum(s => s.EffectiveTotalUSD);
        TotalMonthlyRevenue = CurrencyService.FormatFromUSD(monthlyTotalUSD, now);

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
        ReturnsCount = companyData?.Returns.Count(r =>
            _allRevenue.Any(s => s.Id == r.OriginalTransactionId)) ?? 0;
    }

    [RelayCommand]
    private void RefreshRevenue()
    {
        LoadRevenue();
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
                    Revenue = s,
                    IdScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, s.Id),
                    DescScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, s.Description),
                    CustomerScore = LevenshteinDistance.ComputeSearchScore(SearchQuery,
                        companyData?.GetCustomer(s.CustomerId ?? "")?.Name ?? "")
                })
                .Where(x => x.IdScore >= 0 || x.DescScore >= 0 || x.CustomerScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.IdScore, x.DescScore), x.CustomerScore))
                .Select(x => x.Revenue)
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

        // Apply category filter (via line item product category)
        if (!string.IsNullOrEmpty(FilterCategoryId))
        {
            filtered = filtered.Where(s =>
            {
                var productId = s.LineItems.FirstOrDefault()?.ProductId;
                var product = productId != null ? companyData?.GetProduct(productId) : null;
                return product?.CategoryId == FilterCategoryId;
            }).ToList();
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
        var displayItems = filtered.Select(revenue =>
        {
            var customer = companyData?.GetCustomer(revenue.CustomerId ?? "");
            var productId = revenue.LineItems.FirstOrDefault()?.ProductId;
            var product = productId != null ? companyData?.GetProduct(productId) : null;
            var categoryId = product?.CategoryId;
            var category = categoryId != null ? companyData?.GetCategory(categoryId) : null;
            var accountant = companyData?.GetAccountant(revenue.AccountantId ?? "");
            var statusDisplay = GetStatusDisplay(revenue, companyData);

            var hasReceipt = !string.IsNullOrEmpty(revenue.ReceiptId);
            var receiptFilePath = revenue.ReferenceNumber;

            return new RevenueDisplayItem
            {
                Id = revenue.Id,
                AccountantName = accountant?.Name ?? "System",
                CustomerName = customer?.Name ?? "-",
                ProductDescription = revenue.Description,
                CategoryName = category?.Name ?? "-",
                Date = revenue.Date,
                Total = revenue.Total,
                TotalUSD = revenue.EffectiveTotalUSD,
                AmountUSD = revenue.Amount > 0 ? revenue.EffectiveTotalUSD * (revenue.Amount / revenue.Total) : 0,
                TaxAmountUSD = revenue.TaxAmountUSD > 0 ? revenue.TaxAmountUSD : revenue.TaxAmount,
                ShippingCostUSD = revenue.EffectiveShippingCostUSD,
                DiscountUSD = revenue.DiscountUSD > 0 ? revenue.DiscountUSD : revenue.Discount,
                UnitPriceUSD = revenue.EffectiveUnitPriceUSD,
                StatusDisplay = statusDisplay,
                Notes = revenue.Notes,
                CustomerId = revenue.CustomerId,
                CategoryId = categoryId,
                Amount = revenue.Amount,
                TaxAmount = revenue.TaxAmount,
                TaxRate = revenue.TaxRate,
                ShippingCost = revenue.ShippingCost,
                Discount = revenue.Discount,
                Quantity = (int)revenue.Quantity,
                UnitPrice = revenue.UnitPrice,
                PaymentMethod = revenue.PaymentMethod,
                HasReceipt = hasReceipt,
                ReceiptFilePath = receiptFilePath,
                IsHighlighted = revenue.Id == HighlightTransactionId
            };
        }).ToList();

        // Apply sorting (only if not searching, since search has its own relevance sorting)
        if (string.IsNullOrWhiteSpace(SearchQuery) || SortDirection != SortDirection.None)
        {
            displayItems = displayItems.ApplySort(
                SortColumn,
                SortDirection,
                new Dictionary<string, Func<RevenueDisplayItem, object?>>
                {
                    ["Id"] = r => r.Id,
                    ["Accountant"] = r => r.AccountantName,
                    ["Customer"] = r => r.CustomerName,
                    ["Product"] = r => r.ProductDescription,
                    ["Category"] = r => r.CategoryName,
                    ["Date"] = r => r.Date,
                    ["Total"] = r => r.Total,
                    ["Status"] = r => r.StatusDisplay
                },
                r => r.Date);
        }

        // Navigate to highlighted item if set (from dashboard click)
        NavigateToHighlightedItem(displayItems, x => x.Id);

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

    private static string GetStatusDisplay(Revenue revenue, Core.Data.CompanyData? companyData)
    {
        // Check for lost/damaged related to this revenue
        var relatedLostDamaged = companyData?.LostDamaged.FirstOrDefault(ld => ld.InventoryItemId == revenue.Id);
        if (relatedLostDamaged != null)
        {
            return "Lost/Damaged";
        }

        // Check for returns related to this revenue
        var relatedReturn = companyData?.Returns.FirstOrDefault(r => r.OriginalTransactionId == revenue.Id);

        if (relatedReturn is { Status: ReturnStatus.Completed })
        {
            return "Returned";
        }

        // Default status
        return "Completed";
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
            totalCount, CurrentPage, PageSize, TotalPages, "revenue");
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
    private void MarkAsLostDamaged(RevenueDisplayItem? item)
    {
        App.RevenueModalsViewModel?.OpenMarkAsLostDamagedModal(item);
    }

    [RelayCommand]
    private void MarkAsReturned(RevenueDisplayItem? item)
    {
        App.RevenueModalsViewModel?.OpenMarkAsReturnedModal(item);
    }

    [RelayCommand]
    private void UndoLostDamaged(RevenueDisplayItem? item)
    {
        App.RevenueModalsViewModel?.OpenUndoLostDamagedModal(item);
    }

    [RelayCommand]
    private void UndoReturn(RevenueDisplayItem? item)
    {
        App.RevenueModalsViewModel?.OpenUndoReturnedModal(item);
    }

    [RelayCommand]
    private void OpenFilterModal()
    {
        App.RevenueModalsViewModel?.OpenFilterModal();
    }

    #endregion

    #region Receipt Preview

    [RelayCommand]
    private void ViewReceipt(RevenueDisplayItem? item)
    {
        if (item == null || !item.HasReceipt)
            return;

        // Try to get the receipt path - check if file exists, otherwise load from stored data
        var receiptPath = GetReceiptImagePath(item.Id);
        if (string.IsNullOrEmpty(receiptPath))
            return;

        // Use the shared receipt viewer modal
        App.ReceiptViewerModal?.Show(receiptPath, item.Id);
    }

    private static string? GetReceiptImagePath(string revenueId)
    {
        // Always load from company file to ensure consistency
        var companyData = App.CompanyManager?.CompanyData;

        var revenue = companyData?.Revenues.FirstOrDefault(s => s.Id == revenueId);
        if (revenue == null || string.IsNullOrEmpty(revenue.ReceiptId)) return null;

        var receipt = companyData?.Receipts.FirstOrDefault(r => r.Id == revenue.ReceiptId);
        if (receipt == null || string.IsNullOrEmpty(receipt.FileData)) return null;

        try
        {
            // Create temp file from Base64 data stored in company file
            var tempDir = Path.Combine(Path.GetTempPath(), "ArgoBooks", "Receipts");
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, receipt.FileName);
            var bytes = Convert.FromBase64String(receipt.FileData);
            File.WriteAllBytes(tempPath, bytes);
            return tempPath;
        }
        catch
        {
            return null;
        }
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
    private decimal _totalUSD;

    [ObservableProperty]
    private decimal _amountUSD;

    [ObservableProperty]
    private decimal _taxAmountUSD;

    [ObservableProperty]
    private decimal _shippingCostUSD;

    [ObservableProperty]
    private decimal _discountUSD;

    [ObservableProperty]
    private decimal _unitPriceUSD;

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
    private decimal _taxRate;

    [ObservableProperty]
    private decimal _shippingCost;

    [ObservableProperty]
    private decimal _discount;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private PaymentMethod _paymentMethod;

    public string DateFormatted => DateFormatService.Format(Date);
    public string TotalFormatted => CurrencyService.FormatFromUSD(TotalUSD, Date);
    public string AmountFormatted => CurrencyService.FormatFromUSD(AmountUSD, Date);
    public string TaxAmountFormatted => CurrencyService.FormatFromUSD(TaxAmountUSD, Date);
    public string TaxRateFormatted => $"{TaxRate:N1}%";
    public string ShippingCostFormatted => CurrencyService.FormatFromUSD(ShippingCostUSD, Date);
    public string DiscountFormatted => $"-{CurrencyService.FormatFromUSD(DiscountUSD, Date)}";
    public string UnitPriceFormatted => CurrencyService.FormatFromUSD(UnitPriceUSD, Date);

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
    public bool IsLostDamaged => StatusDisplay == "Lost/Damaged";
    public bool CanMarkAsReturned => !IsReturned && !IsLostDamaged;
    public bool CanMarkAsLostDamaged => !IsReturned && !IsLostDamaged;

    [ObservableProperty]
    private bool _hasReceipt;

    [ObservableProperty]
    private string _receiptFilePath = string.Empty;

    [ObservableProperty]
    private bool _isHighlighted;
}
