using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page.
/// Provides an overview of key business metrics, recent transactions, and quick actions.
/// </summary>
public partial class DashboardPageViewModel : ChartContextMenuViewModelBase
{
    #region Date Range

    /// <summary>
    /// Available date range options.
    /// </summary>
    public ObservableCollection<string> DateRangeOptions { get; } =
    [
        "This Month",
        "Last Month",
        "This Quarter",
        "Last Quarter",
        "This Year",
        "Last Year",
        "All Time",
        "Custom Range"
    ];

    [ObservableProperty]
    private string _selectedDateRange = "This Month";

    [ObservableProperty]
    private DateTime _startDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Now;

    /// <summary>
    /// Gets whether the custom date range option is selected.
    /// </summary>
    public bool IsCustomDateRange => SelectedDateRange == "Custom Range";

    /// <summary>
    /// Gets the label for comparison period based on selected date range.
    /// </summary>
    public string ComparisonPeriodLabel => SelectedDateRange switch
    {
        "This Month" => "from last month",
        "Last Month" => "from prior month",
        "This Quarter" => "from last quarter",
        "Last Quarter" => "from prior quarter",
        "This Year" => "from last year",
        "Last Year" => "from prior year",
        "All Time" => "",
        "Custom Range" => "from prior period",
        _ => "from last period"
    };

    partial void OnSelectedDateRangeChanged(string value)
    {
        OnPropertyChanged(nameof(IsCustomDateRange));
        OnPropertyChanged(nameof(ComparisonPeriodLabel));
        UpdateDateRangeFromSelection();
        LoadDashboardData();
    }

    /// <summary>
    /// Updates the start and end dates based on the selected date range option.
    /// </summary>
    private void UpdateDateRangeFromSelection()
    {
        var now = DateTime.Now;

        switch (SelectedDateRange)
        {
            case "This Month":
                StartDate = new DateTime(now.Year, now.Month, 1);
                EndDate = now;
                break;

            case "Last Month":
                var lastMonth = now.AddMonths(-1);
                StartDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                EndDate = new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                break;

            case "This Quarter":
                var quarterStart = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1);
                StartDate = quarterStart;
                EndDate = now;
                break;

            case "Last Quarter":
                var lastQuarterEnd = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddDays(-1);
                var lastQuarterStart = lastQuarterEnd.AddMonths(-2);
                lastQuarterStart = new DateTime(lastQuarterStart.Year, lastQuarterStart.Month, 1);
                StartDate = lastQuarterStart;
                EndDate = lastQuarterEnd;
                break;

            case "This Year":
                StartDate = new DateTime(now.Year, 1, 1);
                EndDate = now;
                break;

            case "Last Year":
                StartDate = new DateTime(now.Year - 1, 1, 1);
                EndDate = new DateTime(now.Year - 1, 12, 31);
                break;

            case "All Time":
                StartDate = new DateTime(2000, 1, 1);
                EndDate = now;
                break;

            case "Custom Range":
                // Keep current values, let user modify
                break;
        }
    }

    /// <summary>
    /// Gets the comparison period dates based on the selected date range.
    /// </summary>
    private (DateTime prevStartDate, DateTime prevEndDate) GetComparisonPeriod()
    {
        var now = DateTime.Now;

        return SelectedDateRange switch
        {
            "This Month" => (
                new DateTime(now.Year, now.Month, 1).AddMonths(-1),
                new DateTime(now.Year, now.Month, 1).AddDays(-1)
            ),
            "Last Month" => (
                new DateTime(now.Year, now.Month, 1).AddMonths(-2),
                new DateTime(now.Year, now.Month, 1).AddMonths(-1).AddDays(-1)
            ),
            "This Quarter" => (
                new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddMonths(-3),
                new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddDays(-1)
            ),
            "Last Quarter" => (
                new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddMonths(-6),
                new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddMonths(-3).AddDays(-1)
            ),
            "This Year" => (
                new DateTime(now.Year - 1, 1, 1),
                new DateTime(now.Year - 1, 12, 31)
            ),
            "Last Year" => (
                new DateTime(now.Year - 2, 1, 1),
                new DateTime(now.Year - 2, 12, 31)
            ),
            "All Time" => (DateTime.MinValue, DateTime.MinValue), // No comparison for All Time
            "Custom Range" => (
                StartDate.AddDays(-(EndDate - StartDate).TotalDays - 1),
                StartDate.AddDays(-1)
            ),
            _ => (StartDate.AddDays(-30), StartDate.AddDays(-1))
        };
    }

    #endregion

    #region Statistics Properties

    [ObservableProperty]
    private string _totalRevenue = "$0.00";

    [ObservableProperty]
    private double? _revenueChangeValue;

    [ObservableProperty]
    private string? _revenueChangeText;

    [ObservableProperty]
    private string _totalExpenses = "$0.00";

    [ObservableProperty]
    private double? _expenseChangeValue;

    [ObservableProperty]
    private string? _expenseChangeText;

    [ObservableProperty]
    private string _outstandingInvoices = "$0.00";

    [ObservableProperty]
    private int _outstandingInvoiceCount;

    [ObservableProperty]
    private string _activeRentals = "0";

    [ObservableProperty]
    private int _overdueRentalCount;

    #endregion

    #region Net Profit Properties

    [ObservableProperty]
    private string _netProfit = "$0.00";

    [ObservableProperty]
    private double? _profitChangeValue;

    [ObservableProperty]
    private string? _profitChangeText;

    #endregion

    #region Recent Transactions

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecentTransactions))]
    [NotifyPropertyChangedFor(nameof(HasNoRecentTransactions))]
    private ObservableCollection<RecentTransactionItem> _recentTransactions = [];

    public bool HasRecentTransactions => RecentTransactions.Count > 0;
    public bool HasNoRecentTransactions => RecentTransactions.Count == 0;

    #endregion

    #region Active Rentals

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveRentals))]
    [NotifyPropertyChangedFor(nameof(HasNoActiveRentals))]
    private ObservableCollection<ActiveRentalItem> _activeRentalsList = [];

    public bool HasActiveRentals => ActiveRentalsList.Count > 0;
    public bool HasNoActiveRentals => ActiveRentalsList.Count == 0;

    #endregion

    #region Profits Overview Chart

    private readonly ChartLoaderService _chartLoaderService = new();

    [ObservableProperty]
    private ObservableCollection<ISeries> _profitsChartSeries = [];

    [ObservableProperty]
    private Axis[] _profitsChartXAxes = [];

    [ObservableProperty]
    private Axis[] _profitsChartYAxes = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProfitsChartTitleVisual))]
    private string _profitsChartTitle = "Total profits: $0.00";

    /// <summary>
    /// Gets the visual title element for the profits chart.
    /// </summary>
    public LabelVisual ProfitsChartTitleVisual => ChartLoaderService.CreateChartTitle(ProfitsChartTitle);

    [ObservableProperty]
    private bool _hasProfitsChartData;

    #endregion

    #region Sales vs Expenses Chart

    [ObservableProperty]
    private ObservableCollection<ISeries> _salesVsExpensesSeries = [];

    [ObservableProperty]
    private Axis[] _salesVsExpensesXAxes = [];

    [ObservableProperty]
    private Axis[] _salesVsExpensesYAxes = [];

    [ObservableProperty]
    private bool _hasSalesVsExpensesData;

    /// <summary>
    /// Gets the visual title element for the sales vs expenses chart.
    /// </summary>
    public LabelVisual SalesVsExpensesChartTitle => ChartLoaderService.CreateChartTitle("Sales vs Expenses");

    #endregion

    #region Company Data Reference

    private CompanyManager? _companyManager;

    #endregion

    #region Constructor

    public DashboardPageViewModel()
    {
        // Initialize with empty data - will be populated when company is loaded
        RecentTransactions = [];
        ActiveRentalsList = [];

        // Subscribe to theme changes to update legend text color and chart titles
        ThemeService.Instance.ThemeChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(LegendTextPaint));
            OnPropertyChanged(nameof(ProfitsChartTitleVisual));
            OnPropertyChanged(nameof(SalesVsExpensesChartTitle));
        };
    }

    /// <summary>
    /// Gets the legend text paint based on the current theme.
    /// </summary>
    public SolidColorPaint LegendTextPaint => ChartLoaderService.GetLegendTextPaint();

    #endregion

    #region Data Loading

    /// <summary>
    /// Initializes the ViewModel with the company manager.
    /// </summary>
    public void Initialize(CompanyManager companyManager)
    {
        _companyManager = companyManager;
        LoadDashboardData();

        // Subscribe to data change events
        _companyManager.CompanyDataChanged += OnCompanyDataChanged;
    }

    /// <summary>
    /// Cleans up event subscriptions.
    /// </summary>
    public void Cleanup()
    {
        if (_companyManager != null)
        {
            _companyManager.CompanyDataChanged -= OnCompanyDataChanged;
        }
    }

    private void OnCompanyDataChanged(object? sender, EventArgs e)
    {
        LoadDashboardData();
    }

    /// <summary>
    /// Loads all dashboard data from the company data.
    /// </summary>
    public void LoadDashboardData()
    {
        var data = _companyManager?.CompanyData;
        if (data == null) return;

        LoadStatistics(data);
        LoadRecentTransactions(data);
        LoadActiveRentals(data);
        LoadProfitsChart(data);
        LoadSalesVsExpensesChart(data);
    }

    private void LoadStatistics(CompanyData data)
    {
        // Calculate comparison period based on selected date range
        var (prevStartDate, prevEndDate) = GetComparisonPeriod();

        // Calculate current period revenue
        var currentRevenue = data.Sales
            .Where(s => s.Date >= StartDate && s.Date <= EndDate)
            .Sum(s => s.Total);

        // Calculate previous period revenue for comparison
        var prevRevenue = data.Sales
            .Where(s => s.Date >= prevStartDate && s.Date <= prevEndDate)
            .Sum(s => s.Total);

        TotalRevenue = FormatCurrency(currentRevenue);
        RevenueChangeValue = CalculatePercentageChange(prevRevenue, currentRevenue);
        RevenueChangeText = FormatPercentageChange(RevenueChangeValue);

        // Calculate current period expenses
        var currentExpenses = data.Purchases
            .Where(p => p.Date >= StartDate && p.Date <= EndDate)
            .Sum(p => p.Total);

        // Calculate previous period expenses for comparison
        var prevExpenses = data.Purchases
            .Where(p => p.Date >= prevStartDate && p.Date <= prevEndDate)
            .Sum(p => p.Total);

        TotalExpenses = FormatCurrency(currentExpenses);
        ExpenseChangeValue = CalculatePercentageChange(prevExpenses, currentExpenses);
        ExpenseChangeText = FormatPercentageChange(ExpenseChangeValue);

        // Calculate net profit
        var netProfitValue = currentRevenue - currentExpenses;
        var prevProfit = prevRevenue - prevExpenses;
        NetProfit = FormatCurrency(Math.Abs(netProfitValue));
        ProfitChangeValue = CalculatePercentageChange(prevProfit, netProfitValue);
        ProfitChangeText = FormatPercentageChange(ProfitChangeValue);

        // Calculate outstanding invoices
        var outstandingInvoices = data.Invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .ToList();

        OutstandingInvoiceCount = outstandingInvoices.Count;
        var outstandingAmount = outstandingInvoices.Sum(i => i.Balance);
        OutstandingInvoices = FormatCurrency(outstandingAmount);

        // Calculate active rentals
        var activeRentals = data.Rentals
            .Where(r => r.Status == RentalStatus.Active)
            .ToList();

        ActiveRentals = activeRentals.Count.ToString();
        OverdueRentalCount = activeRentals.Count(r => r.IsOverdue);
    }

    private void LoadRecentTransactions(CompanyData data)
    {
        var recentItems = new List<RecentTransactionItem>();

        // Get recent sales
        var recentSales = data.Sales
            .OrderByDescending(s => s.Date)
            .Take(5)
            .Select(s => new RecentTransactionItem
            {
                Id = s.Id,
                Type = "Sale",
                Description = string.IsNullOrEmpty(s.Description) ? "Sale Transaction" : s.Description,
                Amount = FormatCurrency(s.Total),
                AmountValue = s.Total,
                Date = s.Date,
                DateFormatted = FormatDate(s.Date),
                Status = "Completed",
                StatusVariant = "success",
                IsIncome = true,
                CustomerName = GetCustomerName(data, s.CustomerId)
            });

        recentItems.AddRange(recentSales);

        // Get recent purchases/expenses
        var recentPurchases = data.Purchases
            .OrderByDescending(p => p.Date)
            .Take(5)
            .Select(p => new RecentTransactionItem
            {
                Id = p.Id,
                Type = "Expense",
                Description = string.IsNullOrEmpty(p.Description) ? "Purchase Transaction" : p.Description,
                Amount = FormatCurrency(p.Total),
                AmountValue = p.Total,
                Date = p.Date,
                DateFormatted = FormatDate(p.Date),
                Status = "Completed",
                StatusVariant = "success",
                IsIncome = false,
                CustomerName = GetSupplierName(data, p.SupplierId)
            });

        recentItems.AddRange(recentPurchases);

        // Get recent invoices
        var recentInvoices = data.Invoices
            .OrderByDescending(i => i.IssueDate)
            .Take(5)
            .Select(i => new RecentTransactionItem
            {
                Id = i.Id,
                Type = "Invoice",
                Description = $"Invoice {i.InvoiceNumber}",
                Amount = FormatCurrency(i.Total),
                AmountValue = i.Total,
                Date = i.IssueDate,
                DateFormatted = FormatDate(i.IssueDate),
                Status = "Completed",
                StatusVariant = "success",
                IsIncome = true,
                CustomerName = GetCustomerName(data, i.CustomerId)
            });

        recentItems.AddRange(recentInvoices);

        // Sort by date and take top 10
        var sortedItems = recentItems
            .OrderByDescending(t => t.Date)
            .Take(10)
            .ToList();

        RecentTransactions = new ObservableCollection<RecentTransactionItem>(sortedItems);
    }

    private void LoadActiveRentals(CompanyData data)
    {
        var activeRentals = data.Rentals
            .Where(r => r.Status == RentalStatus.Active)
            .OrderBy(r => r.DueDate)
            .Take(10)
            .Select(r => new ActiveRentalItem
            {
                Id = r.Id,
                ItemName = GetRentalItemName(data, r.RentalItemId),
                CustomerName = GetCustomerName(data, r.CustomerId),
                StartDate = r.StartDate,
                StartDateFormatted = FormatDate(r.StartDate),
                DueDate = r.DueDate,
                DueDateFormatted = FormatDate(r.DueDate),
                RateAmount = FormatCurrency(r.RateAmount),
                RateType = r.RateType.ToString(),
                Status = r.IsOverdue ? "Overdue" : "Active",
                StatusVariant = r.IsOverdue ? "error" : "success",
                DaysRemaining = CalculateDaysRemaining(r.DueDate),
                IsOverdue = r.IsOverdue
            })
            .ToList();

        ActiveRentalsList = new ObservableCollection<ActiveRentalItem>(activeRentals);
    }

    private void LoadProfitsChart(CompanyData data)
    {
        // Update theme colors based on current theme
        _chartLoaderService.UpdateThemeColors(ThemeService.Instance.IsDarkTheme);

        // Load profits chart data for the selected date range
        var (series, labels, dates, totalProfit) = _chartLoaderService.LoadProfitsOverviewChart(data, StartDate, EndDate);

        ProfitsChartSeries = series;
        ProfitsChartXAxes = _chartLoaderService.CreateDateXAxes(dates);
        ProfitsChartYAxes = _chartLoaderService.CreateCurrencyYAxes();
        ProfitsChartTitle = $"Total profits: {FormatCurrency(totalProfit)}";
        HasProfitsChartData = series.Count > 0 && labels.Length > 0;
    }

    private void LoadSalesVsExpensesChart(CompanyData data)
    {
        // Update theme colors based on current theme
        _chartLoaderService.UpdateThemeColors(ThemeService.Instance.IsDarkTheme);

        var (series, labels) = _chartLoaderService.LoadSalesVsExpensesChart(data, StartDate, EndDate);
        SalesVsExpensesSeries = series;
        SalesVsExpensesXAxes = _chartLoaderService.CreateMonthXAxes(labels);
        SalesVsExpensesYAxes = _chartLoaderService.CreateCurrencyYAxes();
        HasSalesVsExpensesData = series.Count > 0 && labels.Length > 0;
    }

    #endregion

    #region Chart Context Menu Overrides

    /// <summary>
    /// Event raised when a chart image should be saved.
    /// The View should subscribe to this and handle the actual save dialog.
    /// </summary>
    public event EventHandler<SaveChartImageEventArgs>? SaveChartImageRequested;

    /// <inheritdoc />
    protected override void OnSaveChartAsImage()
    {
        SaveChartImageRequested?.Invoke(this, new SaveChartImageEventArgs { ChartId = SelectedChartId });
    }

    /// <summary>
    /// Event raised when exporting to Google Sheets starts or completes.
    /// </summary>
    public event EventHandler<GoogleSheetsExportEventArgs>? GoogleSheetsExportStatusChanged;

    /// <inheritdoc />
    protected override void OnExportToGoogleSheets()
    {
        // Fire and forget the async operation
        _ = ExportToGoogleSheetsAsync();
    }

    /// <summary>
    /// Exports the chart data to Google Sheets asynchronously.
    /// </summary>
    private async Task ExportToGoogleSheetsAsync()
    {
        var exportData = _chartLoaderService.GetGoogleSheetsExportData(SelectedChartId);
        if (exportData.Count == 0)
        {
            GoogleSheetsExportStatusChanged?.Invoke(this, new GoogleSheetsExportEventArgs
            {
                IsSuccess = false,
                ErrorMessage = "No chart data to export."
            });
            return;
        }

        // Check if Google credentials are configured
        if (!ArgoBooks.Core.Services.GoogleCredentialsManager.AreCredentialsConfigured())
        {
            GoogleSheetsExportStatusChanged?.Invoke(this, new GoogleSheetsExportEventArgs
            {
                IsSuccess = false,
                ErrorMessage = "Google OAuth credentials not configured. Please add GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET to your .env file."
            });
            return;
        }

        // Notify that export is starting
        GoogleSheetsExportStatusChanged?.Invoke(this, new GoogleSheetsExportEventArgs
        {
            IsExporting = true
        });

        try
        {
            var companyName = App.CompanyManager?.CurrentCompanyName ?? "Argo Books";
            var chartExportData = _chartLoaderService.GetExportDataForChart(SelectedChartId);
            // Use the UI title (SelectedChartId) for Google Sheets, not the internal stored title
            var chartTitle = !string.IsNullOrEmpty(SelectedChartId) ? SelectedChartId : (chartExportData?.ChartTitle ?? "Chart");

            // Use Pie chart type for distribution charts, Line or Column for time-based charts
            ArgoBooks.Core.Services.GoogleSheetsService.ChartType chartType;
            if (chartExportData?.ChartType == Services.ChartType.Distribution)
            {
                chartType = ArgoBooks.Core.Services.GoogleSheetsService.ChartType.Pie;
            }
            else
            {
                chartType = _chartLoaderService.UseLineChart
                    ? ArgoBooks.Core.Services.GoogleSheetsService.ChartType.Line
                    : ArgoBooks.Core.Services.GoogleSheetsService.ChartType.Column;
            }

            var googleSheetsService = new ArgoBooks.Core.Services.GoogleSheetsService();
            var url = await googleSheetsService.ExportFormattedDataToGoogleSheetsAsync(
                exportData,
                chartTitle,
                chartType,
                companyName
            );

            if (!string.IsNullOrEmpty(url))
            {
                // Open the spreadsheet in the browser
                ArgoBooks.Core.Services.GoogleSheetsService.OpenInBrowser(url);

                GoogleSheetsExportStatusChanged?.Invoke(this, new GoogleSheetsExportEventArgs
                {
                    IsSuccess = true,
                    SpreadsheetUrl = url
                });
            }
            else
            {
                GoogleSheetsExportStatusChanged?.Invoke(this, new GoogleSheetsExportEventArgs
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to create spreadsheet."
                });
            }
        }
        catch (InvalidOperationException ex)
        {
            GoogleSheetsExportStatusChanged?.Invoke(this, new GoogleSheetsExportEventArgs
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            GoogleSheetsExportStatusChanged?.Invoke(this, new GoogleSheetsExportEventArgs
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to export to Google Sheets: {ex.Message}"
            });
        }
    }

    /// <inheritdoc />
    protected override void OnExportToExcel()
    {
        var exportData = _chartLoaderService.GetExcelExportData();
        if (exportData.Rows.Count == 0)
        {
            // No data to export
            return;
        }

        // TODO: Implement Excel export using ClosedXML or EPPlus
        // The data is already formatted in exportData with headers, rows, and total
        // For now, this is a placeholder - the data structure is ready for export
        System.Diagnostics.Debug.WriteLine($"Excel export: {exportData.Rows.Count} rows ready for export.");
    }

    /// <inheritdoc />
    protected override void OnResetChartZoom()
    {
        ChartLoaderService.ResetZoom(ProfitsChartXAxes, ProfitsChartYAxes);
        ChartLoaderService.ResetZoom(SalesVsExpensesXAxes, SalesVsExpensesYAxes);
    }

    /// <summary>
    /// Gets the chart loader service for external use (e.g., report generation).
    /// </summary>
    public ChartLoaderService ChartLoaderService => _chartLoaderService;

    #endregion

    #region Quick Actions

    [RelayCommand]
    private void AddExpense()
    {
        App.NavigationService?.NavigateTo("Expenses");
        // Open the add expense modal after navigation
        App.ExpenseModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void RecordSale()
    {
        App.NavigationService?.NavigateTo("Revenue");
        // Open the add revenue modal after navigation
        App.RevenueModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void CreateInvoice()
    {
        App.NavigationService?.NavigateTo("Invoices");
        // Open the create invoice modal after navigation
        App.InvoiceModalsViewModel?.OpenCreateModal();
    }

    [RelayCommand]
    private void NewRental()
    {
        App.NavigationService?.NavigateTo("RentalRecords");
        // Open the add rental modal after navigation
        App.RentalRecordsModalsViewModel?.OpenAddModal();
    }

    [RelayCommand]
    private void NavigateToAnalytics()
    {
        App.NavigationService?.NavigateTo("Analytics");
    }

    [RelayCommand]
    private void NavigateToReports()
    {
        App.NavigationService?.NavigateTo("Reports");
    }

    [RelayCommand]
    private void NavigateToRevenue()
    {
        App.NavigationService?.NavigateTo("Revenue");
    }

    [RelayCommand]
    private void NavigateToTransaction(RecentTransactionItem? transaction)
    {
        if (transaction == null) return;

        var pageName = transaction.IsIncome ? "Revenue" : "Expenses";
        App.NavigationService?.NavigateTo(pageName, new TransactionNavigationParameter(transaction.Id));
    }

    [RelayCommand]
    private void NavigateToRentals()
    {
        App.NavigationService?.NavigateTo("RentalRecords");
    }

    #endregion

    #region Helper Methods

    private static string FormatCurrency(decimal amount)
    {
        return amount.ToString("C2");
    }

    private static string FormatDate(DateTime date)
    {
        var now = DateTime.Now;
        if (date.Date == now.Date)
            return "Today";
        if (date.Date == now.Date.AddDays(-1))
            return "Yesterday";
        if (date.Date > now.Date.AddDays(-7))
            return date.ToString("dddd");
        return date.ToString("MMM dd, yyyy");
    }

    private static double? CalculatePercentageChange(decimal previous, decimal current)
    {
        // Return null when there's no previous period data to compare against
        if (previous == 0)
        {
            return null;
        }
        return (double)((current - previous) / previous * 100);
    }

    private static string? FormatPercentageChange(double? change)
    {
        if (!change.HasValue)
        {
            return null;
        }
        // Use absolute value since the arrow indicates direction
        return $"{Math.Abs(change.Value):F1}%";
    }

    private static string GetCustomerName(CompanyData data, string? customerId)
    {
        if (string.IsNullOrEmpty(customerId)) return "Unknown";
        var customer = data.Customers.FirstOrDefault(c => c.Id == customerId);
        return customer?.Name ?? "Unknown";
    }

    private static string GetSupplierName(CompanyData data, string? supplierId)
    {
        if (string.IsNullOrEmpty(supplierId)) return "Unknown";
        var supplier = data.Suppliers.FirstOrDefault(s => s.Id == supplierId);
        return supplier?.Name ?? "Unknown";
    }

    private static string GetRentalItemName(CompanyData data, string rentalItemId)
    {
        var item = data.RentalInventory.FirstOrDefault(r => r.Id == rentalItemId);
        return item?.Name ?? "Unknown Item";
    }

    private static int CalculateDaysRemaining(DateTime dueDate)
    {
        var days = (dueDate.Date - DateTime.Now.Date).Days;
        return days;
    }

    #endregion
}

#region View Models for Lists

/// <summary>
/// Represents a recent transaction item for display.
/// </summary>
public class RecentTransactionItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public decimal AmountValue { get; set; }
    public DateTime Date { get; set; }
    public string DateFormatted { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusVariant { get; set; } = "neutral";
    public bool IsIncome { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    // Helper properties for status variant styling
    public bool IsStatusSuccess => StatusVariant == "success";
    public bool IsStatusWarning => StatusVariant == "warning";
    public bool IsStatusError => StatusVariant == "error";
    public bool IsStatusInfo => StatusVariant == "info";
    public bool IsStatusNeutral => StatusVariant == "neutral" || string.IsNullOrEmpty(StatusVariant);
}

/// <summary>
/// Represents an active rental item for display.
/// </summary>
public class ActiveRentalItem
{
    public string Id { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string StartDateFormatted { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string DueDateFormatted { get; set; } = string.Empty;
    public string RateAmount { get; set; } = string.Empty;
    public string RateType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusVariant { get; set; } = "success";
    public int DaysRemaining { get; set; }
    public bool IsOverdue { get; set; }

    public string DaysRemainingText => IsOverdue
        ? $"{Math.Abs(DaysRemaining)} days overdue"
        : DaysRemaining == 0
            ? "Due today"
            : $"{DaysRemaining} days left";
}

/// <summary>
/// Event arguments for Google Sheets export status changes.
/// </summary>
public class GoogleSheetsExportEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets whether an export is currently in progress.
    /// </summary>
    public bool IsExporting { get; set; }

    /// <summary>
    /// Gets or sets whether the export completed successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the URL of the created spreadsheet.
    /// </summary>
    public string? SpreadsheetUrl { get; set; }

    /// <summary>
    /// Gets or sets the error message if the export failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Event arguments for saving a chart as an image.
/// </summary>
public class SaveChartImageEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the identifier of the chart to save.
    /// </summary>
    public string ChartId { get; set; } = string.Empty;
}

/// <summary>
/// Navigation parameter for navigating to a specific transaction.
/// </summary>
public class TransactionNavigationParameter
{
    /// <summary>
    /// Gets the transaction ID to highlight.
    /// </summary>
    public string TransactionId { get; }

    public TransactionNavigationParameter(string transactionId)
    {
        TransactionId = transactionId;
    }
}

#endregion
