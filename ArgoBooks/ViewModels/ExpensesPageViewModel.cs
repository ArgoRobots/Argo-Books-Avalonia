using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using ArgoBooks.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Expenses page displaying expense transactions.
/// </summary>
public partial class ExpensesPageViewModel : SortablePageViewModelBase
{
    #region Statistics

    [ObservableProperty]
    private string _totalMonthlyExpenses = "$0.00";

    [ObservableProperty]
    private int _transactionCount;

    [ObservableProperty]
    private int _receiptsOnFile;

    [ObservableProperty]
    private int _returnsCount;

    #endregion

    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterExpenses();
    }

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string? _filterSupplierId;

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

    [ObservableProperty]
    private string _filterReceiptStatus = "All";

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
    public TableColumnWidths ColumnWidths => App.ExpensesColumnWidths;

    [ObservableProperty]
    private bool _showIdColumn = ColumnVisibilityHelper.Load("Expenses", "Id", true);

    [ObservableProperty]
    private bool _showAccountantColumn = ColumnVisibilityHelper.Load("Expenses", "Accountant", false); // No Accountant column in Expenses UI

    [ObservableProperty]
    private bool _showProductColumn = ColumnVisibilityHelper.Load("Expenses", "Product", true);

    [ObservableProperty]
    private bool _showSupplierColumn = ColumnVisibilityHelper.Load("Expenses", "Supplier", true);

    [ObservableProperty]
    private bool _showDateColumn = ColumnVisibilityHelper.Load("Expenses", "Date", true);

    [ObservableProperty]
    private bool _showQuantityColumn = ColumnVisibilityHelper.Load("Expenses", "Quantity", false);

    [ObservableProperty]
    private bool _showAmountColumn = ColumnVisibilityHelper.Load("Expenses", "Amount", false);

    [ObservableProperty]
    private bool _showTaxColumn = ColumnVisibilityHelper.Load("Expenses", "Tax", false);

    [ObservableProperty]
    private bool _showShippingColumn = ColumnVisibilityHelper.Load("Expenses", "Shipping", false);

    [ObservableProperty]
    private bool _showDiscountColumn = ColumnVisibilityHelper.Load("Expenses", "Discount", false);

    [ObservableProperty]
    private bool _showFeeColumn = ColumnVisibilityHelper.Load("Expenses", "Fee", false);

    [ObservableProperty]
    private bool _showTotalColumn = ColumnVisibilityHelper.Load("Expenses", "Total", true);

    [ObservableProperty]
    private bool _showReceiptColumn = ColumnVisibilityHelper.Load("Expenses", "Receipt", true);

    [ObservableProperty]
    private bool _showStatusColumn = ColumnVisibilityHelper.Load("Expenses", "Status", true);

    partial void OnShowIdColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Id", value); ColumnVisibilityHelper.Save("Expenses", "Id", value); }
    partial void OnShowAccountantColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Accountant", value); ColumnVisibilityHelper.Save("Expenses", "Accountant", value); }
    partial void OnShowProductColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Product", value); ColumnVisibilityHelper.Save("Expenses", "Product", value); }
    partial void OnShowSupplierColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Supplier", value); ColumnVisibilityHelper.Save("Expenses", "Supplier", value); }
    partial void OnShowDateColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Date", value); ColumnVisibilityHelper.Save("Expenses", "Date", value); }
    partial void OnShowQuantityColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Quantity", value); ColumnVisibilityHelper.Save("Expenses", "Quantity", value); }
    partial void OnShowAmountColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Amount", value); ColumnVisibilityHelper.Save("Expenses", "Amount", value); }
    partial void OnShowTaxColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Tax", value); ColumnVisibilityHelper.Save("Expenses", "Tax", value); }
    partial void OnShowShippingColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Shipping", value); ColumnVisibilityHelper.Save("Expenses", "Shipping", value); }
    partial void OnShowDiscountColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Discount", value); ColumnVisibilityHelper.Save("Expenses", "Discount", value); }
    partial void OnShowFeeColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Fee", value); ColumnVisibilityHelper.Save("Expenses", "Fee", value); }
    partial void OnShowTotalColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Total", value); ColumnVisibilityHelper.Save("Expenses", "Total", value); }
    partial void OnShowReceiptColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Receipt", value); ColumnVisibilityHelper.Save("Expenses", "Receipt", value); }
    partial void OnShowStatusColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Status", value); ColumnVisibilityHelper.Save("Expenses", "Status", value); }

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
        ColumnVisibilityHelper.ResetPage("Expenses");
        ShowIdColumn = true;
        ShowAccountantColumn = false;
        ShowProductColumn = true;
        ShowSupplierColumn = true;
        ShowDateColumn = true;
        ShowQuantityColumn = false;
        ShowAmountColumn = false;
        ShowTaxColumn = false;
        ShowShippingColumn = false;
        ShowDiscountColumn = false;
        ShowFeeColumn = false;
        ShowTotalColumn = true;
        ShowReceiptColumn = true;
        ShowStatusColumn = true;
    }

    #endregion

    #region Responsive Layout

    /// <summary>
    /// Responsive header helper for adaptive layout.
    /// </summary>
    public Helpers.ResponsiveHeaderHelper ResponsiveHeader { get; } = new();

    #endregion

    #region Expenses Collection

    private readonly List<Expense> _allExpenses = [];

    public ObservableCollection<ExpenseDisplayItem> Expenses { get; } = [];

    #endregion

    #region Pagination

    [ObservableProperty]
    private string _paginationText = "0 expenses";

    /// <inheritdoc />
    protected override void OnSortOrPageChanged() => FilterExpenses();

    #endregion

    #region Constructor

    public ExpensesPageViewModel()
    {
        // Set default sort values for expenses
        SortColumn = "Date";
        SortDirection = SortDirection.Descending;

        // Initialize column visibility settings
        InitializeColumnVisibility();

        LoadExpenses();

        // Subscribe to undo/redo state changes to refresh UI
        App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;

        // Subscribe to expense modal events to refresh data
        if (App.ExpenseModalsViewModel != null)
        {
            App.ExpenseModalsViewModel.ExpenseSaved += OnExpenseSaved;
            App.ExpenseModalsViewModel.ExpenseDeleted += OnExpenseDeleted;
            App.ExpenseModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.ExpenseModalsViewModel.FiltersCleared += OnFiltersCleared;
        }

        // Subscribe to date format changes to refresh date display
        DateFormatService.DateFormatChanged += (_, _) => FilterExpenses();

        // Subscribe to currency changes to refresh currency display
        CurrencyService.CurrencyChanged += (_, _) =>
        {
            UpdateStatistics();
            FilterExpenses();
        };
    }

    private void InitializeColumnVisibility()
    {
        // Set initial visibility for columns that are hidden by default
        ColumnWidths.SetColumnVisibility("Id", ShowIdColumn);
        ColumnWidths.SetColumnVisibility("Accountant", ShowAccountantColumn);
        ColumnWidths.SetColumnVisibility("Product", ShowProductColumn);
        ColumnWidths.SetColumnVisibility("Supplier", ShowSupplierColumn);
        ColumnWidths.SetColumnVisibility("Date", ShowDateColumn);
        ColumnWidths.SetColumnVisibility("Quantity", ShowQuantityColumn);
        ColumnWidths.SetColumnVisibility("Amount", ShowAmountColumn);
        ColumnWidths.SetColumnVisibility("Tax", ShowTaxColumn);
        ColumnWidths.SetColumnVisibility("Shipping", ShowShippingColumn);
        ColumnWidths.SetColumnVisibility("Discount", ShowDiscountColumn);
        ColumnWidths.SetColumnVisibility("Fee", ShowFeeColumn);
        ColumnWidths.SetColumnVisibility("Total", ShowTotalColumn);
        ColumnWidths.SetColumnVisibility("Receipt", ShowReceiptColumn);
        ColumnWidths.SetColumnVisibility("Status", ShowStatusColumn);
        ColumnWidths.SetColumnVisibility("Actions", true); // Actions column is always visible

        // Initial calculation
        ColumnWidths.RecalculateWidths();
    }

    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadExpenses();
    }

    private void OnExpenseSaved(object? sender, EventArgs e)
    {
        LoadExpenses();
    }

    private void OnExpenseDeleted(object? sender, EventArgs e)
    {
        LoadExpenses();
    }

    private void OnFiltersApplied(object? sender, EventArgs e)
    {
        var modals = App.ExpenseModalsViewModel;
        if (modals != null)
        {
            FilterStatus = modals.FilterStatus;
            FilterSupplierId = modals.FilterSupplierId;
            FilterCategoryId = modals.FilterCategoryId;
            FilterAmountMin = modals.FilterAmountMin;
            FilterAmountMax = modals.FilterAmountMax;
            FilterDateFrom = modals.FilterDateFrom;
            FilterDateTo = modals.FilterDateTo;
            FilterReceiptStatus = modals.FilterReceiptStatus;
        }
        CurrentPage = 1;
        FilterExpenses();
    }

    private void OnFiltersCleared(object? sender, EventArgs e)
    {
        FilterStatus = "All";
        FilterSupplierId = null;
        FilterCategoryId = null;
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterDateFrom = null;
        FilterDateTo = null;
        FilterReceiptStatus = "All";
        SearchQuery = null;
        CurrentPage = 1;
        FilterExpenses();
    }

    #endregion

    #region Data Loading

    private void LoadExpenses()
    {
        _allExpenses.Clear();
        Expenses.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Expenses == null)
            return;

        _allExpenses.AddRange(companyData.Expenses);
        UpdateStatistics();
        FilterExpenses();
    }

    private void UpdateStatistics()
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        // Total monthly expenses (in USD, then convert to display currency)
        var monthlyTotalUSD = _allExpenses
            .Where(p => p.Date >= startOfMonth)
            .Sum(p => p.EffectiveTotalUSD);
        TotalMonthlyExpenses = CurrencyService.FormatFromUSD(monthlyTotalUSD, now);

        // Transaction count
        TransactionCount = _allExpenses.Count;

        // Receipts on file
        ReceiptsOnFile = _allExpenses.Count(p => !string.IsNullOrEmpty(p.ReceiptId));

        // Returns count (linked to returns data)
        var companyData = App.CompanyManager?.CompanyData;
        ReturnsCount = companyData?.Returns.Count(r =>
            _allExpenses.Any(p => p.Id == r.OriginalTransactionId)) ?? 0;
    }

    [RelayCommand]
    private void RefreshExpenses()
    {
        LoadExpenses();
    }

    private void FilterExpenses()
    {
        Expenses.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        var filtered = _allExpenses.ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered
                .Select(p => new
                {
                    Expense = p,
                    IdScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, p.Id),
                    DescScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, p.Description),
                    SupplierScore = LevenshteinDistance.ComputeSearchScore(SearchQuery,
                        companyData?.GetSupplier(p.SupplierId ?? "")?.Name ?? "")
                })
                .Where(x => x.IdScore >= 0 || x.DescScore >= 0 || x.SupplierScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.IdScore, x.DescScore), x.SupplierScore))
                .Select(x => x.Expense)
                .ToList();
        }

        // Apply status filter
        if (FilterStatus != "All")
        {
            filtered = filtered.Where(p => GetStatusDisplay(p, companyData) == FilterStatus).ToList();
        }

        // Apply supplier filter
        if (!string.IsNullOrEmpty(FilterSupplierId))
        {
            filtered = filtered.Where(p => p.SupplierId == FilterSupplierId).ToList();
        }

        // Apply category filter (via line item product category)
        if (!string.IsNullOrEmpty(FilterCategoryId))
        {
            filtered = filtered.Where(p =>
            {
                var productId = p.LineItems.FirstOrDefault()?.ProductId;
                var product = productId != null ? companyData?.GetProduct(productId) : null;
                return product?.CategoryId == FilterCategoryId;
            }).ToList();
        }

        // Apply amount filter
        if (decimal.TryParse(FilterAmountMin, out var minAmount))
        {
            filtered = filtered.Where(p => p.Total >= minAmount).ToList();
        }
        if (decimal.TryParse(FilterAmountMax, out var maxAmount))
        {
            filtered = filtered.Where(p => p.Total <= maxAmount).ToList();
        }

        // Apply date filter
        if (FilterDateFrom.HasValue)
        {
            filtered = filtered.Where(p => p.Date >= FilterDateFrom.Value.DateTime).ToList();
        }
        if (FilterDateTo.HasValue)
        {
            filtered = filtered.Where(p => p.Date <= FilterDateTo.Value.DateTime).ToList();
        }

        // Apply receipt status filter
        if (FilterReceiptStatus != "All")
        {
            filtered = FilterReceiptStatus switch
            {
                "With Receipt" => filtered.Where(p => !string.IsNullOrEmpty(p.ReceiptId)).ToList(),
                "No Receipt" => filtered.Where(p => string.IsNullOrEmpty(p.ReceiptId)).ToList(),
                _ => filtered
            };
        }

        // Create display items
        var displayItems = filtered.Select(purchase =>
        {
            var supplier = companyData?.GetSupplier(purchase.SupplierId ?? "");
            var productId = purchase.LineItems.FirstOrDefault()?.ProductId;
            var product = productId != null ? companyData?.GetProduct(productId) : null;
            var categoryId = product?.CategoryId;
            var category = categoryId != null ? companyData?.GetCategory(categoryId) : null;
            var accountant = companyData?.GetAccountant(purchase.AccountantId ?? "");
            var statusDisplay = GetStatusDisplay(purchase, companyData);
            var (productName, productMoreText) = FormatProductDescription(purchase);
            var hasReceipt = !string.IsNullOrEmpty(purchase.ReceiptId);
            var receipt = hasReceipt ? companyData?.Receipts.FirstOrDefault(r => r.Id == purchase.ReceiptId) : null;
            var receiptFilePath = receipt?.OriginalFilePath ?? string.Empty;

            return new ExpenseDisplayItem
            {
                Id = purchase.Id,
                AccountantName = accountant?.Name ?? "System",
                ProductDescription = productName,
                ProductMoreText = productMoreText,
                CategoryName = category?.Name ?? "-",
                SupplierName = supplier?.Name ?? "-",
                Date = purchase.Date,
                Total = purchase.Total,
                TotalUSD = purchase.EffectiveTotalUSD,
                AmountUSD = purchase.Amount > 0 && purchase.Total > 0 ? purchase.EffectiveTotalUSD * (purchase.Amount / purchase.Total) : 0,
                TaxAmountUSD = purchase.TaxAmountUSD > 0 ? purchase.TaxAmountUSD : purchase.TaxAmount,
                ShippingCostUSD = purchase.EffectiveShippingCostUSD,
                DiscountUSD = purchase.DiscountUSD > 0 ? purchase.DiscountUSD : purchase.Discount,
                FeeUSD = purchase.FeeUSD > 0 ? purchase.FeeUSD : purchase.Fee,
                UnitPriceUSD = purchase.EffectiveUnitPriceUSD,
                HasReceipt = hasReceipt,
                ReceiptFilePath = receiptFilePath,
                StatusDisplay = statusDisplay,
                Notes = purchase.Notes,
                SupplierId = purchase.SupplierId,
                CategoryId = categoryId,
                Amount = purchase.Amount,
                TaxAmount = purchase.TaxAmount,
                TaxRate = purchase.TaxRate,
                ShippingCost = purchase.ShippingCost,
                Discount = purchase.Discount,
                Fee = purchase.Fee,
                Quantity = (int)purchase.Quantity,
                UnitPrice = purchase.UnitPrice,
                PaymentMethod = purchase.PaymentMethod,
                IsHighlighted = purchase.Id == HighlightTransactionId
            };
        }).ToList();

        // Apply sorting (only if not searching, since search has its own relevance sorting)
        if (string.IsNullOrWhiteSpace(SearchQuery) || SortDirection != SortDirection.None)
        {
            displayItems = displayItems.ApplySort(
                SortColumn,
                SortDirection,
                new Dictionary<string, Func<ExpenseDisplayItem, object?>>
                {
                    ["Id"] = e => e.Id,
                    ["Accountant"] = e => e.AccountantName,
                    ["Product"] = e => e.ProductDescription,
                    ["Category"] = e => e.CategoryName,
                    ["Supplier"] = e => e.SupplierName,
                    ["Date"] = e => e.Date,
                    ["Total"] = e => e.Total,
                    ["Status"] = e => e.StatusDisplay
                },
                e => e.Date);
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
        var pagedExpenses = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedExpenses)
        {
            Expenses.Add(item);
        }
    }

    private static (string name, string moreText) FormatProductDescription(Expense purchase)
    {
        if (purchase.LineItems.Count <= 1)
            return (purchase.Description, string.Empty);

        var firstName = purchase.LineItems[0].Description;
        if (string.IsNullOrEmpty(firstName))
            firstName = purchase.Description.Split(',')[0].Trim();

        var remaining = purchase.LineItems.Count - 1;
        return (firstName, $" +{remaining} more");
    }

    private static string GetStatusDisplay(Expense purchase, Core.Data.CompanyData? companyData)
    {
        // Check for lost/damaged related to this purchase
        var relatedLostDamaged = companyData?.LostDamaged.FirstOrDefault(ld => ld.InventoryItemId == purchase.Id);
        if (relatedLostDamaged != null)
        {
            return "Lost/Damaged";
        }

        // Check for returns related to this purchase
        var relatedReturn = companyData?.Returns.FirstOrDefault(r => r.OriginalTransactionId == purchase.Id);

        if (relatedReturn is { Status: ReturnStatus.Completed })
        {
            return "Returned";
        }

        // Default status based on payment info
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
            totalCount, CurrentPage, PageSize, TotalPages, "expense");
    }

    #endregion

    #region Modal Commands

    [RelayCommand]
    private void OpenAddModal()
    {
        App.ExpenseModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void OpenEditModal(ExpenseDisplayItem? item)
    {
        App.ExpenseModalsViewModel?.OpenEditModal(item);
    }

    [RelayCommand]
    private void OpenDeleteConfirm(ExpenseDisplayItem? item)
    {
        App.ExpenseModalsViewModel?.OpenDeleteConfirm(item);
    }

    [RelayCommand]
    private void MarkAsLostDamaged(ExpenseDisplayItem? item)
    {
        App.ExpenseModalsViewModel?.OpenMarkAsLostDamagedModal(item);
    }

    [RelayCommand]
    private void MarkAsReturned(ExpenseDisplayItem? item)
    {
        App.ExpenseModalsViewModel?.OpenMarkAsReturnedModal(item);
    }

    [RelayCommand]
    private void UndoLostDamaged(ExpenseDisplayItem? item)
    {
        App.ExpenseModalsViewModel?.OpenUndoLostDamagedModal(item);
    }

    [RelayCommand]
    private void UndoReturn(ExpenseDisplayItem? item)
    {
        App.ExpenseModalsViewModel?.OpenUndoReturnedModal(item);
    }

    [RelayCommand]
    private void OpenFilterModal()
    {
        App.ExpenseModalsViewModel?.OpenFilterModal();
    }

    #endregion

    #region Receipt Preview

    [RelayCommand]
    private void ViewReceipt(ExpenseDisplayItem? item)
    {
        if (item == null || !item.HasReceipt)
            return;

        var receiptPath = GetReceiptImagePath(item.Id);
        if (string.IsNullOrEmpty(receiptPath))
            return;

        App.ReceiptViewerModal?.Show(receiptPath, item.Id);
    }

    private static string? GetReceiptImagePath(string expenseId)
    {
        var companyData = App.CompanyManager?.CompanyData;

        var expense = companyData?.Expenses.FirstOrDefault(p => p.Id == expenseId);
        if (expense == null || string.IsNullOrEmpty(expense.ReceiptId)) return null;

        var receipt = companyData?.Receipts.FirstOrDefault(r => r.Id == expense.ReceiptId);
        if (receipt == null || string.IsNullOrEmpty(receipt.FileData)) return null;

        try
        {
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
/// Display model for expenses in the UI.
/// </summary>
public partial class ExpenseDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _accountantName = string.Empty;

    [ObservableProperty]
    private string _productDescription = string.Empty;

    [ObservableProperty]
    private string _productMoreText = string.Empty;

    [ObservableProperty]
    private string _categoryName = string.Empty;

    [ObservableProperty]
    private string _supplierName = string.Empty;

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
    private decimal _feeUSD;

    [ObservableProperty]
    private decimal _unitPriceUSD;

    [ObservableProperty]
    private bool _hasReceipt;

    [ObservableProperty]
    private string _receiptFilePath = string.Empty;

    [ObservableProperty]
    private string _statusDisplay = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string? _supplierId;

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
    private decimal _fee;

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
    public string FeeFormatted => CurrencyService.FormatFromUSD(FeeUSD, Date);
    public string UnitPriceFormatted => CurrencyService.FormatFromUSD(UnitPriceUSD, Date);
    public string ReceiptIcon => HasReceipt ? "✓" : "✗";

    public bool IsReturned => StatusDisplay == "Returned";
    public bool IsPartialReturn => StatusDisplay == "Partial Return";
    public bool IsLostDamaged => StatusDisplay == "Lost/Damaged";
    public bool CanMarkAsReturned => !IsReturned && !IsLostDamaged;
    public bool CanMarkAsLostDamaged => !IsReturned && !IsLostDamaged;

    [ObservableProperty]
    private bool _isHighlighted;
}
