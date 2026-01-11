using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
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
    /// Gets the shared chart settings service.
    /// </summary>
    private ChartSettingsService ChartSettings => ChartSettingsService.Instance;

    // Flag to prevent duplicate data loading when changing settings locally
    private bool _isLocalSettingChange;

    /// <summary>
    /// Available date range options.
    /// </summary>
    public ObservableCollection<string> DateRangeOptions { get; } = new(DatePresetNames.StandardDateRangeOptions);

    /// <summary>
    /// Gets or sets the selected date range (delegates to shared service).
    /// </summary>
    public string SelectedDateRange
    {
        get => ChartSettings.SelectedDateRange;
        set
        {
            if (ChartSettings.SelectedDateRange != value)
            {
                var oldValue = ChartSettings.SelectedDateRange;
                _isLocalSettingChange = true;
                try
                {
                    ChartSettings.SelectedDateRange = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCustomDateRange));
                    OnPropertyChanged(nameof(ComparisonPeriodLabel));

                    if (value == "Custom Range")
                    {
                        OpenCustomDateRangeModal();
                    }
                    else if (oldValue != value)
                    {
                        HasAppliedCustomRange = false;
                        LoadDashboardData();
                    }
                }
                finally
                {
                    _isLocalSettingChange = false;
                }
            }
        }
    }

    // Stores the previous selection before opening custom range modal
    private string _previousDateRange = "This Month";

    /// <summary>
    /// Gets or sets the start date (delegates to shared service).
    /// </summary>
    public DateTime StartDate
    {
        get => ChartSettings.StartDate;
        set
        {
            if (ChartSettings.StartDate != value)
            {
                ChartSettings.StartDate = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the end date (delegates to shared service).
    /// </summary>
    public DateTime EndDate
    {
        get => ChartSettings.EndDate;
        set
        {
            if (ChartSettings.EndDate != value)
            {
                ChartSettings.EndDate = value;
                OnPropertyChanged();
            }
        }
    }

    // Temporary date values for the modal (before applying)
    [ObservableProperty]
    private DateTime _modalStartDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);

    [ObservableProperty]
    private DateTime _modalEndDate = DateTime.Now;

    /// <summary>
    /// Gets or sets whether the custom date range modal is open.
    /// </summary>
    [ObservableProperty]
    private bool _isCustomDateRangeModalOpen;

    /// <summary>
    /// Gets or sets whether a custom date range has been applied (delegates to shared service).
    /// </summary>
    public bool HasAppliedCustomRange
    {
        get => ChartSettings.HasAppliedCustomRange;
        set
        {
            if (ChartSettings.HasAppliedCustomRange != value)
            {
                ChartSettings.HasAppliedCustomRange = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AppliedDateRangeText));
            }
        }
    }

    /// <summary>
    /// Gets the formatted text showing the applied custom date range.
    /// </summary>
    public string AppliedDateRangeText => ChartSettings.AppliedDateRangeText;

    /// <summary>
    /// Gets or sets the start date as DateTimeOffset for DatePicker binding.
    /// </summary>
    public DateTimeOffset? StartDateOffset
    {
        get => new DateTimeOffset(StartDate);
        set
        {
            if (value.HasValue)
            {
                StartDate = value.Value.DateTime;
                LoadDashboardData();
            }
        }
    }

    /// <summary>
    /// Gets or sets the end date as DateTimeOffset for DatePicker binding.
    /// </summary>
    public DateTimeOffset? EndDateOffset
    {
        get => new DateTimeOffset(EndDate);
        set
        {
            if (value.HasValue)
            {
                EndDate = value.Value.DateTime;
                LoadDashboardData();
            }
        }
    }

    /// <summary>
    /// Gets or sets the modal start date as DateTimeOffset for DatePicker binding.
    /// </summary>
    public DateTimeOffset? ModalStartDateOffset
    {
        get => new DateTimeOffset(ModalStartDate);
        set
        {
            if (value.HasValue)
            {
                ModalStartDate = value.Value.DateTime;
            }
        }
    }

    /// <summary>
    /// Gets or sets the modal end date as DateTimeOffset for DatePicker binding.
    /// </summary>
    public DateTimeOffset? ModalEndDateOffset
    {
        get => new DateTimeOffset(ModalEndDate);
        set
        {
            if (value.HasValue)
            {
                ModalEndDate = value.Value.DateTime;
            }
        }
    }

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

    /// <summary>
    /// Opens the custom date range modal.
    /// </summary>
    [RelayCommand]
    private void OpenCustomDateRangeModal()
    {
        // Store the previous selection before opening the modal
        // so we can restore it if the user cancels
        if (SelectedDateRange != "Custom Range")
        {
            _previousDateRange = SelectedDateRange;
        }

        // Initialize modal dates with current values
        ModalStartDate = StartDate;
        ModalEndDate = EndDate;
        OnPropertyChanged(nameof(ModalStartDateOffset));
        OnPropertyChanged(nameof(ModalEndDateOffset));
        IsCustomDateRangeModalOpen = true;
    }

    /// <summary>
    /// Applies the custom date range from the modal.
    /// </summary>
    [RelayCommand]
    private async Task ApplyCustomDateRange()
    {
        // Check if start date is after end date
        if (ModalStartDate > ModalEndDate)
        {
            var result = await App.ConfirmationDialog!.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Invalid Date Range",
                Message = "The start date is after the end date. Would you like to swap the dates?",
                PrimaryButtonText = "Swap Dates",
                CancelButtonText = "Cancel"
            });

            if (result == ConfirmationResult.Primary)
            {
                // Swap the dates
                (ModalStartDate, ModalEndDate) = (ModalEndDate, ModalStartDate);
                OnPropertyChanged(nameof(ModalStartDateOffset));
                OnPropertyChanged(nameof(ModalEndDateOffset));
            }
            else
            {
                // User cancelled, keep modal open
                return;
            }
        }

        StartDate = ModalStartDate;
        EndDate = ModalEndDate;
        HasAppliedCustomRange = true;
        OnPropertyChanged(nameof(AppliedDateRangeText));
        IsCustomDateRangeModalOpen = false;
        LoadDashboardData();
    }

    /// <summary>
    /// Cancels the custom date range modal.
    /// </summary>
    [RelayCommand]
    private void CancelCustomDateRange()
    {
        IsCustomDateRangeModalOpen = false;

        // If no custom range was previously applied, revert to the previous selection
        if (!HasAppliedCustomRange)
        {
            ChartSettings.SelectedDateRange = _previousDateRange;
            OnPropertyChanged(nameof(SelectedDateRange));
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

    #region Chart Type

    /// <summary>
    /// Available chart type options for the selector.
    /// </summary>
    public string[] ChartTypeOptions { get; } = ["Line", "Column", "Step Line", "Area", "Scatter"];

    /// <summary>
    /// Gets or sets the selected chart type (delegates to shared service).
    /// </summary>
    public string SelectedChartType
    {
        get => ChartSettings.SelectedChartType;
        set
        {
            if (ChartSettings.SelectedChartType != value)
            {
                _isLocalSettingChange = true;
                try
                {
                    ChartSettings.SelectedChartType = value;
                    OnPropertyChanged();
                    LoadDashboardData();
                }
                finally
                {
                    _isLocalSettingChange = false;
                }
            }
        }
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
    /// Gets the visual title element for the expenses vs revenue chart.
    /// </summary>
    public LabelVisual SalesVsExpensesChartTitle => ChartLoaderService.CreateChartTitle("Expenses vs Revenue");

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

        // Initialize the shared chart settings service
        ChartSettings.Initialize();

        // Subscribe to chart settings changes from other pages
        ChartSettings.ChartTypeChanged += OnChartSettingsChartTypeChanged;
        ChartSettings.DateRangeChanged += OnChartSettingsDateRangeChanged;

        // Subscribe to theme changes to reload charts with new colors
        ThemeService.Instance.ThemeChanged += (_, _) =>
        {
            LoadDashboardData();
            // Notify chart titles that have static text (no backing field changes during load)
            OnPropertyChanged(nameof(SalesVsExpensesChartTitle));
        };

        // Subscribe to date format changes to refresh charts and transaction dates
        DateFormatService.DateFormatChanged += (_, _) =>
        {
            LoadDashboardData();
        };

        // Subscribe to currency changes to refresh all monetary displays
        CurrencyService.CurrencyChanged += (_, _) =>
        {
            LoadDashboardData();
        };
    }

    private void OnChartSettingsChartTypeChanged(object? sender, string chartType)
    {
        // Only reload if the change came from another page
        if (!_isLocalSettingChange)
        {
            OnPropertyChanged(nameof(SelectedChartType));
            LoadDashboardData();
        }
    }

    private void OnChartSettingsDateRangeChanged(object? sender, string dateRange)
    {
        // Only reload if the change came from another page
        if (!_isLocalSettingChange)
        {
            OnPropertyChanged(nameof(SelectedDateRange));
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));
            OnPropertyChanged(nameof(HasAppliedCustomRange));
            OnPropertyChanged(nameof(AppliedDateRangeText));
            OnPropertyChanged(nameof(IsCustomDateRange));
            OnPropertyChanged(nameof(ComparisonPeriodLabel));
            LoadDashboardData();
        }
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

        // Subscribe to language changes to refresh translated chart titles
        LanguageService.Instance.LanguageChanged += OnLanguageChanged;
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

        // Unsubscribe from chart settings events
        ChartSettings.ChartTypeChanged -= OnChartSettingsChartTypeChanged;
        ChartSettings.DateRangeChanged -= OnChartSettingsDateRangeChanged;

        // Unsubscribe from language changes
        LanguageService.Instance.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        // Refresh chart titles that use Loc.Tr()
        LoadDashboardData();

        // Notify computed chart title properties to refresh
        OnPropertyChanged(nameof(SalesVsExpensesChartTitle));

        // Force ComboBox to re-render items with new translations
        // by refreshing the collection (items don't change, but triggers re-render)
        var currentSelection = SelectedDateRange;
        DateRangeOptions.Clear();
        foreach (var option in DatePresetNames.StandardDateRangeOptions)
        {
            DateRangeOptions.Add(option);
        }
        // Restore selection without triggering data reload
        _isLocalSettingChange = true;
        try
        {
            ChartSettings.SelectedDateRange = currentSelection;
            OnPropertyChanged(nameof(SelectedDateRange));
        }
        finally
        {
            _isLocalSettingChange = false;
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

        // Correct rental statuses before displaying
        CorrectRentalStatuses(data);

        LoadStatistics(data);
        LoadRecentTransactions(data);
        LoadActiveRentals(data);
        LoadProfitsChart(data);
        LoadSalesVsExpensesChart(data);
    }

    /// <summary>
    /// Corrects rental statuses based on due dates.
    /// </summary>
    private static void CorrectRentalStatuses(CompanyData data)
    {
        // Mark active rentals as overdue if past due
        foreach (var rental in data.Rentals.Where(r => r.Status == RentalStatus.Active))
        {
            if (rental.IsOverdue)
            {
                rental.Status = RentalStatus.Overdue;
            }
        }

        // Reset incorrectly marked overdue rentals back to active if due date is in the future
        foreach (var rental in data.Rentals.Where(r => r.Status == RentalStatus.Overdue))
        {
            if (DateTime.Today <= rental.DueDate.Date)
            {
                rental.Status = RentalStatus.Active;
            }
        }
    }

    private void LoadStatistics(CompanyData data)
    {
        // Calculate comparison period based on selected date range
        var (prevStartDate, prevEndDate) = GetComparisonPeriod();

        // Calculate current period revenue (using USD for consistent calculations)
        var currentRevenueUSD = data.Sales
            .Where(s => s.Date >= StartDate && s.Date <= EndDate)
            .Sum(s => s.EffectiveTotalUSD);

        // Calculate previous period revenue for comparison
        var prevRevenueUSD = data.Sales
            .Where(s => s.Date >= prevStartDate && s.Date <= prevEndDate)
            .Sum(s => s.EffectiveTotalUSD);

        TotalRevenue = FormatCurrencyFromUSD(currentRevenueUSD, DateTime.Now);
        RevenueChangeValue = CalculatePercentageChange(prevRevenueUSD, currentRevenueUSD);
        RevenueChangeText = FormatPercentageChange(RevenueChangeValue);

        // Calculate current period expenses (using USD for consistent calculations)
        var currentExpensesUSD = data.Purchases
            .Where(p => p.Date >= StartDate && p.Date <= EndDate)
            .Sum(p => p.EffectiveTotalUSD);

        // Calculate previous period expenses for comparison
        var prevExpensesUSD = data.Purchases
            .Where(p => p.Date >= prevStartDate && p.Date <= prevEndDate)
            .Sum(p => p.EffectiveTotalUSD);

        TotalExpenses = FormatCurrencyFromUSD(currentExpensesUSD, DateTime.Now);
        ExpenseChangeValue = CalculatePercentageChange(prevExpensesUSD, currentExpensesUSD);
        ExpenseChangeText = FormatPercentageChange(ExpenseChangeValue);

        // Calculate net profit
        var netProfitUSD = currentRevenueUSD - currentExpensesUSD;
        var prevProfitUSD = prevRevenueUSD - prevExpensesUSD;
        NetProfit = FormatCurrencyFromUSD(Math.Abs(netProfitUSD), DateTime.Now);
        ProfitChangeValue = CalculatePercentageChange(prevProfitUSD, netProfitUSD);
        ProfitChangeText = FormatPercentageChange(ProfitChangeValue);

        // Calculate outstanding invoices (using USD)
        var outstandingInvoices = data.Invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .ToList();

        OutstandingInvoiceCount = outstandingInvoices.Count;
        var outstandingAmountUSD = outstandingInvoices.Sum(i => i.EffectiveBalanceUSD);
        OutstandingInvoices = FormatCurrencyFromUSD(outstandingAmountUSD, DateTime.Now);

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

        // Get recent sales (no status badge needed for completed transactions)
        var recentSales = data.Sales
            .OrderByDescending(s => s.Date)
            .Take(10)
            .Select(s => new RecentTransactionItem
            {
                Id = s.Id,
                Type = "Sale",
                Description = string.IsNullOrEmpty(s.Description) ? "Sale Transaction" : s.Description,
                Amount = FormatCurrencyFromUSD(s.EffectiveTotalUSD, s.Date),
                AmountValue = CurrencyService.GetDisplayAmount(s.EffectiveTotalUSD, s.Date),
                Date = s.Date,
                DateFormatted = FormatDate(s.Date),
                Status = string.Empty,
                StatusVariant = string.Empty,
                IsIncome = true,
                CustomerName = GetCustomerName(data, s.CustomerId)
            });

        recentItems.AddRange(recentSales);

        // Get recent purchases/expenses (no status badge needed for completed transactions)
        var recentPurchases = data.Purchases
            .OrderByDescending(p => p.Date)
            .Take(10)
            .Select(p => new RecentTransactionItem
            {
                Id = p.Id,
                Type = "Expense",
                Description = string.IsNullOrEmpty(p.Description) ? "Purchase Transaction" : p.Description,
                Amount = FormatCurrencyFromUSD(p.EffectiveTotalUSD, p.Date),
                AmountValue = CurrencyService.GetDisplayAmount(p.EffectiveTotalUSD, p.Date),
                Date = p.Date,
                DateFormatted = FormatDate(p.Date),
                Status = string.Empty,
                StatusVariant = string.Empty,
                IsIncome = false,
                CustomerName = GetSupplierName(data, p.SupplierId)
            });

        recentItems.AddRange(recentPurchases);

        // Note: Invoices are intentionally excluded from recent transactions

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
        // Update theme colors and chart style
        ChartLoaderService.UpdateThemeColors(ThemeService.Instance.IsDarkTheme);
        ChartLoaderService.SelectedChartStyle = SelectedChartType switch
        {
            "Line" => ChartStyle.Line,
            "Column" => ChartStyle.Column,
            "Step Line" => ChartStyle.StepLine,
            "Area" => ChartStyle.Area,
            "Scatter" => ChartStyle.Scatter,
            _ => ChartStyle.Line
        };

        // Load profits chart data for the selected date range
        var (series, labels, dates, totalProfit) = ChartLoaderService.LoadProfitsOverviewChart(data, StartDate, EndDate);

        ProfitsChartSeries = series;
        ProfitsChartXAxes = ChartLoaderService.CreateDateXAxes(dates);
        ProfitsChartYAxes = ChartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        ProfitsChartTitle = $"Total profits: {FormatCurrencyFromUSD(totalProfit, DateTime.Now)}";
        HasProfitsChartData = series.Count > 0 && labels.Length > 0;
    }

    private void LoadSalesVsExpensesChart(CompanyData data)
    {
        // Update theme colors based on current theme
        ChartLoaderService.UpdateThemeColors(ThemeService.Instance.IsDarkTheme);

        var (series, _, dates) = ChartLoaderService.LoadSalesVsExpensesChart(data, StartDate, EndDate);
        SalesVsExpensesSeries = series;
        SalesVsExpensesXAxes = ChartLoaderService.CreateDateXAxes(dates);
        SalesVsExpensesYAxes = ChartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        HasSalesVsExpensesData = series.Count > 0 && dates.Length > 0;
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
        var exportData = ChartLoaderService.GetGoogleSheetsExportData(SelectedChartId);
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
        if (!GoogleCredentialsManager.AreCredentialsConfigured())
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
            var chartExportData = ChartLoaderService.GetExportDataForChart(SelectedChartId);
            // Use the UI title (SelectedChartId) for Google Sheets, not the internal stored title
            var chartTitle = !string.IsNullOrEmpty(SelectedChartId) ? SelectedChartId : (chartExportData?.ChartTitle ?? "Chart");

            // Use Pie chart type for distribution charts, Line or Column for time-based charts
            ArgoBooks.Core.Services.GoogleSheetsService.ChartType chartType;
            if (chartExportData?.ChartType == ChartType.Distribution)
            {
                chartType = GoogleSheetsService.ChartType.Pie;
            }
            else
            {
                chartType = ChartLoaderService.SelectedChartStyle == ChartStyle.Line
                    ? GoogleSheetsService.ChartType.Line
                    : GoogleSheetsService.ChartType.Column;
            }

            var googleSheetsService = new GoogleSheetsService();
            var url = await googleSheetsService.ExportFormattedDataToGoogleSheetsAsync(
                exportData,
                chartTitle,
                chartType,
                companyName
            );

            if (!string.IsNullOrEmpty(url))
            {
                // Open the spreadsheet in the browser
                var browserOpened = GoogleSheetsService.OpenInBrowser(url);

                GoogleSheetsExportStatusChanged?.Invoke(this, new GoogleSheetsExportEventArgs
                {
                    IsSuccess = true,
                    SpreadsheetUrl = url
                });

                if (!browserOpened)
                {
                    var dialog = App.ConfirmationDialog;
                    if (dialog != null)
                    {
                        await dialog.ShowAsync(new ConfirmationDialogOptions
                        {
                            Title = "Browser Error".Translate(),
                            Message = "The spreadsheet was created but could not open in your browser. You can access it at:\n\n{0}".TranslateFormat(url),
                            PrimaryButtonText = "OK".Translate(),
                            SecondaryButtonText = null,
                            CancelButtonText = null
                        });
                    }
                }
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

    /// <summary>
    /// Event raised when exporting to Excel is requested.
    /// The View should subscribe to this and handle the file save dialog.
    /// </summary>
    public event EventHandler<ExcelExportEventArgs>? ExcelExportRequested;

    /// <inheritdoc />
    protected override void OnExportToExcel()
    {
        var exportData = ChartLoaderService.GetExportDataForChart(SelectedChartId);
        if (exportData == null || exportData.Labels.Length == 0)
        {
            // No data to export
            return;
        }

        // Raise event for View to handle file save dialog
        ExcelExportRequested?.Invoke(this, new ExcelExportEventArgs
        {
            ChartId = SelectedChartId,
            ChartTitle = !string.IsNullOrEmpty(SelectedChartId) ? SelectedChartId : exportData.ChartTitle,
            Labels = exportData.Labels,
            Values = exportData.Values,
            SeriesName = exportData.SeriesName,
            ChartType = exportData.ChartType,
            AdditionalSeries = exportData.AdditionalSeries
        });
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
    public ChartLoaderService ChartLoaderService { get; } = new();

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

    /// <summary>
    /// Formats a currency amount using the current display currency.
    /// For legacy data (USD), converts to current currency if exchange rates are available.
    /// </summary>
    private static string FormatCurrency(decimal amount)
    {
        return CurrencyService.Format(amount);
    }

    /// <summary>
    /// Formats a currency amount from USD using the current display currency with conversion.
    /// </summary>
    private static string FormatCurrencyFromUSD(decimal amountUSD, DateTime date)
    {
        return CurrencyService.FormatFromUSD(amountUSD, date);
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
        return DateFormatService.Format(date);
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
    public bool HasStatus => !string.IsNullOrEmpty(Status);
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
/// Event arguments for Excel export requests.
/// </summary>
public class ExcelExportEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the identifier of the chart to export.
    /// </summary>
    public string ChartId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title of the chart.
    /// </summary>
    public string ChartTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the labels (categories/dates) for the chart data.
    /// </summary>
    public string[] Labels { get; set; } = [];

    /// <summary>
    /// Gets or sets the primary series values.
    /// </summary>
    public double[] Values { get; set; } = [];

    /// <summary>
    /// Gets or sets the name of the primary series.
    /// </summary>
    public string SeriesName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the chart type for export formatting.
    /// </summary>
    public ChartType ChartType { get; set; }

    /// <summary>
    /// Gets or sets additional series for multi-series charts.
    /// </summary>
    public List<(string Name, double[] Values)> AdditionalSeries { get; set; } = [];

    /// <summary>
    /// Returns true if this is a multi-series chart.
    /// </summary>
    public bool IsMultiSeries => AdditionalSeries.Count > 0;

    /// <summary>
    /// Returns true if this is a distribution/pie chart.
    /// </summary>
    public bool IsDistribution => ChartType == ChartType.Distribution;
}

/// <summary>
/// Navigation parameter for navigating to a specific transaction.
/// </summary>
public class TransactionNavigationParameter(string transactionId)
{
    /// <summary>
    /// Gets the transaction ID to highlight.
    /// </summary>
    public string TransactionId { get; } = transactionId;
}

#endregion
