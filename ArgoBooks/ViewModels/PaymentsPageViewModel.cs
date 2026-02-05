using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Controls.ColumnWidths;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArgoBooks.Helpers;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Payments page.
/// </summary>
public partial class PaymentsPageViewModel : SortablePageViewModelBase
{
    public Helpers.ResponsiveHeaderHelper ResponsiveHeader { get; } = new();

    #region Statistics

    [ObservableProperty]
    private string _receivedThisMonth = "$0.00";

    [ObservableProperty]
    private int _totalTransactions;

    [ObservableProperty]
    private int _pendingPayments;

    [ObservableProperty]
    private int _refundedPayments;

    #endregion

    #region Payment Portal

    [ObservableProperty]
    private string _lastSyncTime = "2 minutes ago";

    [ObservableProperty]
    private bool _isPortalConnected = true;

    [ObservableProperty]
    private bool _isSyncing;

    /// <summary>
    /// Syncs the payment portal data.
    /// </summary>
    [RelayCommand]
    private async Task SyncPortal()
    {
        if (IsSyncing) return;

        IsSyncing = true;
        try
        {
            // Simulate sync delay
            await Task.Delay(1500);
            LastSyncTime = "Just now";
            LoadPayments();
        }
        finally
        {
            IsSyncing = false;
        }
    }

    /// <summary>
    /// Opens the payment portal in the default browser.
    /// </summary>
    [RelayCommand]
    private void OpenPortal()
    {
        // TODO: Implement payment portal URL opening
    }

    #endregion

    #region Search and Filter

    [ObservableProperty]
    private string? _searchQuery;

    partial void OnSearchQueryChanged(string? value)
    {
        CurrentPage = 1;
        FilterPayments();
    }

    [ObservableProperty]
    private string _filterPaymentMethod = "All";

    [ObservableProperty]
    private string _filterStatus = "All";

    [ObservableProperty]
    private string? _filterCustomerId;

    [ObservableProperty]
    private string? _filterAmountMin;

    [ObservableProperty]
    private string? _filterAmountMax;

    [ObservableProperty]
    private DateTimeOffset? _filterDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterDateTo;

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
    public PaymentsTableColumnWidths ColumnWidths => App.PaymentsColumnWidths;

    [ObservableProperty]
    private bool _showIdColumn = ColumnVisibilityHelper.Load("Payments", "Id", true);

    [ObservableProperty]
    private bool _showInvoiceColumn = ColumnVisibilityHelper.Load("Payments", "Invoice", true);

    [ObservableProperty]
    private bool _showCustomerColumn = ColumnVisibilityHelper.Load("Payments", "Customer", true);

    [ObservableProperty]
    private bool _showDateColumn = ColumnVisibilityHelper.Load("Payments", "Date", true);

    [ObservableProperty]
    private bool _showMethodColumn = ColumnVisibilityHelper.Load("Payments", "Method", true);

    [ObservableProperty]
    private bool _showAmountColumn = ColumnVisibilityHelper.Load("Payments", "Amount", true);

    [ObservableProperty]
    private bool _showStatusColumn = ColumnVisibilityHelper.Load("Payments", "Status", true);

    partial void OnShowIdColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Id", value); ColumnVisibilityHelper.Save("Payments", "Id", value); }
    partial void OnShowInvoiceColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Invoice", value); ColumnVisibilityHelper.Save("Payments", "Invoice", value); }
    partial void OnShowCustomerColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Customer", value); ColumnVisibilityHelper.Save("Payments", "Customer", value); }
    partial void OnShowDateColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Date", value); ColumnVisibilityHelper.Save("Payments", "Date", value); }
    partial void OnShowMethodColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Method", value); ColumnVisibilityHelper.Save("Payments", "Method", value); }
    partial void OnShowAmountColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Amount", value); ColumnVisibilityHelper.Save("Payments", "Amount", value); }
    partial void OnShowStatusColumnChanged(bool value) { ColumnWidths.SetColumnVisibility("Status", value); ColumnVisibilityHelper.Save("Payments", "Status", value); }

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
        ColumnVisibilityHelper.ResetPage("Payments");
        ShowIdColumn = true;
        ShowInvoiceColumn = true;
        ShowCustomerColumn = true;
        ShowDateColumn = true;
        ShowMethodColumn = true;
        ShowAmountColumn = true;
        ShowStatusColumn = true;
    }

    #endregion

    #region Payments Collection

    /// <summary>
    /// All payments (unfiltered).
    /// </summary>
    private readonly List<Payment> _allPayments = [];

    /// <summary>
    /// Payments for display in the table.
    /// </summary>
    public ObservableCollection<PaymentDisplayItem> Payments { get; } = [];

    /// <summary>
    /// Payment method options for filter.
    /// </summary>
    public ObservableCollection<string> PaymentMethodOptions { get; } = ["All", "Cash", "Check"];

    /// <summary>
    /// Status options for filter.
    /// </summary>
    public ObservableCollection<string> StatusOptions { get; } = ["All", "Completed", "Pending", "Partial", "Refunded"];

    /// <summary>
    /// Customer options for filter (populated from company data).
    /// </summary>
    public ObservableCollection<CustomerOption> CustomerOptions { get; } = [];

    #endregion

    #region Pagination

    [ObservableProperty]
    private string _paginationText = "0 payments";

    /// <inheritdoc />
    protected override void OnSortOrPageChanged() => FilterPayments();

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public PaymentsPageViewModel()
    {
        // Set default sort values for payments
        SortColumn = "Date";
        SortDirection = SortDirection.Descending;

        LoadPayments();
        LoadCustomerOptions();

        // Subscribe to undo/redo state changes to refresh UI
        App.UndoRedoManager.StateChanged += OnUndoRedoStateChanged;

        // Subscribe to payment modal events to refresh data
        if (App.PaymentModalsViewModel != null)
        {
            App.PaymentModalsViewModel.PaymentSaved += OnPaymentSaved;
            App.PaymentModalsViewModel.PaymentDeleted += OnPaymentDeleted;
            App.PaymentModalsViewModel.FiltersApplied += OnFiltersApplied;
            App.PaymentModalsViewModel.FiltersCleared += OnFiltersCleared;
        }

        // Subscribe to currency changes to refresh currency display
        CurrencyService.CurrencyChanged += (_, _) =>
        {
            UpdateStatistics();
            FilterPayments();
        };
    }

    /// <summary>
    /// Handles undo/redo state changes by refreshing the payments.
    /// </summary>
    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        LoadPayments();
    }

    /// <summary>
    /// Handles payment saved event from modals.
    /// </summary>
    private void OnPaymentSaved(object? sender, EventArgs e)
    {
        LoadPayments();
    }

    /// <summary>
    /// Handles payment deleted event from modals.
    /// </summary>
    private void OnPaymentDeleted(object? sender, EventArgs e)
    {
        LoadPayments();
    }

    /// <summary>
    /// Handles filters applied event from modals.
    /// </summary>
    private void OnFiltersApplied(object? sender, EventArgs e)
    {
        // Copy filter values from shared ViewModel
        var modals = App.PaymentModalsViewModel;
        if (modals != null)
        {
            FilterPaymentMethod = modals.FilterPaymentMethod;
            FilterStatus = modals.FilterStatus;
            FilterCustomerId = modals.FilterCustomerId;
            FilterAmountMin = modals.FilterAmountMin;
            FilterAmountMax = modals.FilterAmountMax;
            FilterDateFrom = modals.FilterDateFrom;
            FilterDateTo = modals.FilterDateTo;
        }
        CurrentPage = 1;
        FilterPayments();
    }

    /// <summary>
    /// Handles filters cleared event from modals.
    /// </summary>
    private void OnFiltersCleared(object? sender, EventArgs e)
    {
        FilterPaymentMethod = "All";
        FilterStatus = "All";
        FilterCustomerId = null;
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterDateFrom = null;
        FilterDateTo = null;
        SearchQuery = null;
        CurrentPage = 1;
        FilterPayments();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads payments from the company data.
    /// </summary>
    private void LoadPayments()
    {
        _allPayments.Clear();
        Payments.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Payments == null)
            return;

        _allPayments.AddRange(companyData.Payments);
        UpdateStatistics();
        FilterPayments();
    }

    /// <summary>
    /// Loads customer options for the filter dropdown.
    /// </summary>
    private void LoadCustomerOptions()
    {
        CustomerOptions.Clear();
        CustomerOptions.Add(new CustomerOption { Id = string.Empty, Name = "All Customers" });

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Customers == null)
            return;

        foreach (var customer in companyData.Customers.OrderBy(c => c.Name))
        {
            CustomerOptions.Add(new CustomerOption { Id = customer.Id, Name = customer.Name });
        }
    }

    /// <summary>
    /// Updates the statistics based on current data.
    /// </summary>
    private void UpdateStatistics()
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        // Received this month (completed payments only) - calculate in USD, convert for display
        var monthlyReceivedUSD = _allPayments
            .Where(p => p.Date >= startOfMonth && GetPaymentStatus(p) == "Completed")
            .Sum(p => p.EffectiveAmountUSD);
        ReceivedThisMonth = CurrencyService.FormatFromUSD(monthlyReceivedUSD, now);

        // Total transactions
        TotalTransactions = _allPayments.Count;

        // Pending payments
        PendingPayments = _allPayments.Count(p => GetPaymentStatus(p) == "Pending");

        // Refunded payments
        RefundedPayments = _allPayments.Count(p => GetPaymentStatus(p) == "Refunded");
    }

    /// <summary>
    /// Gets the payment status based on amount and invoice.
    /// </summary>
    private string GetPaymentStatus(Payment payment)
    {
        if (payment.Amount < 0)
            return "Refunded";

        var companyData = App.CompanyManager?.CompanyData;
        var invoice = companyData?.Invoices.FirstOrDefault(i => i.Id == payment.InvoiceId);

        if (invoice == null)
            return "Completed";

        // Check if invoice is fully paid
        var totalPaid = companyData?.Payments
            .Where(p => p.InvoiceId == payment.InvoiceId && p.Amount > 0)
            .Sum(p => p.Amount) ?? 0;

        if (totalPaid >= invoice.Total)
            return "Completed";
        if (totalPaid > 0)
            return "Partial";
        return "Pending";
    }

    /// <summary>
    /// Refreshes the payments from the data source.
    /// </summary>
    [RelayCommand]
    private void RefreshPayments()
    {
        LoadPayments();
        LoadCustomerOptions();
    }

    /// <summary>
    /// Filters payments based on search query and filters.
    /// </summary>
    private void FilterPayments()
    {
        Payments.Clear();

        var companyData = App.CompanyManager?.CompanyData;
        var filtered = _allPayments.ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered
                .Select(p => new
                {
                    Payment = p,
                    IdScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, p.Id),
                    CustomerScore = LevenshteinDistance.ComputeSearchScore(SearchQuery,
                        companyData?.GetCustomer(p.CustomerId)?.Name ?? ""),
                    InvoiceScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, p.InvoiceId),
                    RefScore = LevenshteinDistance.ComputeSearchScore(SearchQuery, p.ReferenceNumber ?? "")
                })
                .Where(x => x.IdScore >= 0 || x.CustomerScore >= 0 || x.InvoiceScore >= 0 || x.RefScore >= 0)
                .OrderByDescending(x => Math.Max(Math.Max(x.IdScore, x.CustomerScore), Math.Max(x.InvoiceScore, x.RefScore)))
                .Select(x => x.Payment)
                .ToList();
        }

        // Apply payment method filter
        if (FilterPaymentMethod != "All")
        {
            var method = FilterPaymentMethod switch
            {
                "Cash" => PaymentMethod.Cash,
                "Check" => PaymentMethod.Check,
                _ => PaymentMethod.Cash
            };
            filtered = filtered.Where(p => p.PaymentMethod == method).ToList();
        }

        // Apply status filter
        if (FilterStatus != "All")
        {
            filtered = filtered.Where(p => GetPaymentStatus(p) == FilterStatus).ToList();
        }

        // Apply customer filter
        if (!string.IsNullOrEmpty(FilterCustomerId))
        {
            filtered = filtered.Where(p => p.CustomerId == FilterCustomerId).ToList();
        }

        // Apply amount filter
        if (decimal.TryParse(FilterAmountMin, out var minAmount))
        {
            filtered = filtered.Where(p => Math.Abs(p.Amount) >= minAmount).ToList();
        }
        if (decimal.TryParse(FilterAmountMax, out var maxAmount))
        {
            filtered = filtered.Where(p => Math.Abs(p.Amount) <= maxAmount).ToList();
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

        // Create display items
        var displayItems = filtered.Select(payment =>
        {
            var customer = companyData?.GetCustomer(payment.CustomerId);
            var status = GetPaymentStatus(payment);

            return new PaymentDisplayItem
            {
                Id = payment.Id,
                InvoiceId = payment.InvoiceId,
                InvoiceDisplay = string.IsNullOrEmpty(payment.InvoiceId) ? "-" : payment.InvoiceId,
                CustomerId = payment.CustomerId,
                CustomerName = customer?.Name ?? "Unknown Customer",
                Date = payment.Date,
                PaymentMethod = payment.PaymentMethod,
                PaymentMethodDisplay = payment.PaymentMethod switch
                {
                    PaymentMethod.Cash => "Cash",
                    PaymentMethod.Check => "Check",
                    _ => payment.PaymentMethod.ToString()
                },
                Amount = payment.Amount,
                AmountUSD = payment.EffectiveAmountUSD,
                Status = status,
                ReferenceNumber = payment.ReferenceNumber,
                Notes = payment.Notes
            };
        }).ToList();

        // Apply sorting (only if not searching, since search has its own relevance sorting)
        if (string.IsNullOrWhiteSpace(SearchQuery) || SortDirection != SortDirection.None)
        {
            displayItems = displayItems.ApplySort(
                SortColumn,
                SortDirection,
                new Dictionary<string, Func<PaymentDisplayItem, object?>>
                {
                    ["Id"] = p => p.Id,
                    ["Invoice"] = p => p.InvoiceDisplay,
                    ["Customer"] = p => p.CustomerName,
                    ["Date"] = p => p.Date,
                    ["Method"] = p => p.PaymentMethodDisplay,
                    ["Amount"] = p => p.Amount,
                    ["Status"] = p => p.Status
                },
                p => p.Date);
        }

        // Calculate pagination
        var totalCount = displayItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;

        UpdatePageNumbers();
        UpdatePaginationText(totalCount);

        // Apply pagination and add to collection
        var pagedPayments = displayItems
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        foreach (var item in pagedPayments)
        {
            Payments.Add(item);
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
            totalCount, CurrentPage, PageSize, TotalPages, "payment");
    }

    #endregion

    #region Modal Commands

    /// <summary>
    /// Opens the Add Payment modal.
    /// </summary>
    [RelayCommand]
    private void OpenAddModal()
    {
        App.PaymentModalsViewModel?.OpenAddModal();
    }

    /// <summary>
    /// Opens the Edit Payment modal.
    /// </summary>
    [RelayCommand]
    private void OpenEditModal(PaymentDisplayItem? item)
    {
        App.PaymentModalsViewModel?.OpenEditModal(item);
    }

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    [RelayCommand]
    private void OpenDeleteConfirm(PaymentDisplayItem? item)
    {
        App.PaymentModalsViewModel?.OpenDeleteConfirm(item);
    }

    /// <summary>
    /// Opens the filter modal.
    /// </summary>
    [RelayCommand]
    private void OpenFilterModal()
    {
        App.PaymentModalsViewModel?.OpenFilterModal();
    }

    #endregion
}

/// <summary>
/// Display model for payments in the UI.
/// </summary>
public partial class PaymentDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _invoiceId = string.Empty;

    [ObservableProperty]
    private string _invoiceDisplay = string.Empty;

    [ObservableProperty]
    private string _customerId = string.Empty;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private PaymentMethod _paymentMethod;

    [ObservableProperty]
    private string _paymentMethodDisplay = string.Empty;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private decimal _amountUSD;

    [ObservableProperty]
    private string _status = "Completed";

    [ObservableProperty]
    private string? _referenceNumber;

    [ObservableProperty]
    private string _notes = string.Empty;

    /// <summary>
    /// Gets the formatted date.
    /// </summary>
    public string DateFormatted => Date.ToString("MMM d, yyyy");

    /// <summary>
    /// Gets the formatted amount.
    /// </summary>
    public string AmountFormatted => AmountUSD < 0
        ? $"-{CurrencyService.FormatFromUSD(Math.Abs(AmountUSD), Date)}"
        : CurrencyService.FormatFromUSD(AmountUSD, Date);

    /// <summary>
    /// Gets whether the amount is negative (refund).
    /// </summary>
    public bool IsRefund => Amount < 0;
}

/// <summary>
/// Invoice option for dropdown.
/// </summary>
public class InvoiceOption
{
    public string? Id { get; set; }
    public string Display { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }

    public override string ToString() => Display;
}
