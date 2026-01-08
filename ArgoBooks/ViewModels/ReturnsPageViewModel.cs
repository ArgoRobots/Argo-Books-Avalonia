using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Returns page displaying expense and customer returns.
/// </summary>
public partial class ReturnsPageViewModel : ViewModelBase
{
    #region Table Column Widths

    /// <summary>
    /// Column widths manager for the table (shared across page navigations).
    /// </summary>
    public ReturnsTableColumnWidths ColumnWidths => App.ReturnsColumnWidths;

    #endregion

    #region Statistics

    [ObservableProperty]
    private int _totalReturns;

    [ObservableProperty]
    private int _expenseReturns;

    [ObservableProperty]
    private int _customerReturns;

    [ObservableProperty]
    private string _totalRefunded = "$0.00";

    #endregion

    #region Tabs

    [ObservableProperty]
    private int _selectedTabIndex;

    partial void OnSelectedTabIndexChanged(int value)
    {
        CurrentPage = 1;
        FilterReturns();
    }

    public bool IsExpenseTabActive => SelectedTabIndex == 0;

    #endregion

    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterReturns();
    }

    #endregion

    #region Returns Collection

    private readonly List<Return> _allReturns = [];

    public ObservableCollection<ReturnDisplayItem> Returns { get; } = [];

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
        FilterReturns();
    }

    [ObservableProperty]
    private string _paginationText = "0 returns";

    public ObservableCollection<int> PageNumbers { get; } = [];

    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages;

    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        FilterReturns();
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

    public ReturnsPageViewModel()
    {
        LoadReturns();

        // Subscribe to undo/redo state changes to refresh UI
        if (App.UndoRedoManager != null)
        {
            App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;
        }

        // Subscribe to filter modal events
        if (App.ReturnsModalsViewModel != null)
        {
            App.ReturnsModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.ReturnsModalsViewModel.FiltersCleared += OnFiltersCleared;
        }
    }

    private void OnFiltersApplied(object? sender, EventArgs e)
    {
        CurrentPage = 1;
        FilterReturns();
    }

    private void OnFiltersCleared(object? sender, EventArgs e)
    {
        SearchQuery = null;
        CurrentPage = 1;
        FilterReturns();
    }

    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadReturns();
    }

    #endregion

    #region Data Loading

    private void LoadReturns()
    {
        _allReturns.Clear();
        Returns.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Returns == null)
            return;

        _allReturns.AddRange(companyData.Returns);
        UpdateStatistics();
        FilterReturns();
    }

    private void UpdateStatistics()
    {
        TotalReturns = _allReturns.Count;
        ExpenseReturns = _allReturns.Count(r => r.ReturnType == "Expense");
        CustomerReturns = _allReturns.Count(r => r.ReturnType == "Customer");
        var totalRefundedValue = _allReturns.Sum(r => r.NetRefund);
        TotalRefunded = $"${totalRefundedValue:N2}";
    }

    [RelayCommand]
    private void RefreshReturns()
    {
        LoadReturns();
    }

    private void FilterReturns()
    {
        Returns.Clear();

        var filtered = _allReturns.ToList();

        // Get filter values from modals view model
        var modals = App.ReturnsModalsViewModel;
        var filterStatus = modals?.FilterStatus ?? "All";
        var filterReason = modals?.FilterReason ?? "All";
        var filterDateFrom = modals?.FilterDateFrom;
        var filterDateTo = modals?.FilterDateTo;

        // Filter by tab (expense vs customer)
        var returnType = IsExpenseTabActive ? "Expense" : "Customer";
        filtered = filtered.Where(r => r.ReturnType == returnType).ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(r =>
                r.Id.ToLowerInvariant().Contains(query) ||
                r.OriginalTransactionId.ToLowerInvariant().Contains(query) ||
                GetProductNames(r).ToLowerInvariant().Contains(query) ||
                GetSupplierOrCustomerName(r).ToLowerInvariant().Contains(query)
            ).ToList();
        }

        // Apply status filter
        if (filterStatus != "All")
        {
            filtered = filtered.Where(r => r.Status.ToString() == filterStatus).ToList();
        }

        // Apply reason filter
        if (filterReason != "All")
        {
            filtered = filtered.Where(r =>
                r.Items.Any(item => item.Reason.Equals(filterReason, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        // Apply date filter
        if (filterDateFrom.HasValue)
        {
            filtered = filtered.Where(r => r.ReturnDate >= filterDateFrom.Value.DateTime).ToList();
        }
        if (filterDateTo.HasValue)
        {
            filtered = filtered.Where(r => r.ReturnDate <= filterDateTo.Value.DateTime).ToList();
        }

        // Sort by date descending (newest first)
        filtered = filtered.OrderByDescending(r => r.ReturnDate).ToList();

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
        var pagedReturns = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedReturns)
        {
            Returns.Add(item);
        }
    }

    private ReturnDisplayItem CreateDisplayItem(Return returnRecord)
    {
        var companyData = App.CompanyManager?.CompanyData;
        var productNames = GetProductNames(returnRecord);
        var supplierOrCustomerName = GetSupplierOrCustomerName(returnRecord);
        var processedByName = GetProcessedByName(returnRecord);
        var reason = returnRecord.Items.FirstOrDefault()?.Reason ?? "Not specified";

        return new ReturnDisplayItem
        {
            Id = returnRecord.Id,
            OriginalTransactionId = returnRecord.OriginalTransactionId,
            ReturnType = returnRecord.ReturnType,
            ProductNames = productNames,
            SupplierOrCustomerName = supplierOrCustomerName,
            ReturnDate = returnRecord.ReturnDate,
            Reason = reason,
            ProcessedBy = processedByName,
            RefundAmount = returnRecord.NetRefund,
            Status = returnRecord.Status,
            Notes = returnRecord.Notes,
            ItemCount = returnRecord.Items.Sum(i => i.Quantity)
        };
    }

    private string GetProductNames(Return returnRecord)
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return "Unknown";

        var productNames = returnRecord.Items
            .Select(item => companyData.GetProduct(item.ProductId)?.Name ?? "Unknown Product")
            .Distinct()
            .ToList();

        return productNames.Count > 2
            ? $"{productNames[0]}, {productNames[1]} +{productNames.Count - 2} more"
            : string.Join(", ", productNames);
    }

    private string GetSupplierOrCustomerName(Return returnRecord)
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return "Unknown";

        if (returnRecord.ReturnType == "Expense")
        {
            // For expense returns, look up the supplier from the original purchase
            var purchase = companyData.Purchases.FirstOrDefault(p => p.Id == returnRecord.OriginalTransactionId);
            if (purchase != null)
            {
                var supplier = companyData.GetSupplier(purchase.SupplierId ?? "");
                return supplier?.Name ?? "Unknown Supplier";
            }
            return "Unknown Supplier";
        }
        else
        {
            // For customer returns, look up the customer
            var customer = companyData.GetCustomer(returnRecord.CustomerId);
            return customer?.Name ?? "Unknown Customer";
        }
    }

    private string GetProcessedByName(Return returnRecord)
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return "Unknown";

        if (returnRecord.ReturnType == "Expense")
        {
            // For expense returns, the ProcessedBy is typically an employee
            var employee = companyData.GetEmployee(returnRecord.ProcessedBy ?? "");
            return employee?.FullName ?? returnRecord.ProcessedBy ?? "Unknown";
        }
        else
        {
            // For customer returns, the ProcessedBy is typically an accountant
            var accountant = companyData.GetAccountant(returnRecord.ProcessedBy ?? "");
            return accountant?.Name ?? returnRecord.ProcessedBy ?? "Unknown";
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
        PaginationText = PaginationTextHelper.FormatPaginationText(
            totalCount, CurrentPage, PageSize, TotalPages, "return");
    }

    #endregion

    #region Filter Modal Commands

    [RelayCommand]
    private void OpenFilterModal()
    {
        App.ReturnsModalsViewModel?.OpenFilterModal();
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
    private string _viewDetailsReason = string.Empty;

    [ObservableProperty]
    private string _viewDetailsNotes = string.Empty;

    [ObservableProperty]
    private string _viewDetailsDate = string.Empty;

    [ObservableProperty]
    private string _viewDetailsRefund = string.Empty;

    #endregion

    #region Undo Return Modal

    private ReturnDisplayItem? _undoReturnItem;

    [ObservableProperty]
    private bool _isUndoReturnModalOpen;

    [ObservableProperty]
    private string _undoReturnItemDescription = string.Empty;

    [ObservableProperty]
    private string _undoReturnReason = string.Empty;

    #endregion

    #region Action Commands

    [RelayCommand]
    private void ViewReturnDetails(ReturnDisplayItem? item)
    {
        if (item == null) return;

        ViewDetailsId = item.Id;
        ViewDetailsProduct = item.ProductNames;
        ViewDetailsReason = item.Reason;
        ViewDetailsNotes = string.IsNullOrWhiteSpace(item.Notes) ? "No notes provided" : item.Notes;
        ViewDetailsDate = item.DateFormatted;
        ViewDetailsRefund = item.RefundAmountFormatted;
        IsViewDetailsModalOpen = true;
    }

    [RelayCommand]
    private void CloseViewDetailsModal()
    {
        IsViewDetailsModalOpen = false;
    }

    [RelayCommand]
    private void UndoReturn(ReturnDisplayItem? item)
    {
        if (item == null) return;

        _undoReturnItem = item;
        UndoReturnItemDescription = $"{item.Id} - {item.ProductNames}";
        UndoReturnReason = string.Empty;
        IsUndoReturnModalOpen = true;
    }

    [RelayCommand]
    private void CloseUndoReturnModal()
    {
        IsUndoReturnModalOpen = false;
        _undoReturnItem = null;
        UndoReturnReason = string.Empty;
    }

    [RelayCommand]
    private void ConfirmUndoReturn()
    {
        if (_undoReturnItem == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
        {
            CloseUndoReturnModal();
            return;
        }

        var returnRecord = companyData.Returns.FirstOrDefault(r => r.Id == _undoReturnItem.Id);
        if (returnRecord != null)
        {
            companyData.Returns.Remove(returnRecord);
            App.CompanyManager?.MarkAsChanged();
        }

        CloseUndoReturnModal();
        LoadReturns();
    }

    #endregion
}

/// <summary>
/// Display model for returns in the UI.
/// </summary>
public partial class ReturnDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _originalTransactionId = string.Empty;

    [ObservableProperty]
    private string _returnType = string.Empty;

    [ObservableProperty]
    private string _productNames = string.Empty;

    [ObservableProperty]
    private string _supplierOrCustomerName = string.Empty;

    [ObservableProperty]
    private DateTime _returnDate;

    [ObservableProperty]
    private string _reason = string.Empty;

    [ObservableProperty]
    private string _processedBy = string.Empty;

    [ObservableProperty]
    private decimal _refundAmount;

    [ObservableProperty]
    private ReturnStatus _status;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private int _itemCount;

    // Computed properties for display
    public string DateFormatted => ReturnDate.ToString("MMM d, yyyy");
    public string RefundAmountFormatted => $"${RefundAmount:N2}";
    public string StatusText => Status.ToString();

    public bool IsPending => Status == ReturnStatus.Pending;
    public bool IsApproved => Status == ReturnStatus.Approved;
    public bool IsCompleted => Status == ReturnStatus.Completed;
    public bool IsRejected => Status == ReturnStatus.Rejected;

    public string StatusBadgeBackground => Status switch
    {
        ReturnStatus.Pending => "#FEF3C7",
        ReturnStatus.Approved => "#DBEAFE",
        ReturnStatus.Completed => "#DCFCE7",
        ReturnStatus.Rejected => "#FEE2E2",
        _ => "#F3F4F6"
    };

    public string StatusBadgeForeground => Status switch
    {
        ReturnStatus.Pending => "#D97706",
        ReturnStatus.Approved => "#2563EB",
        ReturnStatus.Completed => "#16A34A",
        ReturnStatus.Rejected => "#DC2626",
        _ => "#6B7280"
    };
}
