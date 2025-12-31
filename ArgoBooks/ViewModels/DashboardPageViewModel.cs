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

    #region Expenses Overview Chart

    private readonly ChartLoaderService _chartLoaderService = new();

    [ObservableProperty]
    private ObservableCollection<ISeries> _expensesChartSeries = [];

    [ObservableProperty]
    private Axis[] _expensesChartXAxes = [];

    [ObservableProperty]
    private Axis[] _expensesChartYAxes = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpensesChartTitleVisual))]
    private string _expensesChartTitle = "Total expenses: $0.00";

    /// <summary>
    /// Gets the visual title element for the expenses chart.
    /// </summary>
    public LabelVisual ExpensesChartTitleVisual => ChartLoaderService.CreateChartTitle(ExpensesChartTitle);

    [ObservableProperty]
    private bool _hasExpensesChartData;

    #endregion

    #region Expense Distribution Chart

    [ObservableProperty]
    private ObservableCollection<ISeries> _expenseDistributionSeries = [];

    [ObservableProperty]
    private bool _hasExpenseDistributionData;

    /// <summary>
    /// Gets the visual title element for the expense distribution chart.
    /// </summary>
    public LabelVisual ExpenseDistributionChartTitle => ChartLoaderService.CreateChartTitle("Distribution of expenses");

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
            OnPropertyChanged(nameof(ExpensesChartTitleVisual));
            OnPropertyChanged(nameof(ExpenseDistributionChartTitle));
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
        LoadExpensesChart(data);
        LoadExpenseDistributionChart(data);
    }

    private void LoadStatistics(CompanyData data)
    {
        var now = DateTime.Now;
        var thisMonth = new DateTime(now.Year, now.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);
        var lastMonthEnd = thisMonth.AddDays(-1);

        // Calculate this month's revenue
        var thisMonthRevenue = data.Sales
            .Where(s => s.Date >= thisMonth && s.Date <= now)
            .Sum(s => s.Total);

        // Calculate last month's revenue for comparison
        var lastMonthRevenue = data.Sales
            .Where(s => s.Date >= lastMonth && s.Date <= lastMonthEnd)
            .Sum(s => s.Total);

        TotalRevenue = FormatCurrency(thisMonthRevenue);
        RevenueChangeValue = CalculatePercentageChange(lastMonthRevenue, thisMonthRevenue);
        RevenueChangeText = FormatPercentageChange(RevenueChangeValue);

        // Calculate this month's expenses
        var thisMonthExpenses = data.Purchases
            .Where(p => p.Date >= thisMonth && p.Date <= now)
            .Sum(p => p.Total);

        // Calculate last month's expenses for comparison
        var lastMonthExpenses = data.Purchases
            .Where(p => p.Date >= lastMonth && p.Date <= lastMonthEnd)
            .Sum(p => p.Total);

        TotalExpenses = FormatCurrency(thisMonthExpenses);
        ExpenseChangeValue = CalculatePercentageChange(lastMonthExpenses, thisMonthExpenses);
        ExpenseChangeText = FormatPercentageChange(ExpenseChangeValue);

        // Calculate net profit
        var netProfitValue = thisMonthRevenue - thisMonthExpenses;
        var lastMonthProfit = lastMonthRevenue - lastMonthExpenses;
        NetProfit = FormatCurrency(Math.Abs(netProfitValue));
        ProfitChangeValue = CalculatePercentageChange(lastMonthProfit, netProfitValue);
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

    private void LoadExpensesChart(CompanyData data)
    {
        // Update theme colors based on current theme
        _chartLoaderService.UpdateThemeColors(ThemeService.Instance.IsDarkTheme);

        // Load expenses chart data for the last 30 days
        var (series, labels, dates, totalExpenses) = _chartLoaderService.LoadExpensesOverviewChart(data);

        ExpensesChartSeries = series;
        ExpensesChartXAxes = _chartLoaderService.CreateDateXAxes(dates);
        ExpensesChartYAxes = _chartLoaderService.CreateCurrencyYAxes();
        ExpensesChartTitle = $"Total expenses: {FormatCurrency(totalExpenses)}";
        HasExpensesChartData = series.Count > 0 && labels.Length > 0;
    }

    private void LoadExpenseDistributionChart(CompanyData data)
    {
        var (series, total) = _chartLoaderService.LoadExpenseDistributionChart(data);
        ExpenseDistributionSeries = series;
        HasExpenseDistributionData = series.Count > 0 && total > 0;
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
        ChartLoaderService.ResetZoom(ExpensesChartXAxes, ExpensesChartYAxes);
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

#endregion
