using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Geo;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Analytics page.
/// Handles tab selection, date range filtering, chart type toggling, and chart data loading.
/// </summary>
public partial class AnalyticsPageViewModel : ViewModelBase
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

    partial void OnSelectedDateRangeChanged(string value)
    {
        OnPropertyChanged(nameof(IsCustomDateRange));
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

    #region Chart Type Toggle

    [ObservableProperty]
    private bool _useLineChart = true;

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

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public AnalyticsPageViewModel()
    {
        // Initialize with default values
        UpdateDateRangeFromSelection();
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

        // Update theme colors
        _chartLoaderService.UpdateThemeColors(ThemeService.Instance.IsDarkTheme);

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

        // Returns charts
        LoadReturnsOverTimeChart(data);
        LoadReturnReasonsChart(data);
        LoadReturnFinancialImpactChart(data);

        // Losses charts
        LoadLossesOverTimeChart(data);
        LoadLossFinancialImpactChart(data);
    }

    private void LoadExpensesTrendsChart(CompanyData data)
    {
        var (series, labels, _) = _chartLoaderService.LoadExpensesOverviewChart(data, StartDate, EndDate);
        ExpensesTrendsSeries = series;
        ExpensesTrendsXAxes = _chartLoaderService.CreateXAxes(labels);
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
        var (series, labels, _) = _chartLoaderService.LoadRevenueOverviewChart(data, StartDate, EndDate);
        RevenueTrendsSeries = series;
        RevenueTrendsXAxes = _chartLoaderService.CreateXAxes(labels);
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
        var (series, labels, _) = _chartLoaderService.LoadProfitsOverviewChart(data, StartDate, EndDate);
        ProfitTrendsSeries = series;
        ProfitTrendsXAxes = _chartLoaderService.CreateXAxes(labels);
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
        var (series, labels, _) = _chartLoaderService.LoadReturnsOverTimeChart(data, StartDate, EndDate);
        ReturnsOverTimeSeries = series;
        ReturnsOverTimeXAxes = _chartLoaderService.CreateXAxes(labels);
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
        var (series, labels, _) = _chartLoaderService.LoadLossesOverTimeChart(data, StartDate, EndDate);
        LossesOverTimeSeries = series;
        LossesOverTimeXAxes = _chartLoaderService.CreateXAxes(labels);
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

    #endregion
}
