using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Geo;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Analytics page.
/// Handles tab selection, date range filtering, chart type toggling, and chart data loading.
/// </summary>
public partial class AnalyticsPageViewModel : ChartContextMenuViewModelBase
{
    #region Services

    private readonly ChartLoaderService _chartLoaderService = new();
    private CompanyManager? _companyManager;

    #endregion

    #region Tab Selection

    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>
    /// Gets whether the Dashboard tab is selected.
    /// </summary>
    public bool IsDashboardTabSelected => SelectedTabIndex == 0;

    /// <summary>
    /// Gets whether the Geographic tab is selected.
    /// </summary>
    public bool IsGeographicTabSelected => SelectedTabIndex == 1;

    /// <summary>
    /// Gets whether the Operational tab is selected.
    /// </summary>
    public bool IsOperationalTabSelected => SelectedTabIndex == 2;

    /// <summary>
    /// Gets whether the Performance tab is selected.
    /// </summary>
    public bool IsPerformanceTabSelected => SelectedTabIndex == 3;

    /// <summary>
    /// Gets whether the Customers tab is selected.
    /// </summary>
    public bool IsCustomersTabSelected => SelectedTabIndex == 4;

    /// <summary>
    /// Gets whether the Returns tab is selected.
    /// </summary>
    public bool IsReturnsTabSelected => SelectedTabIndex == 5;

    /// <summary>
    /// Gets whether the Losses tab is selected.
    /// </summary>
    public bool IsLossesTabSelected => SelectedTabIndex == 6;

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsDashboardTabSelected));
        OnPropertyChanged(nameof(IsGeographicTabSelected));
        OnPropertyChanged(nameof(IsOperationalTabSelected));
        OnPropertyChanged(nameof(IsPerformanceTabSelected));
        OnPropertyChanged(nameof(IsCustomersTabSelected));
        OnPropertyChanged(nameof(IsReturnsTabSelected));
        OnPropertyChanged(nameof(IsLossesTabSelected));
    }

    #endregion

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
    private DateTime _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

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
        LoadAllCharts();
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

    #endregion

    #region Dashboard Tab Statistics

    [ObservableProperty]
    private string _totalPurchases = "$0.00";

    [ObservableProperty]
    private double? _purchasesChangeValue;

    [ObservableProperty]
    private string? _purchasesChangeText;

    [ObservableProperty]
    private string _totalSales = "$0.00";

    [ObservableProperty]
    private double? _salesChangeValue;

    [ObservableProperty]
    private string? _salesChangeText;

    [ObservableProperty]
    private string _netProfit = "$0.00";

    [ObservableProperty]
    private double? _profitChangeValue;

    [ObservableProperty]
    private string? _profitChangeText;

    [ObservableProperty]
    private string _profitMargin = "0.0%";

    [ObservableProperty]
    private double? _profitMarginChangeValue;

    [ObservableProperty]
    private string? _profitMarginChangeText;

    #endregion

    #region Operational Tab Statistics

    [ObservableProperty]
    private string _activeAccountants = "0";

    [ObservableProperty]
    private string _transactionsProcessed = "0";

    [ObservableProperty]
    private double? _transactionsChangeValue;

    [ObservableProperty]
    private string? _transactionsChangeText;

    [ObservableProperty]
    private string _avgProcessingTime = "N/A";

    [ObservableProperty]
    private double? _processingTimeChangeValue;

    [ObservableProperty]
    private string? _processingTimeChangeText;

    [ObservableProperty]
    private string _accuracyRate = "N/A";

    [ObservableProperty]
    private double? _accuracyChangeValue;

    [ObservableProperty]
    private string? _accuracyChangeText;

    #endregion

    #region Performance Tab Statistics

    [ObservableProperty]
    private string _revenueGrowth = "0.0%";

    [ObservableProperty]
    private double? _revenueGrowthChangeValue;

    [ObservableProperty]
    private string? _revenueGrowthChangeText;

    [ObservableProperty]
    private string _totalTransactions = "0";

    [ObservableProperty]
    private double? _totalTransactionsChangeValue;

    [ObservableProperty]
    private string? _totalTransactionsChangeText;

    [ObservableProperty]
    private string _avgTransactionValue = "$0.00";

    [ObservableProperty]
    private double? _avgTransactionChangeValue;

    [ObservableProperty]
    private string? _avgTransactionChangeText;

    [ObservableProperty]
    private string _avgShippingCost = "$0.00";

    [ObservableProperty]
    private double? _avgShippingChangeValue;

    [ObservableProperty]
    private string? _avgShippingChangeText;

    #endregion

    #region Customers Tab Statistics

    [ObservableProperty]
    private string _totalCustomers = "0";

    [ObservableProperty]
    private double? _customersChangeValue;

    [ObservableProperty]
    private string? _customersChangeText;

    [ObservableProperty]
    private string _newCustomers = "0";

    [ObservableProperty]
    private double? _newCustomersChangeValue;

    [ObservableProperty]
    private string? _newCustomersChangeText;

    [ObservableProperty]
    private string _retentionRate = "N/A";

    [ObservableProperty]
    private double? _retentionChangeValue;

    [ObservableProperty]
    private string? _retentionChangeText;

    [ObservableProperty]
    private string _avgCustomerValue = "$0.00";

    [ObservableProperty]
    private double? _avgCustomerValueChangeValue;

    [ObservableProperty]
    private string? _avgCustomerValueChangeText;

    #endregion

    #region Returns Tab Statistics

    [ObservableProperty]
    private string _totalReturns = "0";

    [ObservableProperty]
    private double? _returnsChangeValue;

    [ObservableProperty]
    private string? _returnsChangeText;

    [ObservableProperty]
    private string _returnRate = "0.0%";

    [ObservableProperty]
    private double? _returnRateChangeValue;

    [ObservableProperty]
    private string? _returnRateChangeText;

    [ObservableProperty]
    private string _returnsFinancialImpact = "$0.00";

    [ObservableProperty]
    private double? _returnsImpactChangeValue;

    [ObservableProperty]
    private string? _returnsImpactChangeText;

    [ObservableProperty]
    private string _avgResolutionTime = "N/A";

    [ObservableProperty]
    private double? _resolutionTimeChangeValue;

    [ObservableProperty]
    private string? _resolutionTimeChangeText;

    #endregion

    #region Losses Tab Statistics

    [ObservableProperty]
    private string _totalLosses = "0";

    [ObservableProperty]
    private double? _lossesChangeValue;

    [ObservableProperty]
    private string? _lossesChangeText;

    [ObservableProperty]
    private string _lossRate = "0.0%";

    [ObservableProperty]
    private double? _lossRateChangeValue;

    [ObservableProperty]
    private string? _lossRateChangeText;

    [ObservableProperty]
    private string _lossesFinancialImpact = "$0.00";

    [ObservableProperty]
    private double? _lossesImpactChangeValue;

    [ObservableProperty]
    private string? _lossesImpactChangeText;

    [ObservableProperty]
    private string _insuranceClaims = "0";

    [ObservableProperty]
    private double? _insuranceClaimsChangeValue;

    [ObservableProperty]
    private string? _insuranceClaimsChangeText;

    #endregion

    #region Chart Type Toggle

    [ObservableProperty]
    private bool _useLineChart = true;

    partial void OnUseLineChartChanged(bool value)
    {
        LoadAllCharts();
    }

    #endregion

    #region Map Mode Toggle

    [ObservableProperty]
    private bool _isMapModeOrigin = true;

    /// <summary>
    /// Gets or sets whether the map mode is Destination.
    /// </summary>
    public bool IsMapModeDestination
    {
        get => !IsMapModeOrigin;
        set
        {
            if (value != !IsMapModeOrigin)
            {
                IsMapModeOrigin = !value;
            }
        }
    }

    partial void OnIsMapModeOriginChanged(bool value)
    {
        OnPropertyChanged(nameof(IsMapModeDestination));
        LoadGeoMapChart();
    }

    #endregion

    #region Dashboard Charts - Expenses/Purchases

    [ObservableProperty]
    private ObservableCollection<ISeries> _expensesTrendsSeries = [];

    [ObservableProperty]
    private Axis[] _expensesTrendsXAxes = [];

    [ObservableProperty]
    private Axis[] _expensesTrendsYAxes = [];

    [ObservableProperty]
    private bool _hasExpensesTrendsData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _expensesDistributionSeries = [];

    [ObservableProperty]
    private bool _hasExpensesDistributionData;

    #endregion

    #region Dashboard Charts - Revenue/Sales

    [ObservableProperty]
    private ObservableCollection<ISeries> _revenueTrendsSeries = [];

    [ObservableProperty]
    private Axis[] _revenueTrendsXAxes = [];

    [ObservableProperty]
    private Axis[] _revenueTrendsYAxes = [];

    [ObservableProperty]
    private bool _hasRevenueTrendsData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _revenueDistributionSeries = [];

    [ObservableProperty]
    private bool _hasRevenueDistributionData;

    #endregion

    #region Dashboard Charts - Profits

    [ObservableProperty]
    private ObservableCollection<ISeries> _profitTrendsSeries = [];

    [ObservableProperty]
    private Axis[] _profitTrendsXAxes = [];

    [ObservableProperty]
    private Axis[] _profitTrendsYAxes = [];

    [ObservableProperty]
    private bool _hasProfitTrendsData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _salesVsExpensesSeries = [];

    [ObservableProperty]
    private Axis[] _salesVsExpensesXAxes = [];

    [ObservableProperty]
    private Axis[] _salesVsExpensesYAxes = [];

    [ObservableProperty]
    private bool _hasSalesVsExpensesData;

    #endregion

    #region Geographic Charts

    [ObservableProperty]
    private ObservableCollection<ISeries> _countriesOfOriginSeries = [];

    [ObservableProperty]
    private bool _hasCountriesOfOriginData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _companiesOfOriginSeries = [];

    [ObservableProperty]
    private bool _hasCompaniesOfOriginData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _countriesOfDestinationSeries = [];

    [ObservableProperty]
    private bool _hasCountriesOfDestinationData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _companiesOfDestinationSeries = [];

    [ObservableProperty]
    private bool _hasCompaniesOfDestinationData;

    [ObservableProperty]
    private Dictionary<string, double> _worldMapData = new();

    [ObservableProperty]
    private ObservableCollection<IGeoSeries> _geoMapSeries = [];

    [ObservableProperty]
    private bool _hasWorldMapData;

    #endregion

    #region Operational Charts

    [ObservableProperty]
    private ObservableCollection<ISeries> _avgTransactionValueSeries = [];

    [ObservableProperty]
    private Axis[] _avgTransactionValueXAxes = [];

    [ObservableProperty]
    private Axis[] _avgTransactionValueYAxes = [];

    [ObservableProperty]
    private bool _hasAvgTransactionValueData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _totalTransactionsSeries = [];

    [ObservableProperty]
    private Axis[] _totalTransactionsXAxes = [];

    [ObservableProperty]
    private Axis[] _totalTransactionsYAxes = [];

    [ObservableProperty]
    private bool _hasTotalTransactionsData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _avgShippingCostsSeries = [];

    [ObservableProperty]
    private Axis[] _avgShippingCostsXAxes = [];

    [ObservableProperty]
    private Axis[] _avgShippingCostsYAxes = [];

    [ObservableProperty]
    private bool _hasAvgShippingCostsData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _accountantsTransactionsSeries = [];

    [ObservableProperty]
    private bool _hasAccountantsTransactionsData;

    #endregion

    #region Performance Charts

    [ObservableProperty]
    private ObservableCollection<ISeries> _growthRatesSeries = [];

    [ObservableProperty]
    private Axis[] _growthRatesXAxes = [];

    [ObservableProperty]
    private Axis[] _growthRatesYAxes = [];

    [ObservableProperty]
    private bool _hasGrowthRatesData;

    #endregion

    #region Returns Charts

    [ObservableProperty]
    private ObservableCollection<ISeries> _returnsOverTimeSeries = [];

    [ObservableProperty]
    private Axis[] _returnsOverTimeXAxes = [];

    [ObservableProperty]
    private Axis[] _returnsOverTimeYAxes = [];

    [ObservableProperty]
    private bool _hasReturnsOverTimeData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _returnReasonsSeries = [];

    [ObservableProperty]
    private bool _hasReturnReasonsData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _returnFinancialImpactSeries = [];

    [ObservableProperty]
    private Axis[] _returnFinancialImpactXAxes = [];

    [ObservableProperty]
    private Axis[] _returnFinancialImpactYAxes = [];

    [ObservableProperty]
    private bool _hasReturnFinancialImpactData;

    #endregion

    #region Customer Charts

    [ObservableProperty]
    private ObservableCollection<ISeries> _customerPaymentStatusSeries = [];

    [ObservableProperty]
    private bool _hasCustomerPaymentStatusData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _activeInactiveCustomersSeries = [];

    [ObservableProperty]
    private bool _hasActiveInactiveCustomersData;

    #endregion

    #region Losses Charts

    [ObservableProperty]
    private ObservableCollection<ISeries> _lossesOverTimeSeries = [];

    [ObservableProperty]
    private Axis[] _lossesOverTimeXAxes = [];

    [ObservableProperty]
    private Axis[] _lossesOverTimeYAxes = [];

    [ObservableProperty]
    private bool _hasLossesOverTimeData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _lossFinancialImpactSeries = [];

    [ObservableProperty]
    private Axis[] _lossFinancialImpactXAxes = [];

    [ObservableProperty]
    private Axis[] _lossFinancialImpactYAxes = [];

    [ObservableProperty]
    private bool _hasLossFinancialImpactData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _lossReasonsSeries = [];

    [ObservableProperty]
    private bool _hasLossReasonsData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _lossesByProductSeries = [];

    [ObservableProperty]
    private bool _hasLossesByProductData;

    #endregion

    #region Chart Titles

    // Dashboard Tab Chart Titles
    public LabelVisual ProfitOverTimeTitle => ChartLoaderService.CreateChartTitle("Profit Over Time");
    public LabelVisual SalesVsExpensesTitle => ChartLoaderService.CreateChartTitle("Sales vs Expenses");
    public LabelVisual SalesTrendsTitle => ChartLoaderService.CreateChartTitle("Sales Trends");
    public LabelVisual SalesDistributionTitle => ChartLoaderService.CreateChartTitle("Sales Distribution");
    public LabelVisual PurchaseTrendsTitle => ChartLoaderService.CreateChartTitle("Purchase Trends");
    public LabelVisual PurchaseDistributionTitle => ChartLoaderService.CreateChartTitle("Purchase Distribution");

    // Geographic Tab Chart Titles
    public LabelVisual CountriesOfOriginTitle => ChartLoaderService.CreateChartTitle("Countries of Origin");
    public LabelVisual CompaniesOfOriginTitle => ChartLoaderService.CreateChartTitle("Companies of Origin");
    public LabelVisual CountriesOfDestinationTitle => ChartLoaderService.CreateChartTitle("Countries of Destination");
    public LabelVisual CompaniesOfDestinationTitle => ChartLoaderService.CreateChartTitle("Companies of Destination");
    public LabelVisual WorldMapOverviewTitle => ChartLoaderService.CreateChartTitle("World Map Overview");

    // Operational Tab Chart Titles
    public LabelVisual TransactionsByAccountantTitle => ChartLoaderService.CreateChartTitle("Transactions by Accountant");
    public LabelVisual WorkloadDistributionTitle => ChartLoaderService.CreateChartTitle("Workload Distribution");
    public LabelVisual ProcessingTimeTrendsTitle => ChartLoaderService.CreateChartTitle("Processing Time Trends");

    // Performance Tab Chart Titles
    public LabelVisual AverageTransactionValueTitle => ChartLoaderService.CreateChartTitle("Average Transaction Value");
    public LabelVisual TotalTransactionsTitle => ChartLoaderService.CreateChartTitle("Total Transactions");
    public LabelVisual AverageShippingCostsTitle => ChartLoaderService.CreateChartTitle("Average Shipping Costs");
    public LabelVisual GrowthRatesTitle => ChartLoaderService.CreateChartTitle("Growth Rates");

    // Customers Tab Chart Titles
    public LabelVisual TopCustomersByRevenueTitle => ChartLoaderService.CreateChartTitle("Top Customers by Revenue");
    public LabelVisual CustomerPaymentStatusTitle => ChartLoaderService.CreateChartTitle("Customer Payment Status");
    public LabelVisual CustomerGrowthTitle => ChartLoaderService.CreateChartTitle("Customer Growth");
    public LabelVisual CustomerLifetimeValueTitle => ChartLoaderService.CreateChartTitle("Customer Lifetime Value");
    public LabelVisual ActiveVsInactiveCustomersTitle => ChartLoaderService.CreateChartTitle("Active vs Inactive Customers");
    public LabelVisual RentalsPerCustomerTitle => ChartLoaderService.CreateChartTitle("Rentals per Customer");

    // Returns Tab Chart Titles
    public LabelVisual ReturnsOverTimeTitle => ChartLoaderService.CreateChartTitle("Returns Over Time");
    public LabelVisual ReturnReasonsTitle => ChartLoaderService.CreateChartTitle("Return Reasons");
    public LabelVisual FinancialImpactOfReturnsTitle => ChartLoaderService.CreateChartTitle("Financial Impact of Returns");
    public LabelVisual ReturnsByCategoryTitle => ChartLoaderService.CreateChartTitle("Returns by Category");
    public LabelVisual ReturnsByProductTitle => ChartLoaderService.CreateChartTitle("Returns by Product");
    public LabelVisual PurchaseVsSaleReturnsTitle => ChartLoaderService.CreateChartTitle("Purchase vs Sale Returns");

    // Losses Tab Chart Titles
    public LabelVisual LossesOverTimeTitle => ChartLoaderService.CreateChartTitle("Losses Over Time");
    public LabelVisual LossReasonsTitle => ChartLoaderService.CreateChartTitle("Loss Reasons");
    public LabelVisual FinancialImpactOfLossesTitle => ChartLoaderService.CreateChartTitle("Financial Impact of Losses");
    public LabelVisual LossesByCategoryTitle => ChartLoaderService.CreateChartTitle("Losses by Category");
    public LabelVisual LossesByProductTitle => ChartLoaderService.CreateChartTitle("Losses by Product");
    public LabelVisual PurchaseVsSaleLossesTitle => ChartLoaderService.CreateChartTitle("Purchase vs Sale Losses");

    /// <summary>
    /// Notifies all chart title properties changed (for theme updates).
    /// </summary>
    private void NotifyAllChartTitlesChanged()
    {
        OnPropertyChanged(nameof(ProfitOverTimeTitle));
        OnPropertyChanged(nameof(SalesVsExpensesTitle));
        OnPropertyChanged(nameof(SalesTrendsTitle));
        OnPropertyChanged(nameof(SalesDistributionTitle));
        OnPropertyChanged(nameof(PurchaseTrendsTitle));
        OnPropertyChanged(nameof(PurchaseDistributionTitle));
        OnPropertyChanged(nameof(CountriesOfOriginTitle));
        OnPropertyChanged(nameof(CompaniesOfOriginTitle));
        OnPropertyChanged(nameof(CountriesOfDestinationTitle));
        OnPropertyChanged(nameof(CompaniesOfDestinationTitle));
        OnPropertyChanged(nameof(WorldMapOverviewTitle));
        OnPropertyChanged(nameof(TransactionsByAccountantTitle));
        OnPropertyChanged(nameof(WorkloadDistributionTitle));
        OnPropertyChanged(nameof(ProcessingTimeTrendsTitle));
        OnPropertyChanged(nameof(AverageTransactionValueTitle));
        OnPropertyChanged(nameof(TotalTransactionsTitle));
        OnPropertyChanged(nameof(AverageShippingCostsTitle));
        OnPropertyChanged(nameof(GrowthRatesTitle));
        OnPropertyChanged(nameof(TopCustomersByRevenueTitle));
        OnPropertyChanged(nameof(CustomerPaymentStatusTitle));
        OnPropertyChanged(nameof(CustomerGrowthTitle));
        OnPropertyChanged(nameof(CustomerLifetimeValueTitle));
        OnPropertyChanged(nameof(ActiveVsInactiveCustomersTitle));
        OnPropertyChanged(nameof(RentalsPerCustomerTitle));
        OnPropertyChanged(nameof(ReturnsOverTimeTitle));
        OnPropertyChanged(nameof(ReturnReasonsTitle));
        OnPropertyChanged(nameof(FinancialImpactOfReturnsTitle));
        OnPropertyChanged(nameof(ReturnsByCategoryTitle));
        OnPropertyChanged(nameof(ReturnsByProductTitle));
        OnPropertyChanged(nameof(PurchaseVsSaleReturnsTitle));
        OnPropertyChanged(nameof(LossesOverTimeTitle));
        OnPropertyChanged(nameof(LossReasonsTitle));
        OnPropertyChanged(nameof(FinancialImpactOfLossesTitle));
        OnPropertyChanged(nameof(LossesByCategoryTitle));
        OnPropertyChanged(nameof(LossesByProductTitle));
        OnPropertyChanged(nameof(PurchaseVsSaleLossesTitle));
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public AnalyticsPageViewModel()
    {
        // Initialize with default values
        UpdateDateRangeFromSelection();

        // Subscribe to theme changes to update legend text color and chart titles
        ThemeService.Instance.ThemeChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(LegendTextPaint));
            NotifyAllChartTitlesChanged();
        };
    }

    /// <summary>
    /// Gets the legend text paint based on the current theme.
    /// </summary>
    public SolidColorPaint LegendTextPaint => ChartLoaderService.GetLegendTextPaint();

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
            var chartTitle = chartExportData?.ChartTitle ?? "Chart";

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

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the ViewModel with the company manager.
    /// </summary>
    public void Initialize(CompanyManager companyManager)
    {
        _companyManager = companyManager;
        LoadAllCharts();

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
        LoadAllCharts();
    }

    #endregion

    #region Chart Loading

    /// <summary>
    /// Loads all chart data.
    /// </summary>
    public void LoadAllCharts()
    {
        var data = _companyManager?.CompanyData;
        if (data == null) return;

        // Update theme colors and chart type
        _chartLoaderService.UpdateThemeColors(ThemeService.Instance.IsDarkTheme);
        _chartLoaderService.UseLineChart = UseLineChart;

        // Load statistics for stat cards
        LoadAllStatistics(data);

        // Dashboard charts
        LoadExpensesTrendsChart(data);
        LoadExpensesDistributionChart(data);
        LoadRevenueTrendsChart(data);
        LoadRevenueDistributionChart(data);
        LoadProfitTrendsChart(data);
        LoadSalesVsExpensesChart(data);

        // Geographic charts
        LoadCountriesOfOriginChart(data);
        LoadCompaniesOfOriginChart(data);
        LoadCountriesOfDestinationChart(data);
        LoadCompaniesOfDestinationChart(data);
        LoadGeoMapChart();

        // Operational charts
        LoadAvgTransactionValueChart(data);
        LoadTotalTransactionsChart(data);
        LoadAvgShippingCostsChart(data);
        LoadAccountantsTransactionsChart(data);

        // Performance charts
        LoadGrowthRatesChart(data);

        // Customer charts
        LoadCustomerPaymentStatusChart(data);
        LoadActiveInactiveCustomersChart(data);

        // Returns charts
        LoadReturnsOverTimeChart(data);
        LoadReturnReasonsChart(data);
        LoadReturnFinancialImpactChart(data);

        // Losses charts
        LoadLossesOverTimeChart(data);
        LoadLossFinancialImpactChart(data);
        LoadLossReasonsChart(data);
        LoadLossesByProductChart(data);
    }

    private void LoadExpensesTrendsChart(CompanyData data)
    {
        var (series, labels, dates, _) = _chartLoaderService.LoadExpensesOverviewChart(data, StartDate, EndDate);
        ExpensesTrendsSeries = series;
        ExpensesTrendsXAxes = _chartLoaderService.CreateDateXAxes(dates);
        ExpensesTrendsYAxes = _chartLoaderService.CreateCurrencyYAxes();
        HasExpensesTrendsData = series.Count > 0;
    }

    private void LoadExpensesDistributionChart(CompanyData data)
    {
        var (series, _) = _chartLoaderService.LoadExpenseDistributionChart(data, StartDate, EndDate);
        ExpensesDistributionSeries = series;
        HasExpensesDistributionData = series.Count > 0;
    }

    private void LoadRevenueTrendsChart(CompanyData data)
    {
        var (series, labels, dates, _) = _chartLoaderService.LoadRevenueOverviewChart(data, StartDate, EndDate);
        RevenueTrendsSeries = series;
        RevenueTrendsXAxes = _chartLoaderService.CreateDateXAxes(dates);
        RevenueTrendsYAxes = _chartLoaderService.CreateCurrencyYAxes();
        HasRevenueTrendsData = series.Count > 0;
    }

    private void LoadRevenueDistributionChart(CompanyData data)
    {
        var (series, _) = _chartLoaderService.LoadRevenueDistributionChart(data, StartDate, EndDate);
        RevenueDistributionSeries = series;
        HasRevenueDistributionData = series.Count > 0;
    }

    private void LoadProfitTrendsChart(CompanyData data)
    {
        var (series, labels, dates, _) = _chartLoaderService.LoadProfitsOverviewChart(data, StartDate, EndDate);
        ProfitTrendsSeries = series;
        ProfitTrendsXAxes = _chartLoaderService.CreateDateXAxes(dates);
        ProfitTrendsYAxes = _chartLoaderService.CreateCurrencyYAxes();
        HasProfitTrendsData = series.Count > 0;
    }

    private void LoadSalesVsExpensesChart(CompanyData data)
    {
        var (series, labels) = _chartLoaderService.LoadSalesVsExpensesChart(data, StartDate, EndDate);
        SalesVsExpensesSeries = series;
        SalesVsExpensesXAxes = _chartLoaderService.CreateXAxes(labels);
        SalesVsExpensesYAxes = _chartLoaderService.CreateCurrencyYAxes();
        HasSalesVsExpensesData = series.Count > 0;
    }

    private void LoadCountriesOfOriginChart(CompanyData data)
    {
        var (series, _) = _chartLoaderService.LoadCountriesOfOriginChart(data, StartDate, EndDate);
        CountriesOfOriginSeries = series;
        HasCountriesOfOriginData = series.Count > 0;
    }

    private void LoadCompaniesOfOriginChart(CompanyData data)
    {
        var (series, _) = _chartLoaderService.LoadCompaniesOfOriginChart(data, StartDate, EndDate);
        CompaniesOfOriginSeries = series;
        HasCompaniesOfOriginData = series.Count > 0;
    }

    private void LoadCountriesOfDestinationChart(CompanyData data)
    {
        var (series, _) = _chartLoaderService.LoadCountriesOfDestinationChart(data, StartDate, EndDate);
        CountriesOfDestinationSeries = series;
        HasCountriesOfDestinationData = series.Count > 0;
    }

    private void LoadCompaniesOfDestinationChart(CompanyData data)
    {
        // For companies of destination, we use the accountants transactions chart (purchase-side)
        var (series, _) = _chartLoaderService.LoadAccountantsTransactionsChart(data, StartDate, EndDate);
        CompaniesOfDestinationSeries = series;
        HasCompaniesOfDestinationData = series.Count > 0;
    }

    private void LoadGeoMapChart()
    {
        var data = _companyManager?.CompanyData;
        if (data == null) return;

        // Load data based on mode (origin = customers, destination = suppliers)
        if (IsMapModeOrigin)
        {
            WorldMapData = _chartLoaderService.LoadWorldMapData(data, StartDate, EndDate);
        }
        else
        {
            WorldMapData = _chartLoaderService.LoadWorldMapDataBySupplier(data, StartDate, EndDate);
        }

        HasWorldMapData = WorldMapData.Count > 0;

        // Create HeatLandSeries for the GeoMap
        var geoSeries = new ObservableCollection<IGeoSeries>();
        if (HasWorldMapData)
        {
            var lands = WorldMapData.Select(kvp => new HeatLand
            {
                Name = kvp.Key,
                Value = kvp.Value
            }).ToArray();

            geoSeries.Add(new HeatLandSeries
            {
                Lands = lands
            });
        }
        GeoMapSeries = geoSeries;
    }

    private void LoadAvgTransactionValueChart(CompanyData data)
    {
        var (series, labels) = _chartLoaderService.LoadAverageTransactionValueChart(data, StartDate, EndDate);
        AvgTransactionValueSeries = series;
        AvgTransactionValueXAxes = _chartLoaderService.CreateXAxes(labels);
        AvgTransactionValueYAxes = _chartLoaderService.CreateCurrencyYAxes();
        HasAvgTransactionValueData = series.Count > 0;
    }

    private void LoadTotalTransactionsChart(CompanyData data)
    {
        var (series, labels) = _chartLoaderService.LoadTotalTransactionsChart(data, StartDate, EndDate);
        TotalTransactionsSeries = series;
        TotalTransactionsXAxes = _chartLoaderService.CreateXAxes(labels);
        TotalTransactionsYAxes = _chartLoaderService.CreateNumberYAxes();
        HasTotalTransactionsData = series.Count > 0;
    }

    private void LoadAvgShippingCostsChart(CompanyData data)
    {
        var (series, labels) = _chartLoaderService.LoadAverageShippingCostsChart(data, StartDate, EndDate);
        AvgShippingCostsSeries = series;
        AvgShippingCostsXAxes = _chartLoaderService.CreateXAxes(labels);
        AvgShippingCostsYAxes = _chartLoaderService.CreateCurrencyYAxes();
        HasAvgShippingCostsData = series.Count > 0;
    }

    private void LoadAccountantsTransactionsChart(CompanyData data)
    {
        var (series, _) = _chartLoaderService.LoadAccountantsTransactionsChart(data, StartDate, EndDate);
        AccountantsTransactionsSeries = series;
        HasAccountantsTransactionsData = series.Count > 0;
    }

    private void LoadGrowthRatesChart(CompanyData data)
    {
        var (series, labels) = _chartLoaderService.LoadGrowthRatesChart(data, StartDate, EndDate);
        GrowthRatesSeries = series;
        GrowthRatesXAxes = _chartLoaderService.CreateXAxes(labels);
        GrowthRatesYAxes = _chartLoaderService.CreateNumberYAxes();
        HasGrowthRatesData = series.Count > 0;
    }

    private void LoadReturnsOverTimeChart(CompanyData data)
    {
        var (series, labels, dates, _) = _chartLoaderService.LoadReturnsOverTimeChart(data, StartDate, EndDate);
        ReturnsOverTimeSeries = series;
        ReturnsOverTimeXAxes = _chartLoaderService.CreateDateXAxes(dates);
        ReturnsOverTimeYAxes = _chartLoaderService.CreateNumberYAxes();
        HasReturnsOverTimeData = series.Count > 0;
    }

    private void LoadReturnReasonsChart(CompanyData data)
    {
        var (series, _) = _chartLoaderService.LoadReturnReasonsChart(data, StartDate, EndDate);
        ReturnReasonsSeries = series;
        HasReturnReasonsData = series.Count > 0;
    }

    private void LoadReturnFinancialImpactChart(CompanyData data)
    {
        var (series, labels, _) = _chartLoaderService.LoadReturnFinancialImpactChart(data, StartDate, EndDate);
        ReturnFinancialImpactSeries = series;
        ReturnFinancialImpactXAxes = _chartLoaderService.CreateXAxes(labels);
        ReturnFinancialImpactYAxes = _chartLoaderService.CreateCurrencyYAxes();
        HasReturnFinancialImpactData = series.Count > 0;
    }

    private void LoadLossesOverTimeChart(CompanyData data)
    {
        var (series, labels, dates, _) = _chartLoaderService.LoadLossesOverTimeChart(data, StartDate, EndDate);
        LossesOverTimeSeries = series;
        LossesOverTimeXAxes = _chartLoaderService.CreateDateXAxes(dates);
        LossesOverTimeYAxes = _chartLoaderService.CreateNumberYAxes();
        HasLossesOverTimeData = series.Count > 0;
    }

    private void LoadLossFinancialImpactChart(CompanyData data)
    {
        var (series, labels, _) = _chartLoaderService.LoadLossFinancialImpactChart(data, StartDate, EndDate);
        LossFinancialImpactSeries = series;
        LossFinancialImpactXAxes = _chartLoaderService.CreateXAxes(labels);
        LossFinancialImpactYAxes = _chartLoaderService.CreateCurrencyYAxes();
        HasLossFinancialImpactData = series.Count > 0;
    }

    private void LoadCustomerPaymentStatusChart(CompanyData data)
    {
        var (series, _) = _chartLoaderService.LoadCustomerPaymentStatusChart(data, StartDate, EndDate);
        CustomerPaymentStatusSeries = series;
        HasCustomerPaymentStatusData = series.Count > 0;
    }

    private void LoadActiveInactiveCustomersChart(CompanyData data)
    {
        var (series, _) = _chartLoaderService.LoadActiveInactiveCustomersChart(data, StartDate, EndDate);
        ActiveInactiveCustomersSeries = series;
        HasActiveInactiveCustomersData = series.Count > 0;
    }

    private void LoadLossReasonsChart(CompanyData data)
    {
        var (series, _) = _chartLoaderService.LoadLossReasonsChart(data, StartDate, EndDate);
        LossReasonsSeries = series;
        HasLossReasonsData = series.Count > 0;
    }

    private void LoadLossesByProductChart(CompanyData data)
    {
        var (series, _) = _chartLoaderService.LoadLossesByProductChart(data, StartDate, EndDate);
        LossesByProductSeries = series;
        HasLossesByProductData = series.Count > 0;
    }

    #endregion

    #region Statistics Loading

    /// <summary>
    /// Loads all statistics for stat cards across all tabs.
    /// </summary>
    private void LoadAllStatistics(CompanyData data)
    {
        LoadDashboardStatistics(data);
        LoadOperationalStatistics(data);
        LoadPerformanceStatistics(data);
        LoadCustomerStatistics(data);
        LoadReturnsStatistics(data);
        LoadLossesStatistics(data);
    }

    private void LoadDashboardStatistics(CompanyData data)
    {
        // Filter transactions by date range
        var purchases = data.Purchases?.Where(p => p.Date >= StartDate && p.Date <= EndDate).ToList() ?? [];
        var sales = data.Sales?.Where(s => s.Date >= StartDate && s.Date <= EndDate).ToList() ?? [];

        // Calculate totals
        var totalPurchasesAmount = purchases.Sum(p => p.Total);
        var totalSalesAmount = sales.Sum(s => s.Total);
        var netProfit = totalSalesAmount - totalPurchasesAmount;
        var margin = totalSalesAmount > 0 ? (netProfit / totalSalesAmount) * 100 : 0;

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        var prevPurchases = data.Purchases?.Where(p => p.Date >= prevStartDate && p.Date <= prevEndDate).Sum(p => p.Total) ?? 0;
        var prevSales = data.Sales?.Where(s => s.Date >= prevStartDate && s.Date <= prevEndDate).Sum(s => s.Total) ?? 0;
        var prevNetProfit = prevSales - prevPurchases;
        var prevMargin = prevSales > 0 ? (prevNetProfit / prevSales) * 100 : 0;

        // Check if there's any previous period data to compare against
        var hasPrevPeriodData = prevPurchases > 0 || prevSales > 0;

        // Calculate change percentages (only meaningful if there's previous data)
        var purchasesChange = prevPurchases > 0 ? ((totalPurchasesAmount - prevPurchases) / prevPurchases) * 100 : 0;
        var salesChange = prevSales > 0 ? ((totalSalesAmount - prevSales) / prevSales) * 100 : 0;
        var profitChange = prevNetProfit != 0 ? ((netProfit - prevNetProfit) / Math.Abs(prevNetProfit)) * 100 : 0;
        var marginChange = margin - prevMargin;

        // Update properties
        TotalPurchases = totalPurchasesAmount.ToString("C0");
        PurchasesChangeValue = hasPrevPeriodData && prevPurchases > 0 ? (double)purchasesChange : null;
        PurchasesChangeText = hasPrevPeriodData && prevPurchases > 0 ? $"{Math.Abs(purchasesChange):F1}%" : null;

        TotalSales = totalSalesAmount.ToString("C0");
        SalesChangeValue = hasPrevPeriodData && prevSales > 0 ? (double)salesChange : null;
        SalesChangeText = hasPrevPeriodData && prevSales > 0 ? $"{Math.Abs(salesChange):F1}%" : null;

        NetProfit = netProfit.ToString("C0");
        ProfitChangeValue = hasPrevPeriodData && prevNetProfit != 0 ? (double)profitChange : null;
        ProfitChangeText = hasPrevPeriodData && prevNetProfit != 0 ? $"{Math.Abs(profitChange):F1}%" : null;

        ProfitMargin = $"{margin:F1}%";
        ProfitMarginChangeValue = hasPrevPeriodData && prevSales > 0 ? (double)marginChange : null;
        ProfitMarginChangeText = hasPrevPeriodData && prevSales > 0 ? $"{Math.Abs(marginChange):F1}%" : null;
    }

    private void LoadOperationalStatistics(CompanyData data)
    {
        // Count all accountants (no Status property on Accountant)
        var accountantsCount = data.Accountants?.Count ?? 0;
        ActiveAccountants = accountantsCount.ToString();

        // Transactions processed in the date range
        var purchases = data.Purchases?.Where(p => p.Date >= StartDate && p.Date <= EndDate).ToList() ?? [];
        var sales = data.Sales?.Where(s => s.Date >= StartDate && s.Date <= EndDate).ToList() ?? [];
        var totalTransactions = purchases.Count + sales.Count;

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        var prevPurchasesCount = data.Purchases?.Count(p => p.Date >= prevStartDate && p.Date <= prevEndDate) ?? 0;
        var prevSalesCount = data.Sales?.Count(s => s.Date >= prevStartDate && s.Date <= prevEndDate) ?? 0;
        var prevTransactions = prevPurchasesCount + prevSalesCount;

        var hasPrevPeriodData = prevTransactions > 0;
        var transactionsChange = prevTransactions > 0 ? ((double)(totalTransactions - prevTransactions) / prevTransactions) * 100 : 0;

        TransactionsProcessed = totalTransactions.ToString("N0");
        TransactionsChangeValue = hasPrevPeriodData ? transactionsChange : null;
        TransactionsChangeText = hasPrevPeriodData ? $"{(transactionsChange >= 0 ? "+" : "")}{transactionsChange:F1}%" : null;

        // Processing time and accuracy rate are not tracked in the data model
        AvgProcessingTime = "N/A";
        ProcessingTimeChangeValue = null;
        ProcessingTimeChangeText = null;

        AccuracyRate = "N/A";
        AccuracyChangeValue = null;
        AccuracyChangeText = null;
    }

    private void LoadPerformanceStatistics(CompanyData data)
    {
        // Filter transactions by date range
        var sales = data.Sales?.Where(s => s.Date >= StartDate && s.Date <= EndDate).ToList() ?? [];
        var purchases = data.Purchases?.Where(p => p.Date >= StartDate && p.Date <= EndDate).ToList() ?? [];

        var totalTransactionsCount = sales.Count + purchases.Count;
        var allTransactionValues = sales.Select(s => s.Total).Concat(purchases.Select(p => p.Total)).ToList();
        var avgTransactionValue = allTransactionValues.Count > 0 ? allTransactionValues.Average() : 0;

        // Shipping costs from purchases
        var avgShipping = purchases.Count > 0 ? purchases.Average(p => p.ShippingCost) : 0;

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        var prevSales = data.Sales?.Where(s => s.Date >= prevStartDate && s.Date <= prevEndDate).ToList() ?? [];
        var prevPurchases = data.Purchases?.Where(p => p.Date >= prevStartDate && p.Date <= prevEndDate).ToList() ?? [];

        var prevTotalTransactionsCount = prevSales.Count + prevPurchases.Count;
        var prevAllTransactionValues = prevSales.Select(s => s.Total).Concat(prevPurchases.Select(p => p.Total)).ToList();
        var prevAvgTransactionValue = prevAllTransactionValues.Count > 0 ? prevAllTransactionValues.Average() : 0;
        var prevAvgShipping = prevPurchases.Count > 0 ? prevPurchases.Average(p => p.ShippingCost) : 0;

        // Check if there's any previous period data
        var hasPrevPeriodData = prevTotalTransactionsCount > 0;

        // Revenue growth (period over period)
        var currentRevenueTotal = sales.Sum(s => s.Total);
        var prevRevenueTotal = prevSales.Sum(s => s.Total);
        var revenueGrowthValue = prevRevenueTotal > 0 ? ((currentRevenueTotal - prevRevenueTotal) / prevRevenueTotal) * 100 : 0;

        var transactionsChange = prevTotalTransactionsCount > 0 ? ((double)(totalTransactionsCount - prevTotalTransactionsCount) / prevTotalTransactionsCount) * 100 : 0;
        var avgTransactionChange = prevAvgTransactionValue > 0 ? ((avgTransactionValue - prevAvgTransactionValue) / prevAvgTransactionValue) * 100 : 0;
        var shippingChange = prevAvgShipping > 0 ? ((avgShipping - prevAvgShipping) / prevAvgShipping) * 100 : 0;

        RevenueGrowth = $"{revenueGrowthValue:F1}%";
        RevenueGrowthChangeValue = hasPrevPeriodData && prevRevenueTotal > 0 ? (double)revenueGrowthValue : null;
        RevenueGrowthChangeText = hasPrevPeriodData && prevRevenueTotal > 0 ? $"{(revenueGrowthValue >= 0 ? "+" : "")}{revenueGrowthValue:F1}%" : null;

        TotalTransactions = totalTransactionsCount.ToString("N0");
        TotalTransactionsChangeValue = hasPrevPeriodData ? transactionsChange : null;
        TotalTransactionsChangeText = hasPrevPeriodData ? $"{(transactionsChange >= 0 ? "+" : "")}{transactionsChange:F1}%" : null;

        AvgTransactionValue = avgTransactionValue.ToString("C0");
        AvgTransactionChangeValue = hasPrevPeriodData && prevAvgTransactionValue > 0 ? (double)avgTransactionChange : null;
        AvgTransactionChangeText = hasPrevPeriodData && prevAvgTransactionValue > 0 ? $"{(avgTransactionChange >= 0 ? "+" : "")}{avgTransactionChange:F1}%" : null;

        AvgShippingCost = avgShipping.ToString("C2");
        AvgShippingChangeValue = hasPrevPeriodData && prevAvgShipping > 0 ? (double)shippingChange : null;
        AvgShippingChangeText = hasPrevPeriodData && prevAvgShipping > 0 ? $"{(shippingChange >= 0 ? "+" : "")}{shippingChange:F1}%" : null;
    }

    private void LoadCustomerStatistics(CompanyData data)
    {
        // Total customers
        var totalCustomersCount = data.Customers?.Count ?? 0;
        TotalCustomers = totalCustomersCount.ToString("N0");

        // New customers (created within date range)
        var newCustomersCount = data.Customers?.Count(c => c.CreatedAt >= StartDate && c.CreatedAt <= EndDate) ?? 0;
        NewCustomers = newCustomersCount.ToString("N0");

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        // Previous new customers
        var prevNewCustomers = data.Customers?.Count(c => c.CreatedAt >= prevStartDate && c.CreatedAt <= prevEndDate) ?? 0;
        var hasPrevNewCustomers = prevNewCustomers > 0;
        var newCustomersChange = prevNewCustomers > 0 ? ((double)(newCustomersCount - prevNewCustomers) / prevNewCustomers) * 100 : 0;

        CustomersChangeValue = null; // Total customers doesn't have a meaningful change calculation
        CustomersChangeText = null;

        NewCustomersChangeValue = hasPrevNewCustomers ? newCustomersChange : null;
        NewCustomersChangeText = hasPrevNewCustomers ? $"{(newCustomersChange >= 0 ? "+" : "")}{newCustomersChange:F1}%" : null;

        // Retention rate and avg customer value are complex calculations
        // For now, calculate avg customer value based on revenue per customer
        var sales = data.Sales?.Where(s => s.Date >= StartDate && s.Date <= EndDate).ToList() ?? [];
        var customerIds = sales.Select(s => s.CustomerId).Distinct().ToList();
        var avgValue = customerIds.Count > 0 ? sales.Sum(s => s.Total) / customerIds.Count : 0;

        RetentionRate = "N/A";
        RetentionChangeValue = null;
        RetentionChangeText = null;

        AvgCustomerValue = avgValue.ToString("C0");
        AvgCustomerValueChangeValue = null;
        AvgCustomerValueChangeText = null;
    }

    private void LoadReturnsStatistics(CompanyData data)
    {
        // Filter returns by date range
        var returns = data.Returns?.Where(r => r.ReturnDate >= StartDate && r.ReturnDate <= EndDate).ToList() ?? [];

        var totalReturnsCount = returns.Count;
        var financialImpact = returns.Sum(r => r.RefundAmount);

        // Calculate return rate (returns / total sales transactions)
        var salesTransactions = data.Sales?.Count(s => s.Date >= StartDate && s.Date <= EndDate) ?? 0;
        var returnRate = salesTransactions > 0 ? ((double)totalReturnsCount / salesTransactions) * 100 : 0;

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        var prevReturns = data.Returns?.Where(r => r.ReturnDate >= prevStartDate && r.ReturnDate <= prevEndDate).ToList() ?? [];
        var prevReturnsCount = prevReturns.Count;
        var prevFinancialImpact = prevReturns.Sum(r => r.RefundAmount);
        var prevSalesTransactions = data.Sales?.Count(s => s.Date >= prevStartDate && s.Date <= prevEndDate) ?? 0;
        var prevReturnRate = prevSalesTransactions > 0 ? ((double)prevReturnsCount / prevSalesTransactions) * 100 : 0;

        // Check if there's any previous period data
        var hasPrevPeriodData = prevReturnsCount > 0 || prevSalesTransactions > 0;

        var returnsChange = prevReturnsCount > 0 ? ((double)(totalReturnsCount - prevReturnsCount) / prevReturnsCount) * 100 : 0;
        var returnRateChange = returnRate - prevReturnRate;
        var impactChange = prevFinancialImpact > 0 ? ((financialImpact - prevFinancialImpact) / prevFinancialImpact) * 100 : 0;

        TotalReturns = totalReturnsCount.ToString("N0");
        ReturnsChangeValue = hasPrevPeriodData && prevReturnsCount > 0 ? returnsChange : null;
        ReturnsChangeText = hasPrevPeriodData && prevReturnsCount > 0 ? $"{(returnsChange >= 0 ? "+" : "")}{returnsChange:F1}%" : null;

        ReturnRate = $"{returnRate:F1}%";
        ReturnRateChangeValue = hasPrevPeriodData && prevSalesTransactions > 0 ? returnRateChange : null;
        ReturnRateChangeText = hasPrevPeriodData && prevSalesTransactions > 0 ? $"{(returnRateChange >= 0 ? "+" : "")}{returnRateChange:F1}%" : null;

        ReturnsFinancialImpact = financialImpact.ToString("C0");
        ReturnsImpactChangeValue = hasPrevPeriodData && prevFinancialImpact > 0 ? (double)impactChange : null;
        ReturnsImpactChangeText = hasPrevPeriodData && prevFinancialImpact > 0 ? $"{(impactChange >= 0 ? "+" : "")}{impactChange:F1}%" : null;

        // Resolution time is not tracked in the data model
        AvgResolutionTime = "N/A";
        ResolutionTimeChangeValue = null;
        ResolutionTimeChangeText = null;
    }

    private void LoadLossesStatistics(CompanyData data)
    {
        // Filter losses by date range
        var losses = data.LostDamaged?.Where(l => l.DateDiscovered >= StartDate && l.DateDiscovered <= EndDate).ToList() ?? [];

        var totalLossesCount = losses.Count;
        var financialImpact = losses.Sum(l => l.ValueLost);

        // Calculate loss rate (losses / total transactions)
        var totalTransactions = (data.Sales?.Count(s => s.Date >= StartDate && s.Date <= EndDate) ?? 0) +
                               (data.Purchases?.Count(p => p.Date >= StartDate && p.Date <= EndDate) ?? 0);
        var lossRate = totalTransactions > 0 ? ((double)totalLossesCount / totalTransactions) * 100 : 0;

        // Count losses with insurance claims filed
        var insuranceClaimsCount = losses.Count(l => l.InsuranceClaim);

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        var prevLosses = data.LostDamaged?.Where(l => l.DateDiscovered >= prevStartDate && l.DateDiscovered <= prevEndDate).ToList() ?? [];
        var prevLossesCount = prevLosses.Count;
        var prevFinancialImpact = prevLosses.Sum(l => l.ValueLost);
        var prevTotalTransactions = (data.Sales?.Count(s => s.Date >= prevStartDate && s.Date <= prevEndDate) ?? 0) +
                                    (data.Purchases?.Count(p => p.Date >= prevStartDate && p.Date <= prevEndDate) ?? 0);
        var prevLossRate = prevTotalTransactions > 0 ? ((double)prevLossesCount / prevTotalTransactions) * 100 : 0;
        var prevInsuranceClaimsCount = prevLosses.Count(l => l.InsuranceClaim);

        // Check if there's any previous period data
        var hasPrevPeriodData = prevLossesCount > 0 || prevTotalTransactions > 0;

        var lossesChange = prevLossesCount > 0 ? ((double)(totalLossesCount - prevLossesCount) / prevLossesCount) * 100 : 0;
        var lossRateChange = lossRate - prevLossRate;
        var impactChange = prevFinancialImpact > 0 ? ((financialImpact - prevFinancialImpact) / prevFinancialImpact) * 100 : 0;
        var insuranceChange = prevInsuranceClaimsCount > 0 ? ((double)(insuranceClaimsCount - prevInsuranceClaimsCount) / prevInsuranceClaimsCount) * 100 : 0;

        TotalLosses = totalLossesCount.ToString("N0");
        LossesChangeValue = hasPrevPeriodData && prevLossesCount > 0 ? lossesChange : null;
        LossesChangeText = hasPrevPeriodData && prevLossesCount > 0 ? $"{(lossesChange >= 0 ? "+" : "")}{lossesChange:F1}%" : null;

        LossRate = $"{lossRate:F1}%";
        LossRateChangeValue = hasPrevPeriodData && prevTotalTransactions > 0 ? lossRateChange : null;
        LossRateChangeText = hasPrevPeriodData && prevTotalTransactions > 0 ? $"{(lossRateChange >= 0 ? "+" : "")}{lossRateChange:F1}%" : null;

        LossesFinancialImpact = financialImpact.ToString("C0");
        LossesImpactChangeValue = hasPrevPeriodData && prevFinancialImpact > 0 ? (double)impactChange : null;
        LossesImpactChangeText = hasPrevPeriodData && prevFinancialImpact > 0 ? $"{(impactChange >= 0 ? "+" : "")}{impactChange:F1}%" : null;

        InsuranceClaims = insuranceClaimsCount.ToString("N0");
        InsuranceClaimsChangeValue = hasPrevPeriodData && prevInsuranceClaimsCount > 0 ? insuranceChange : null;
        InsuranceClaimsChangeText = hasPrevPeriodData && prevInsuranceClaimsCount > 0 ? $"{(insuranceChange >= 0 ? "+" : "")}{insuranceChange:F1}%" : null;
    }

    #endregion

    #region Customer Activity Info Modal

    [ObservableProperty]
    private bool _isCustomerActivityInfoOpen;

    [RelayCommand]
    private void ShowCustomerActivityInfo()
    {
        IsCustomerActivityInfoOpen = true;
    }

    [RelayCommand]
    private void CloseCustomerActivityInfo()
    {
        IsCustomerActivityInfoOpen = false;
    }

    #endregion
}
