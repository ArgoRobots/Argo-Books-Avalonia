using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Expenses page displaying purchase/expense transactions.
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

    /// <summary>
    /// Column widths manager for the table (shared across page navigations).
    /// </summary>
    public TableColumnWidths ColumnWidths => App.ExpensesColumnWidths;

    [ObservableProperty]
    private bool _showIdColumn = true;

    [ObservableProperty]
    private bool _showAccountantColumn = true;

    [ObservableProperty]
    private bool _showProductColumn = true;

    [ObservableProperty]
    private bool _showSupplierColumn = true;

    [ObservableProperty]
    private bool _showDateColumn = true;

    [ObservableProperty]
    private bool _showQuantityColumn = false;

    [ObservableProperty]
    private bool _showUnitPriceColumn = false;

    [ObservableProperty]
    private bool _showAmountColumn = false;

    [ObservableProperty]
    private bool _showTaxColumn = false;

    [ObservableProperty]
    private bool _showShippingColumn = false;

    [ObservableProperty]
    private bool _showDiscountColumn = false;

    [ObservableProperty]
    private bool _showTotalColumn = true;

    [ObservableProperty]
    private bool _showReceiptColumn = true;

    [ObservableProperty]
    private bool _showStatusColumn = true;

    partial void OnShowIdColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Id", value);
    partial void OnShowAccountantColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Accountant", value);
    partial void OnShowProductColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Product", value);
    partial void OnShowSupplierColumnChanged(bool value) => ColumnWidths.SetColumnVisibility("Supplier", value);
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

    #region Expenses Collection

    private readonly List<Purchase> _allExpenses = [];

    public ObservableCollection<ExpenseDisplayItem> Expenses { get; } = [];

    public ObservableCollection<string> StatusOptions { get; } = ["All", "Completed", "Pending", "Partial Return", "Returned", "Cancelled"];

    public ObservableCollection<SupplierOption> SupplierOptions { get; } = [];

    public ObservableCollection<CategoryOption> CategoryOptions { get; } = [];

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
        LoadDropdownOptions();

        // Subscribe to undo/redo state changes to refresh UI
        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }

        // Subscribe to expense modal events to refresh data
        if (App.ExpenseModalsViewModel != null)
        {
            App.ExpenseModalsViewModel.ExpenseSaved += OnExpenseSaved;
            App.ExpenseModalsViewModel.ExpenseDeleted += OnExpenseDeleted;
            App.ExpenseModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.ExpenseModalsViewModel.FiltersCleared += OnFiltersCleared;
        }
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
        ColumnWidths.SetColumnVisibility("UnitPrice", ShowUnitPriceColumn);
        ColumnWidths.SetColumnVisibility("Amount", ShowAmountColumn);
        ColumnWidths.SetColumnVisibility("Tax", ShowTaxColumn);
        ColumnWidths.SetColumnVisibility("Shipping", ShowShippingColumn);
        ColumnWidths.SetColumnVisibility("Discount", ShowDiscountColumn);
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
        if (companyData?.Purchases == null)
            return;

        _allExpenses.AddRange(companyData.Purchases);
        UpdateStatistics();
        FilterExpenses();
    }

    private void LoadDropdownOptions()
    {
        SupplierOptions.Clear();
        SupplierOptions.Add(new SupplierOption { Id = null, Name = "All Suppliers" });

        CategoryOptions.Clear();
        CategoryOptions.Add(new CategoryOption { Id = null, Name = "All Categories" });

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        // Load suppliers
        foreach (var supplier in companyData.Suppliers.OrderBy(s => s.Name))
        {
            SupplierOptions.Add(new SupplierOption { Id = supplier.Id, Name = supplier.Name });
        }

        // Load expense (purchase) categories
        var purchaseCategories = companyData.Categories
            .Where(c => c.Type == CategoryType.Purchase)
            .OrderBy(c => c.Name);

        foreach (var category in purchaseCategories)
        {
            CategoryOptions.Add(new CategoryOption { Id = category.Id, Name = category.Name });
        }
    }

    private void UpdateStatistics()
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        // Total monthly expenses
        var monthlyTotal = _allExpenses
            .Where(p => p.Date >= startOfMonth)
            .Sum(p => p.Total);
        TotalMonthlyExpenses = $"${monthlyTotal:N2}";

        // Transaction count
        TransactionCount = _allExpenses.Count;

        // Receipts on file
        ReceiptsOnFile = _allExpenses.Count(p => !string.IsNullOrEmpty(p.ReceiptId));

        // Returns count (linked to returns data)
        var companyData = App.CompanyManager?.CompanyData;
        ReturnsCount = companyData?.Returns?.Count(r =>
            _allExpenses.Any(p => p.Id == r.OriginalTransactionId)) ?? 0;
    }

    [RelayCommand]
    private void RefreshExpenses()
    {
        LoadExpenses();
        LoadDropdownOptions();
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
                    Purchase = p,
                    IdScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, p.Id),
                    DescScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, p.Description),
                    SupplierScore = LevenshteinDistance.ComputeSearchScore(SearchQuery,
                        companyData?.GetSupplier(p.SupplierId ?? "")?.Name ?? "")
                })
                .Where(x => x.IdScore >= 0 || x.DescScore >= 0 || x.SupplierScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.IdScore, x.DescScore), x.SupplierScore))
                .Select(x => x.Purchase)
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

        // Apply category filter
        if (!string.IsNullOrEmpty(FilterCategoryId))
        {
            filtered = filtered.Where(p => p.CategoryId == FilterCategoryId).ToList();
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
            var category = companyData?.GetCategory(purchase.CategoryId ?? "");
            var accountant = companyData?.GetAccountant(purchase.AccountantId ?? "");
            var statusDisplay = GetStatusDisplay(purchase, companyData);
            var hasReceipt = !string.IsNullOrEmpty(purchase.ReceiptId);
            var receiptFilePath = purchase.ReferenceNumber;

            return new ExpenseDisplayItem
            {
                Id = purchase.Id,
                AccountantName = accountant?.Name ?? "System",
                ProductDescription = purchase.Description,
                CategoryName = category?.Name ?? "-",
                SupplierName = supplier?.Name ?? "-",
                Date = purchase.Date,
                Total = purchase.Total,
                HasReceipt = hasReceipt,
                ReceiptFilePath = receiptFilePath,
                StatusDisplay = statusDisplay,
                Notes = purchase.Notes,
                SupplierId = purchase.SupplierId,
                CategoryId = purchase.CategoryId,
                Amount = purchase.Amount,
                TaxAmount = purchase.TaxAmount,
                TaxRate = purchase.TaxRate,
                ShippingCost = purchase.ShippingCost,
                Discount = purchase.Discount,
                Quantity = (int)purchase.Quantity,
                UnitPrice = purchase.UnitPrice,
                PaymentMethod = purchase.PaymentMethod
            };
        }).ToList();

        // Apply sorting
        if (SortDirection != SortDirection.None)
        {
            displayItems = SortColumn switch
            {
                "Id" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(e => e.Id).ToList()
                    : displayItems.OrderByDescending(e => e.Id).ToList(),
                "Accountant" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(e => e.AccountantName).ToList()
                    : displayItems.OrderByDescending(e => e.AccountantName).ToList(),
                "Product" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(e => e.ProductDescription).ToList()
                    : displayItems.OrderByDescending(e => e.ProductDescription).ToList(),
                "Category" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(e => e.CategoryName).ToList()
                    : displayItems.OrderByDescending(e => e.CategoryName).ToList(),
                "Supplier" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(e => e.SupplierName).ToList()
                    : displayItems.OrderByDescending(e => e.SupplierName).ToList(),
                "Date" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(e => e.Date).ToList()
                    : displayItems.OrderByDescending(e => e.Date).ToList(),
                "Total" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(e => e.Total).ToList()
                    : displayItems.OrderByDescending(e => e.Total).ToList(),
                "Status" => SortDirection == SortDirection.Ascending
                    ? displayItems.OrderBy(e => e.StatusDisplay).ToList()
                    : displayItems.OrderByDescending(e => e.StatusDisplay).ToList(),
                _ => displayItems.OrderByDescending(e => e.Date).ToList()
            };
        }
        else if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            displayItems = displayItems.OrderByDescending(e => e.Date).ToList();
        }

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

    private static string GetStatusDisplay(Purchase purchase, Core.Data.CompanyData? companyData)
    {
        // Check for lost/damaged related to this purchase
        var relatedLostDamaged = companyData?.LostDamaged?.FirstOrDefault(ld => ld.InventoryItemId == purchase.Id);
        if (relatedLostDamaged != null)
        {
            return "Lost/Damaged";
        }

        // Check for returns related to this purchase
        var relatedReturn = companyData?.Returns?.FirstOrDefault(r => r.OriginalTransactionId == purchase.Id);

        if (relatedReturn is { Status: Core.Enums.ReturnStatus.Completed })
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
        if (totalCount == 0)
        {
            PaginationText = "0 expenses";
            return;
        }

        if (TotalPages <= 1)
        {
            PaginationText = totalCount == 1 ? "1 expense" : $"{totalCount} expenses";
        }
        else
        {
            var start = (CurrentPage - 1) * PageSize + 1;
            var end = Math.Min(CurrentPage * PageSize, totalCount);
            PaginationText = $"{start}-{end} of {totalCount} expenses";
        }
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

    #region Receipt Preview Modal

    [ObservableProperty]
    private bool _isReceiptPreviewOpen;

    [ObservableProperty]
    private string _previewReceiptPath = string.Empty;

    [ObservableProperty]
    private string _previewReceiptId = string.Empty;

    [ObservableProperty]
    private bool _isReceiptFullscreen;

    [RelayCommand]
    private void ViewReceipt(ExpenseDisplayItem? item)
    {
        if (item == null || !item.HasReceipt)
            return;

        // Try to get the receipt path - check if file exists, otherwise load from stored data
        var receiptPath = GetReceiptImagePath(item.Id, item.ReceiptFilePath);
        if (string.IsNullOrEmpty(receiptPath))
            return;

        PreviewReceiptPath = receiptPath;
        PreviewReceiptId = item.Id;
        IsReceiptPreviewOpen = true;
        IsReceiptFullscreen = false;
    }

    private string? GetReceiptImagePath(string expenseId, string? originalPath)
    {
        // Always load from company file to ensure consistency
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return null;

        var expense = companyData.Purchases.FirstOrDefault(p => p.Id == expenseId);
        if (expense == null || string.IsNullOrEmpty(expense.ReceiptId)) return null;

        var receipt = companyData.Receipts.FirstOrDefault(r => r.Id == expense.ReceiptId);
        if (receipt == null || string.IsNullOrEmpty(receipt.FileData)) return null;

        try
        {
            // Create temp file from Base64 data stored in company file
            var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ArgoBooks", "Receipts");
            System.IO.Directory.CreateDirectory(tempDir);
            var tempPath = System.IO.Path.Combine(tempDir, receipt.FileName);
            var bytes = Convert.FromBase64String(receipt.FileData);
            System.IO.File.WriteAllBytes(tempPath, bytes);
            return tempPath;
        }
        catch
        {
            return null;
        }
    }

    [RelayCommand]
    private void CloseReceiptPreview()
    {
        IsReceiptPreviewOpen = false;
        IsReceiptFullscreen = false;
        PreviewReceiptPath = string.Empty;
    }

    [RelayCommand]
    private void ToggleReceiptFullscreen()
    {
        IsReceiptFullscreen = !IsReceiptFullscreen;
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
    private string _categoryName = string.Empty;

    [ObservableProperty]
    private string _supplierName = string.Empty;

    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private decimal _total;

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
    private int _quantity;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private PaymentMethod _paymentMethod;

    public string DateFormatted => Date.ToString("MMM d, yyyy");
    public string TotalFormatted => $"${Total:N2}";
    public string AmountFormatted => $"${Amount:N2}";
    public string TaxAmountFormatted => $"${TaxAmount:N2}";
    public string TaxRateFormatted => $"{TaxRate:N1}%";
    public string ShippingCostFormatted => $"${ShippingCost:N2}";
    public string DiscountFormatted => $"-${Discount:N2}";
    public string UnitPriceFormatted => $"${UnitPrice:N2}";
    public string ReceiptIcon => HasReceipt ? "✓" : "✗";

    public bool IsReturned => StatusDisplay == "Returned";
    public bool IsPartialReturn => StatusDisplay == "Partial Return";
    public bool IsLostDamaged => StatusDisplay == "Lost/Damaged";
    public bool CanMarkAsReturned => !IsReturned && !IsLostDamaged;
    public bool CanMarkAsLostDamaged => !IsReturned && !IsLostDamaged;
}

/// <summary>
/// Undoable action for adding an expense.
/// </summary>
public class ExpenseAddAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public ExpenseAddAction(string description, Purchase _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for editing an expense.
/// </summary>
public class ExpenseEditAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public ExpenseEditAction(string description, Purchase _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Undoable action for deleting an expense.
/// </summary>
public class ExpenseDeleteAction : IUndoableAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public ExpenseDeleteAction(string description, Purchase _, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}
