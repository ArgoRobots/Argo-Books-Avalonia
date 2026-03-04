using System.Collections.ObjectModel;
using ArgoBooks.Controls;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Measure;
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

    /// <summary>
    /// Gets the shared chart settings service.
    /// </summary>
    private ChartSettingsService ChartSettingsShared => ChartSettingsService.Instance;

    // Flag to prevent duplicate data loading when changing settings locally
    private bool _isLocalSettingChange;

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
    /// Gets whether the Taxes tab is selected.
    /// </summary>
    public bool IsTaxesTabSelected => SelectedTabIndex == 5;

    /// <summary>
    /// Gets whether the Returns tab is selected.
    /// </summary>
    public bool IsReturnsTabSelected => SelectedTabIndex == 6;

    /// <summary>
    /// Gets whether the Losses tab is selected.
    /// </summary>
    public bool IsLossesTabSelected => SelectedTabIndex == 7;

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsDashboardTabSelected));
        OnPropertyChanged(nameof(IsGeographicTabSelected));
        OnPropertyChanged(nameof(IsOperationalTabSelected));
        OnPropertyChanged(nameof(IsPerformanceTabSelected));
        OnPropertyChanged(nameof(IsCustomersTabSelected));
        OnPropertyChanged(nameof(IsReturnsTabSelected));
        OnPropertyChanged(nameof(IsLossesTabSelected));
        OnPropertyChanged(nameof(IsTaxesTabSelected));
    }

    #endregion

    #region Date Range

    /// <summary>
    /// Available date range options.
    /// </summary>
    public ObservableCollection<string> DateRangeOptions { get; } = new(DatePresetNames.StandardDateRangeOptions);

    /// <summary>
    /// Gets or sets the selected date range (delegates to shared service).
    /// </summary>
    public string SelectedDateRange
    {
        get => ChartSettingsShared.SelectedDateRange;
        set
        {
            if (ChartSettingsShared.SelectedDateRange != value)
            {
                var oldValue = ChartSettingsShared.SelectedDateRange;
                _isLocalSettingChange = true;
                try
                {
                    ChartSettingsShared.SelectedDateRange = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCustomDateRange));
                    OnPropertyChanged(nameof(ComparisonPeriodLabel));
                    OnPropertyChanged(nameof(DateRangeDisplayText));

                    if (value == "Custom Range")
                    {
                        OpenCustomDateRangeModal();
                    }
                    else if (oldValue != value)
                    {
                        // Explicitly notify even if value unchanged (service may have set it first)
                        ChartSettingsShared.HasAppliedCustomRange = false;
                        OnPropertyChanged(nameof(HasAppliedCustomRange));
                        OnPropertyChanged(nameof(AppliedDateRangeText));
                        LoadAllCharts();
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
        get => ChartSettingsShared.StartDate;
        set
        {
            if (ChartSettingsShared.StartDate != value)
            {
                ChartSettingsShared.StartDate = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the end date (delegates to shared service).
    /// </summary>
    public DateTime EndDate
    {
        get => ChartSettingsShared.EndDate;
        set
        {
            if (ChartSettingsShared.EndDate != value)
            {
                ChartSettingsShared.EndDate = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether a custom date range has been applied (delegates to shared service).
    /// </summary>
    public bool HasAppliedCustomRange
    {
        get => ChartSettingsShared.HasAppliedCustomRange;
        set
        {
            if (ChartSettingsShared.HasAppliedCustomRange != value)
            {
                ChartSettingsShared.HasAppliedCustomRange = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AppliedDateRangeText));
            }
        }
    }

    /// <summary>
    /// Gets the formatted text showing the applied custom date range.
    /// </summary>
    public string AppliedDateRangeText => ChartSettingsShared.AppliedDateRangeText;

    /// <summary>
    /// Gets the formatted text showing the currently selected date range span.
    /// </summary>
    public string DateRangeDisplayText => ChartSettingsShared.DateRangeDisplayText;

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
                LoadAllCharts();
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
                LoadAllCharts();
            }
        }
    }

    /// <summary>
    /// Gets whether the custom date range option is selected.
    /// </summary>
    public bool IsCustomDateRange => SelectedDateRange == "Custom Range";

    /// <summary>
    /// Gets the label for comparison period based on selected date range (delegates to shared service).
    /// </summary>
    public string ComparisonPeriodLabel => ChartSettingsShared.ComparisonPeriodLabel;

    /// <summary>
    /// Opens the custom date range modal.
    /// </summary>
    private void OpenCustomDateRangeModal()
    {
        // Store the previous selection before opening the modal
        // so we can restore it if the user cancels
        if (SelectedDateRange != "Custom Range")
        {
            _previousDateRange = SelectedDateRange;
        }

        var earliestDate = _companyManager?.CompanyData?.GetEarliestDate() ?? StartDate;
        var modalStartDate = HasAppliedCustomRange ? StartDate : earliestDate;

        App.CustomDateRangeModal?.Open(modalStartDate, EndDate,
            onApply: (start, end) =>
            {
                StartDate = start;
                EndDate = end;
                HasAppliedCustomRange = true;
                OnPropertyChanged(nameof(AppliedDateRangeText));
                OnPropertyChanged(nameof(DateRangeDisplayText));
                LoadAllCharts();
            },
            onCancel: () =>
            {
                // If no custom range was previously applied, revert to the previous selection
                if (!HasAppliedCustomRange)
                {
                    ChartSettingsShared.SelectedDateRange = _previousDateRange;
                    OnPropertyChanged(nameof(SelectedDateRange));
                    OnPropertyChanged(nameof(DateRangeDisplayText));
                }
            });
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
    private string _totalRevenue = "$0.00";

    [ObservableProperty]
    private double? _revenueChangeValue;

    [ObservableProperty]
    private string? _revenueChangeText;

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

    #region Taxes Tab Statistics

    [ObservableProperty]
    private string _totalTaxCollected = "$0.00";

    [ObservableProperty]
    private double? _taxCollectedChangeValue;

    [ObservableProperty]
    private string? _taxCollectedChangeText;

    [ObservableProperty]
    private string _totalTaxPaid = "$0.00";

    [ObservableProperty]
    private double? _taxPaidChangeValue;

    [ObservableProperty]
    private string? _taxPaidChangeText;

    [ObservableProperty]
    private string _netTaxLiability = "$0.00";

    [ObservableProperty]
    private double? _taxLiabilityChangeValue;

    [ObservableProperty]
    private string? _taxLiabilityChangeText;

    [ObservableProperty]
    private string _effectiveTaxRate = "0.0%";

    [ObservableProperty]
    private double? _effectiveTaxRateChangeValue;

    [ObservableProperty]
    private string? _effectiveTaxRateChangeText;

    #endregion

    #region Chart Type Toggle

    /// <summary>
    /// Available chart type options for the selector.
    /// </summary>
    public string[] ChartTypeOptions { get; } = ["Line", "Column", "Step Line", "Area", "Scatter"];

    /// <summary>
    /// Gets or sets the selected chart type (delegates to shared service).
    /// </summary>
    public string SelectedChartType
    {
        get => ChartSettingsShared.SelectedChartType;
        set
        {
            if (ChartSettingsShared.SelectedChartType != value)
            {
                _isLocalSettingChange = true;
                try
                {
                    ChartSettingsShared.SelectedChartType = value;
                    OnPropertyChanged();
                    LoadAllCharts(styleChangeOnly: true);
                }
                finally
                {
                    _isLocalSettingChange = false;
                }
            }
        }
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
        // No need to reload - we have two separate GeoMaps that show/hide based on mode
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
    private ObservableCollection<PieLegendItem> _expensesDistributionLegend = [];

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
    private ObservableCollection<PieLegendItem> _revenueDistributionLegend = [];

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
    private ObservableCollection<ISeries> _revenueVsExpensesSeries = [];

    [ObservableProperty]
    private Axis[] _revenueVsExpensesXAxes = [];

    [ObservableProperty]
    private Axis[] _revenueVsExpensesYAxes = [];

    [ObservableProperty]
    private bool _hasRevenueVsExpensesData;

    #endregion

    #region Geographic Charts

    [ObservableProperty]
    private ObservableCollection<ISeries> _countriesOfOriginSeries = [];

    [ObservableProperty]
    private ObservableCollection<PieLegendItem> _countriesOfOriginLegend = [];

    [ObservableProperty]
    private bool _hasCountriesOfOriginData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _companiesOfOriginSeries = [];

    [ObservableProperty]
    private ObservableCollection<PieLegendItem> _companiesOfOriginLegend = [];

    [ObservableProperty]
    private bool _hasCompaniesOfOriginData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _countriesOfDestinationSeries = [];

    [ObservableProperty]
    private ObservableCollection<PieLegendItem> _countriesOfDestinationLegend = [];

    [ObservableProperty]
    private bool _hasCountriesOfDestinationData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _companiesOfDestinationSeries = [];

    [ObservableProperty]
    private ObservableCollection<PieLegendItem> _companiesOfDestinationLegend = [];

    [ObservableProperty]
    private bool _hasCompaniesOfDestinationData;

    [ObservableProperty]
    private ObservableCollection<IGeoSeries> _originGeoMapSeries = [];

    [ObservableProperty]
    private bool _hasOriginMapData;

    [ObservableProperty]
    private ObservableCollection<IGeoSeries> _destinationGeoMapSeries = [];

    [ObservableProperty]
    private bool _hasDestinationMapData;

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
    private ObservableCollection<PieLegendItem> _accountantsTransactionsLegend = [];

    [ObservableProperty]
    private bool _hasAccountantsTransactionsData;

    #endregion

    #region Performance Charts

    [ObservableProperty]
    private ObservableCollection<ISeries> _customerGrowthSeries = [];

    [ObservableProperty]
    private Axis[] _customerGrowthXAxes = [];

    [ObservableProperty]
    private Axis[] _customerGrowthYAxes = [];

    [ObservableProperty]
    private bool _hasCustomerGrowthData;

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
    private ObservableCollection<PieLegendItem> _returnReasonsLegend = [];

    [ObservableProperty]
    private bool _hasReturnReasonsData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _returnsByCategorySeries = [];

    [ObservableProperty]
    private ObservableCollection<PieLegendItem> _returnsByCategoryLegend = [];

    [ObservableProperty]
    private bool _hasReturnsByCategoryData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _returnFinancialImpactSeries = [];

    [ObservableProperty]
    private Axis[] _returnFinancialImpactXAxes = [];

    [ObservableProperty]
    private Axis[] _returnFinancialImpactYAxes = [];

    [ObservableProperty]
    private bool _hasReturnFinancialImpactData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _returnsByProductSeries = [];

    [ObservableProperty]
    private ObservableCollection<PieLegendItem> _returnsByProductLegend = [];

    [ObservableProperty]
    private bool _hasReturnsByProductData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _expenseVsRevenueReturnsSeries = [];

    [ObservableProperty]
    private Axis[] _expenseVsRevenueReturnsXAxes = [];

    [ObservableProperty]
    private Axis[] _expenseVsRevenueReturnsYAxes = [];

    [ObservableProperty]
    private bool _hasExpenseVsRevenueReturnsData;

    #endregion

    #region Customer Charts

    [ObservableProperty]
    private ObservableCollection<ISeries> _customerPaymentStatusSeries = [];

    [ObservableProperty]
    private ObservableCollection<PieLegendItem> _customerPaymentStatusLegend = [];

    [ObservableProperty]
    private bool _hasCustomerPaymentStatusData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _activeInactiveCustomersSeries = [];

    [ObservableProperty]
    private ObservableCollection<PieLegendItem> _activeInactiveCustomersLegend = [];

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
    private ObservableCollection<PieLegendItem> _lossReasonsLegend = [];

    [ObservableProperty]
    private bool _hasLossReasonsData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _lossesByProductSeries = [];

    [ObservableProperty]
    private ObservableCollection<PieLegendItem> _lossesByProductLegend = [];

    [ObservableProperty]
    private bool _hasLossesByProductData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _lossesByCategorySeries = [];

    [ObservableProperty]
    private ObservableCollection<PieLegendItem> _lossesByCategoryLegend = [];

    [ObservableProperty]
    private bool _hasLossesByCategoryData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _expenseVsRevenueLossesSeries = [];

    [ObservableProperty]
    private Axis[] _expenseVsRevenueLossesXAxes = [];

    [ObservableProperty]
    private Axis[] _expenseVsRevenueLossesYAxes = [];

    [ObservableProperty]
    private bool _hasExpenseVsRevenueLossesData;

    #endregion

    #region Taxes Charts

    [ObservableProperty]
    private ObservableCollection<ISeries> _taxCollectedVsPaidSeries = [];

    [ObservableProperty]
    private Axis[] _taxCollectedVsPaidXAxes = [];

    [ObservableProperty]
    private Axis[] _taxCollectedVsPaidYAxes = [];

    [ObservableProperty]
    private bool _hasTaxCollectedVsPaidData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _taxLiabilityTrendSeries = [];

    [ObservableProperty]
    private Axis[] _taxLiabilityTrendXAxes = [];

    [ObservableProperty]
    private Axis[] _taxLiabilityTrendYAxes = [];

    [ObservableProperty]
    private bool _hasTaxLiabilityTrendData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _taxByCategorySeries = [];

    [ObservableProperty]
    private ObservableCollection<PieLegendItem> _taxByCategoryLegend = [];

    [ObservableProperty]
    private bool _hasTaxByCategoryData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _taxRateDistributionSeries = [];

    [ObservableProperty]
    private Axis[] _taxRateDistributionXAxes = [];

    [ObservableProperty]
    private Axis[] _taxRateDistributionYAxes = [];

    [ObservableProperty]
    private bool _hasTaxRateDistributionData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _taxByProductSeries = [];

    [ObservableProperty]
    private ObservableCollection<PieLegendItem> _taxByProductLegend = [];

    [ObservableProperty]
    private bool _hasTaxByProductData;

    [ObservableProperty]
    private ObservableCollection<ISeries> _expenseVsRevenueTaxSeries = [];

    [ObservableProperty]
    private Axis[] _expenseVsRevenueTaxXAxes = [];

    [ObservableProperty]
    private Axis[] _expenseVsRevenueTaxYAxes = [];

    [ObservableProperty]
    private bool _hasExpenseVsRevenueTaxData;

    #endregion

    #region Empty State Date Range Detection

    /// <summary>
    /// True when revenue data exists in all time but the current date range filter is excluding it.
    /// </summary>
    [ObservableProperty]
    private bool _showRevenueDateRangeMessage;

    /// <summary>
    /// True when expense data exists in all time but the current date range filter is excluding it.
    /// </summary>
    [ObservableProperty]
    private bool _showExpenseDateRangeMessage;

    /// <summary>
    /// True when financial data (revenue or expenses) exists in all time but the current date range filter is excluding it.
    /// </summary>
    [ObservableProperty]
    private bool _showFinancialDateRangeMessage;

    /// <summary>
    /// True when return data exists in all time but the current date range filter is excluding it.
    /// </summary>
    [ObservableProperty]
    private bool _showReturnDateRangeMessage;

    /// <summary>
    /// True when loss data exists in all time but the current date range filter is excluding it.
    /// </summary>
    [ObservableProperty]
    private bool _showLossDateRangeMessage;

    /// <summary>
    /// True when rental data exists in all time but the current date range filter is excluding it.
    /// </summary>
    [ObservableProperty]
    private bool _showRentalDateRangeMessage;

    /// <summary>
    /// True when tax data exists in all time but the current date range filter is excluding it.
    /// </summary>
    [ObservableProperty]
    private bool _showTaxDateRangeMessage;

    #endregion

    #region Chart Titles

    // Dashboard Tab Chart Titles
    private string _profitOverTimeTitleText = ChartDataType.TotalProfits.GetDisplayName();
    public LabelVisual ProfitOverTimeTitle => ChartLoaderService.CreateChartTitle(_profitOverTimeTitleText);
    public LabelVisual RevenueVsExpensesTitle => ChartLoaderService.CreateChartTitle(ChartDataType.RevenueVsExpenses.GetDisplayName());
    public LabelVisual RevenueTrendsTitle => ChartLoaderService.CreateChartTitle(ChartDataType.TotalRevenue.GetDisplayName());
    public LabelVisual RevenueDistributionTitle => ChartLoaderService.CreateChartTitle(ChartDataType.RevenueDistribution.GetDisplayName());
    public LabelVisual ExpenseTrendsTitle => ChartLoaderService.CreateChartTitle(ChartDataType.TotalExpenses.GetDisplayName());
    public LabelVisual ExpenseDistributionTitle => ChartLoaderService.CreateChartTitle(ChartDataType.ExpensesDistribution.GetDisplayName());

    // Geographic Tab Chart Titles
    public LabelVisual CountriesOfOriginTitle => ChartLoaderService.CreateChartTitle(ChartDataType.CountriesOfOrigin.GetDisplayName());
    public LabelVisual CompaniesOfOriginTitle => ChartLoaderService.CreateChartTitle(ChartDataType.CompaniesOfOrigin.GetDisplayName());
    public LabelVisual CountriesOfDestinationTitle => ChartLoaderService.CreateChartTitle(ChartDataType.CountriesOfDestination.GetDisplayName());
    public LabelVisual CompaniesOfDestinationTitle => ChartLoaderService.CreateChartTitle(ChartDataType.CompaniesOfDestination.GetDisplayName());
    public LabelVisual WorldMapOverviewTitle => ChartLoaderService.CreateChartTitle(ChartDataType.WorldMap.GetDisplayName());

    // Operational Tab Chart Titles
    public LabelVisual TransactionsByAccountantTitle => ChartLoaderService.CreateChartTitle(ChartDataType.AccountantsTransactions.GetDisplayName());
    public LabelVisual WorkloadDistributionTitle => ChartLoaderService.CreateChartTitle(ChartDataType.TotalTransactions.GetDisplayName());

    // Performance Tab Chart Titles
    public LabelVisual AverageTransactionValueTitle => ChartLoaderService.CreateChartTitle(ChartDataType.AverageTransactionValue.GetDisplayName());
    public LabelVisual TotalTransactionsTitle => ChartLoaderService.CreateChartTitle(ChartDataType.TotalTransactions.GetDisplayName());
    public LabelVisual AverageShippingCostsTitle => ChartLoaderService.CreateChartTitle(ChartDataType.AverageShippingCosts.GetDisplayName());

    // Customers Tab Chart Titles
    public LabelVisual TopCustomersByRevenueTitle => ChartLoaderService.CreateChartTitle(ChartDataType.TopCustomersByRevenue.GetDisplayName());
    public LabelVisual CustomerPaymentStatusTitle => ChartLoaderService.CreateChartTitle(ChartDataType.CustomerPaymentStatus.GetDisplayName());
    public LabelVisual CustomerGrowthTitle => ChartLoaderService.CreateChartTitle(ChartDataType.CustomerGrowth.GetDisplayName());
    public LabelVisual CustomerLifetimeValueTitle => ChartLoaderService.CreateChartTitle(ChartDataType.CustomerLifetimeValue.GetDisplayName());
    public LabelVisual ActiveVsInactiveCustomersTitle => ChartLoaderService.CreateChartTitle(ChartDataType.ActiveVsInactiveCustomers.GetDisplayName());
    public LabelVisual RentalsPerCustomerTitle => ChartLoaderService.CreateChartTitle(ChartDataType.RentalsPerCustomer.GetDisplayName());

    // Returns Tab Chart Titles
    public LabelVisual ReturnsOverTimeTitle => ChartLoaderService.CreateChartTitle(ChartDataType.ReturnsOverTime.GetDisplayName());
    public LabelVisual ReturnReasonsTitle => ChartLoaderService.CreateChartTitle(ChartDataType.ReturnReasons.GetDisplayName());
    public LabelVisual FinancialImpactOfReturnsTitle => ChartLoaderService.CreateChartTitle(ChartDataType.ReturnFinancialImpact.GetDisplayName());
    public LabelVisual ReturnsByCategoryTitle => ChartLoaderService.CreateChartTitle(ChartDataType.ReturnsByCategory.GetDisplayName());
    public LabelVisual ReturnsByProductTitle => ChartLoaderService.CreateChartTitle(ChartDataType.ReturnsByProduct.GetDisplayName());
    public LabelVisual ExpenseVsRevenueReturnsTitle => ChartLoaderService.CreateChartTitle(ChartDataType.ExpenseVsRevenueReturns.GetDisplayName());

    // Losses Tab Chart Titles
    public LabelVisual LossesOverTimeTitle => ChartLoaderService.CreateChartTitle(ChartDataType.LossesOverTime.GetDisplayName());
    public LabelVisual LossReasonsTitle => ChartLoaderService.CreateChartTitle(ChartDataType.LossReasons.GetDisplayName());
    public LabelVisual FinancialImpactOfLossesTitle => ChartLoaderService.CreateChartTitle(ChartDataType.LossFinancialImpact.GetDisplayName());
    public LabelVisual LossesByCategoryTitle => ChartLoaderService.CreateChartTitle(ChartDataType.LossesByCategory.GetDisplayName());
    public LabelVisual LossesByProductTitle => ChartLoaderService.CreateChartTitle(ChartDataType.LossesByProduct.GetDisplayName());
    public LabelVisual ExpenseVsRevenueLossesTitle => ChartLoaderService.CreateChartTitle(ChartDataType.ExpenseVsRevenueLosses.GetDisplayName());

    // Taxes Tab Chart Titles
    public LabelVisual TaxCollectedVsPaidTitle => ChartLoaderService.CreateChartTitle(ChartDataType.TaxCollectedVsPaid.GetDisplayName());
    public LabelVisual TaxLiabilityTrendTitle => ChartLoaderService.CreateChartTitle(ChartDataType.TaxLiabilityTrend.GetDisplayName());
    public LabelVisual TaxByCategoryTitle => ChartLoaderService.CreateChartTitle(ChartDataType.TaxByCategory.GetDisplayName());
    public LabelVisual TaxRateDistributionTitle => ChartLoaderService.CreateChartTitle(ChartDataType.TaxRateDistribution.GetDisplayName());
    public LabelVisual TaxByProductTitle => ChartLoaderService.CreateChartTitle(ChartDataType.TaxByProduct.GetDisplayName());
    public LabelVisual ExpenseVsRevenueTaxTitle => ChartLoaderService.CreateChartTitle(ChartDataType.ExpenseVsRevenueTax.GetDisplayName());

    /// <summary>
    /// Chart title property names for batch notification.
    /// </summary>
    private static readonly string[] ChartTitlePropertyNames =
    [
        nameof(ProfitOverTimeTitle), nameof(RevenueVsExpensesTitle), nameof(RevenueTrendsTitle),
        nameof(RevenueDistributionTitle), nameof(ExpenseTrendsTitle), nameof(ExpenseDistributionTitle),
        nameof(CountriesOfOriginTitle), nameof(CompaniesOfOriginTitle), nameof(CountriesOfDestinationTitle),
        nameof(CompaniesOfDestinationTitle), nameof(WorldMapOverviewTitle), nameof(TransactionsByAccountantTitle),
        nameof(WorkloadDistributionTitle), nameof(AverageTransactionValueTitle),
        nameof(TotalTransactionsTitle), nameof(AverageShippingCostsTitle),
        nameof(TopCustomersByRevenueTitle), nameof(CustomerPaymentStatusTitle), nameof(CustomerGrowthTitle),
        nameof(CustomerLifetimeValueTitle), nameof(ActiveVsInactiveCustomersTitle), nameof(RentalsPerCustomerTitle),
        nameof(ReturnsOverTimeTitle), nameof(ReturnReasonsTitle), nameof(FinancialImpactOfReturnsTitle),
        nameof(ReturnsByCategoryTitle), nameof(ReturnsByProductTitle), nameof(ExpenseVsRevenueReturnsTitle),
        nameof(LossesOverTimeTitle), nameof(LossReasonsTitle), nameof(FinancialImpactOfLossesTitle),
        nameof(LossesByCategoryTitle), nameof(LossesByProductTitle), nameof(ExpenseVsRevenueLossesTitle),
        nameof(TaxCollectedVsPaidTitle), nameof(TaxLiabilityTrendTitle), nameof(TaxByCategoryTitle),
        nameof(TaxRateDistributionTitle), nameof(TaxByProductTitle), nameof(ExpenseVsRevenueTaxTitle)
    ];

    /// <summary>
    /// Notifies all chart title properties changed (for theme updates).
    /// </summary>
    private void NotifyAllChartTitlesChanged()
    {
        foreach (var propertyName in ChartTitlePropertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public AnalyticsPageViewModel()
    {
        // Initialize the shared chart settings service
        ChartSettingsShared.Initialize();

        // Subscribe to chart settings changes from other pages
        ChartSettingsShared.ChartTypeChanged += OnChartSettingsChartTypeChanged;
        ChartSettingsShared.DateRangeChanged += OnChartSettingsDateRangeChanged;

        // Subscribe to theme changes to reload charts with new colors
        ThemeService.Instance.ThemeChanged += (_, _) =>
        {
            LoadAllCharts();
            NotifyAllChartTitlesChanged();
        };

        // Subscribe to date format changes to refresh charts
        DateFormatService.DateFormatChanged += (_, _) =>
        {
            LoadAllCharts();
        };

        // Subscribe to max pie slices changes to refresh pie charts
        ChartSettingsService.MaxPieSlicesChanged += (_, _) =>
        {
            LoadAllCharts();
        };

        // Subscribe to currency changes to refresh all monetary displays
        CurrencyService.CurrencyChanged += (_, _) =>
        {
            LoadAllCharts();
        };
    }

    private void OnChartSettingsChartTypeChanged(object? sender, string chartType)
    {
        // Only reload if the change came from another page
        if (!_isLocalSettingChange)
        {
            OnPropertyChanged(nameof(SelectedChartType));
            LoadAllCharts();
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
            OnPropertyChanged(nameof(DateRangeDisplayText));
            OnPropertyChanged(nameof(IsCustomDateRange));
            OnPropertyChanged(nameof(ComparisonPeriodLabel));
            LoadAllCharts();
        }
    }

    /// <summary>
    /// Gets the legend text paint based on the current theme.
    /// </summary>
    public SolidColorPaint LegendTextPaint => ChartLoaderService.GetLegendTextPaint();

    /// <summary>
    /// Gets the draw margin for pie charts to center them better when legend is on the right.
    /// Adds margins to shift pie toward center and leave space for the legend.
    /// </summary>
    public Margin PieChartDrawMargin => new(0, 40, 120, 0);

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
            var chartExportData = _chartLoaderService.GetExportDataForChart(SelectedChartId);
            // Use the UI title (SelectedChartId) for Google Sheets, not the internal stored title
            var chartTitle = !string.IsNullOrEmpty(SelectedChartId) ? SelectedChartId : (chartExportData?.ChartTitle ?? "Chart");

            // Use Pie chart type for distribution charts, match chart style for time-based charts
            ArgoBooks.Core.Services.GoogleSheetsService.ChartType chartType;
            if (chartExportData?.ChartType == ChartType.Distribution)
            {
                chartType = GoogleSheetsService.ChartType.Pie;
            }
            else
            {
                chartType = _chartLoaderService.SelectedChartStyle switch
                {
                    ChartStyle.Line => GoogleSheetsService.ChartType.Line,
                    ChartStyle.Area => GoogleSheetsService.ChartType.Area,
                    ChartStyle.StepLine => GoogleSheetsService.ChartType.StepLine,
                    ChartStyle.Scatter => GoogleSheetsService.ChartType.Scatter,
                    _ => GoogleSheetsService.ChartType.Column
                };
            }

            var googleSheetsService = new GoogleSheetsService(App.ErrorLogger, App.TelemetryManager);
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
            App.ErrorLogger?.LogError(ex, ErrorCategory.Api, "Google Sheets export failed - invalid operation");
            GoogleSheetsExportStatusChanged?.Invoke(this, new GoogleSheetsExportEventArgs
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, ErrorCategory.Api, "Google Sheets export failed");
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
        var exportData = _chartLoaderService.GetExportDataForChart(SelectedChartId);
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
            ChartStyle = _chartLoaderService.SelectedChartStyle,
            AdditionalSeries = exportData.AdditionalSeries
        });
    }

    /// <inheritdoc />
    protected override void OnResetChartZoom()
    {
        // Only reset the zoom on the chart that was right-clicked
        if (string.IsNullOrEmpty(SelectedChartId))
            return;

        // Map chart titles to their axes and reset the appropriate one.
        // Use Contains/StartsWith for titles that change dynamically (e.g. "Total profits: $X,XXX").
        var id = SelectedChartId;

        if (id is "Profit Over Time" || id.StartsWith("Total profits", StringComparison.OrdinalIgnoreCase))
            ChartLoaderService.ResetZoom(ProfitTrendsXAxes, ProfitTrendsYAxes);
        else if (id is "Expenses vs Revenue")
            ChartLoaderService.ResetZoom(RevenueVsExpensesXAxes, RevenueVsExpensesYAxes);
        else if (id is "Revenue Trends")
            ChartLoaderService.ResetZoom(RevenueTrendsXAxes, RevenueTrendsYAxes);
        else if (id is "Expense Trends")
            ChartLoaderService.ResetZoom(ExpensesTrendsXAxes, ExpensesTrendsYAxes);
        else if (id is "Total Transactions")
            ChartLoaderService.ResetZoom(TotalTransactionsXAxes, TotalTransactionsYAxes);
        else if (id is "Average Transaction Value")
            ChartLoaderService.ResetZoom(AvgTransactionValueXAxes, AvgTransactionValueYAxes);
        else if (id is "Average Shipping Costs")
            ChartLoaderService.ResetZoom(AvgShippingCostsXAxes, AvgShippingCostsYAxes);
        else if (id is "Customer Growth")
            ChartLoaderService.ResetZoom(CustomerGrowthXAxes, CustomerGrowthYAxes);
        else if (id is "Customer Lifetime Value")
            ChartLoaderService.ResetZoom(AvgTransactionValueXAxes, AvgTransactionValueYAxes);
        else if (id is "Rentals per Customer")
            ChartLoaderService.ResetZoom(TotalTransactionsXAxes, TotalTransactionsYAxes);
        else if (id is "Tax Collected vs Paid")
            ChartLoaderService.ResetZoom(TaxCollectedVsPaidXAxes, TaxCollectedVsPaidYAxes);
        else if (id is "Tax Rate Distribution")
            ChartLoaderService.ResetZoom(TaxRateDistributionXAxes, TaxRateDistributionYAxes);
        else if (id is "Net Tax Liability")
            ChartLoaderService.ResetZoom(TaxLiabilityTrendXAxes, TaxLiabilityTrendYAxes);
        else if (id is "Expense vs Revenue Tax")
            ChartLoaderService.ResetZoom(ExpenseVsRevenueTaxXAxes, ExpenseVsRevenueTaxYAxes);
        else if (id is "Returns Over Time")
            ChartLoaderService.ResetZoom(ReturnsOverTimeXAxes, ReturnsOverTimeYAxes);
        else if (id is "Financial Impact of Returns")
            ChartLoaderService.ResetZoom(ReturnFinancialImpactXAxes, ReturnFinancialImpactYAxes);
        else if (id is "Expense vs Revenue Returns")
            ChartLoaderService.ResetZoom(ExpenseVsRevenueReturnsXAxes, ExpenseVsRevenueReturnsYAxes);
        else if (id is "Losses Over Time")
            ChartLoaderService.ResetZoom(LossesOverTimeXAxes, LossesOverTimeYAxes);
        else if (id is "Financial Impact of Losses")
            ChartLoaderService.ResetZoom(LossFinancialImpactXAxes, LossFinancialImpactYAxes);
        else if (id is "Expense vs Revenue Losses")
            ChartLoaderService.ResetZoom(ExpenseVsRevenueLossesXAxes, ExpenseVsRevenueLossesYAxes);
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

        // Mark the analytics page visit as complete for the tutorial checklist
        TutorialService.Instance.CompleteChecklistItem(TutorialService.ChecklistItems.VisitAnalytics);
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
        ChartSettingsShared.ChartTypeChanged -= OnChartSettingsChartTypeChanged;
        ChartSettingsShared.DateRangeChanged -= OnChartSettingsDateRangeChanged;
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
    public void LoadAllCharts(bool styleChangeOnly = false)
    {
        var data = _companyManager?.CompanyData;
        if (data == null) return;

        // Update theme colors and chart style
        _chartLoaderService.UpdateThemeColors(ThemeService.Instance.IsDarkTheme);
        _chartLoaderService.SelectedChartStyle = SelectedChartType switch
        {
            "Line" => ChartStyle.Line,
            "Column" => ChartStyle.Column,
            "Step Line" => ChartStyle.StepLine,
            "Area" => ChartStyle.Area,
            "Scatter" => ChartStyle.Scatter,
            _ => ChartStyle.Line
        };

        if (!styleChangeOnly)
        {
            // Determine if a date range filter is active and data exists beyond it
            var isFiltered = SelectedDateRange != "All Time";
            ShowRevenueDateRangeMessage = isFiltered && data.Revenues.Count > 0;
            ShowExpenseDateRangeMessage = isFiltered && data.Expenses.Count > 0;
            ShowFinancialDateRangeMessage = isFiltered && (data.Revenues.Count > 0 || data.Expenses.Count > 0);
            ShowReturnDateRangeMessage = isFiltered && data.Returns.Count > 0;
            ShowLossDateRangeMessage = isFiltered && data.LostDamaged.Count > 0;
            ShowRentalDateRangeMessage = isFiltered && data.Rentals.Count > 0;
            ShowTaxDateRangeMessage = isFiltered && (data.Revenues.Any(r => r.TaxAmount > 0 || r.TaxAmountUSD > 0) ||
                                                      data.Expenses.Any(e => e.TaxAmount > 0 || e.TaxAmountUSD > 0));

            // Load statistics for stat cards
            LoadAllStatistics(data);
        }

        // Dashboard charts (cartesian)
        LoadExpensesTrendsChart(data);
        LoadRevenueTrendsChart(data);
        LoadProfitTrendsChart(data);
        LoadRevenueVsExpensesChart(data);

        // Operational charts (cartesian)
        LoadAvgTransactionValueChart(data);
        LoadTotalTransactionsChart(data);
        LoadAvgShippingCostsChart(data);

        // Performance charts (cartesian)
        LoadCustomerGrowthChart(data);

        // Returns charts (cartesian)
        LoadReturnsOverTimeChart(data);
        LoadReturnFinancialImpactChart(data);
        LoadExpenseVsRevenueReturnsChart(data);

        // Losses charts (cartesian)
        LoadLossesOverTimeChart(data);
        LoadLossFinancialImpactChart(data);
        LoadExpenseVsRevenueLossesChart(data);

        // Taxes charts (cartesian)
        LoadTaxCollectedVsPaidChart(data);
        LoadTaxLiabilityTrendChart(data);
        LoadTaxRateDistributionChart(data);
        LoadExpenseVsRevenueTaxChart(data);

        // Pie charts and geo map are style-independent — only reload on data/filter changes
        if (!styleChangeOnly)
        {
            // Dashboard pie charts
            LoadExpensesDistributionChart(data);
            LoadRevenueDistributionChart(data);

            // Geographic charts
            LoadCountriesOfOriginChart(data);
            LoadCompaniesOfOriginChart(data);
            LoadCountriesOfDestinationChart(data);
            LoadCompaniesOfDestinationChart(data);
            LoadGeoMapChart();

            // Operational pie chart
            LoadAccountantsTransactionsChart(data);

            // Customer pie charts
            LoadCustomerPaymentStatusChart(data);
            LoadActiveInactiveCustomersChart(data);

            // Returns pie charts
            LoadReturnReasonsChart(data);
            LoadReturnsByCategoryChart(data);
            LoadReturnsByProductChart(data);

            // Losses pie charts
            LoadLossReasonsChart(data);
            LoadLossesByProductChart(data);
            LoadLossesByCategoryChart(data);

            // Taxes pie charts
            LoadTaxByCategoryChart(data);
            LoadTaxByProductChart(data);
        }
    }

    private void LoadExpensesTrendsChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadExpensesOverviewChart(data, StartDate, EndDate);
        ExpensesTrendsSeries = series;
        ExpensesTrendsXAxes = _chartLoaderService.CreateDateXAxes(dates);
        ExpensesTrendsYAxes = _chartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        HasExpensesTrendsData = series.Count > 0;
    }

    private void LoadExpensesDistributionChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadExpenseDistributionChart(data, StartDate, EndDate);
        ExpensesDistributionSeries = series;
        ExpensesDistributionLegend = legend;
        HasExpensesDistributionData = series.Count > 0;
    }

    private void LoadRevenueTrendsChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadRevenueOverviewChart(data, StartDate, EndDate);
        RevenueTrendsSeries = series;
        RevenueTrendsXAxes = _chartLoaderService.CreateDateXAxes(dates);
        RevenueTrendsYAxes = _chartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        HasRevenueTrendsData = series.Count > 0;
    }

    private void LoadRevenueDistributionChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadRevenueDistributionChart(data, StartDate, EndDate);
        RevenueDistributionSeries = series;
        RevenueDistributionLegend = legend;
        HasRevenueDistributionData = series.Count > 0;
    }

    private void LoadProfitTrendsChart(CompanyData data)
    {
        var (series, labels, dates, totalProfit) = _chartLoaderService.LoadProfitsOverviewChart(data, StartDate, EndDate);
        ProfitTrendsSeries = series;
        ProfitTrendsXAxes = _chartLoaderService.CreateDateXAxes(dates);
        ProfitTrendsYAxes = _chartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        HasProfitTrendsData = series.Count > 0;

        // Update chart title to include the total profit amount (matches dashboard format)
        _profitOverTimeTitleText = $"Total profits: {CurrencyService.FormatFromUSD(totalProfit, DateTime.Now)}";
        OnPropertyChanged(nameof(ProfitOverTimeTitle));
    }

    private void LoadRevenueVsExpensesChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadRevenueVsExpensesChart(data, StartDate, EndDate);
        RevenueVsExpensesSeries = series;
        RevenueVsExpensesXAxes = _chartLoaderService.CreateDateXAxes(dates);
        RevenueVsExpensesYAxes = _chartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        HasRevenueVsExpensesData = series.Count > 0;
    }

    private void LoadCountriesOfOriginChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadCountriesOfOriginChart(data, StartDate, EndDate);
        CountriesOfOriginSeries = series;
        CountriesOfOriginLegend = legend;
        HasCountriesOfOriginData = series.Count > 0;
    }

    private void LoadCompaniesOfOriginChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadCompaniesOfOriginChart(data, StartDate, EndDate);
        CompaniesOfOriginSeries = series;
        CompaniesOfOriginLegend = legend;
        HasCompaniesOfOriginData = series.Count > 0;
    }

    private void LoadCountriesOfDestinationChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadCountriesOfDestinationChart(data, StartDate, EndDate);
        CountriesOfDestinationSeries = series;
        CountriesOfDestinationLegend = legend;
        HasCountriesOfDestinationData = series.Count > 0;
    }

    private void LoadCompaniesOfDestinationChart(CompanyData data)
    {
        // For companies of destination, use sales by customer company (where products are shipped to)
        var (series, legend) = _chartLoaderService.LoadCompaniesOfDestinationChart(data, StartDate, EndDate);
        CompaniesOfDestinationSeries = series;
        CompaniesOfDestinationLegend = legend;
        HasCompaniesOfDestinationData = series.Count > 0;
    }

    private void LoadGeoMapChart()
    {
        var data = _companyManager?.CompanyData;
        if (data == null) return;

        // Load Origin map (supplier countries from purchases)
        var originData = _chartLoaderService.LoadWorldMapDataBySupplier(data, StartDate, EndDate);
        HasOriginMapData = originData.Count > 0;

        var originSeries = new ObservableCollection<IGeoSeries>();
        if (HasOriginMapData)
        {
            var lands = originData
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => new HeatLand { Name = kvp.Key, Value = kvp.Value })
                .ToArray();
            originSeries.Add(new HeatLandSeries { Lands = lands });
        }
        OriginGeoMapSeries = originSeries;

        // Load Destination map (customer countries from sales)
        var destinationData = _chartLoaderService.LoadWorldMapData(data, StartDate, EndDate);
        HasDestinationMapData = destinationData.Count > 0;

        var destinationSeries = new ObservableCollection<IGeoSeries>();
        if (HasDestinationMapData)
        {
            var lands = destinationData
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => new HeatLand { Name = kvp.Key, Value = kvp.Value })
                .ToArray();
            destinationSeries.Add(new HeatLandSeries { Lands = lands });
        }
        DestinationGeoMapSeries = destinationSeries;
    }

    private void LoadAvgTransactionValueChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadAverageTransactionValueChart(data, StartDate, EndDate);
        AvgTransactionValueSeries = series;
        AvgTransactionValueXAxes = _chartLoaderService.CreateDateXAxes(dates);
        AvgTransactionValueYAxes = _chartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        HasAvgTransactionValueData = series.Count > 0;
    }

    private void LoadTotalTransactionsChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadTotalTransactionsChart(data, StartDate, EndDate);
        TotalTransactionsSeries = series;
        TotalTransactionsXAxes = _chartLoaderService.CreateDateXAxes(dates);
        TotalTransactionsYAxes = _chartLoaderService.CreateNumberYAxes();
        HasTotalTransactionsData = series.Count > 0;
    }

    private void LoadAvgShippingCostsChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadAverageShippingCostsChart(data, StartDate, EndDate);
        AvgShippingCostsSeries = series;
        AvgShippingCostsXAxes = _chartLoaderService.CreateDateXAxes(dates);
        AvgShippingCostsYAxes = _chartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        HasAvgShippingCostsData = series.Count > 0;
    }

    private void LoadAccountantsTransactionsChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadAccountantsTransactionsChart(data, StartDate, EndDate);
        AccountantsTransactionsSeries = series;
        AccountantsTransactionsLegend = legend;
        HasAccountantsTransactionsData = series.Count > 0;
    }

    private void LoadCustomerGrowthChart(CompanyData data)
    {
        var (series, labels) = _chartLoaderService.LoadCustomerGrowthChart(data, StartDate, EndDate, SelectedDateRange);
        CustomerGrowthSeries = series;
        CustomerGrowthXAxes = _chartLoaderService.CreateXAxes(labels);
        CustomerGrowthYAxes = _chartLoaderService.CreateNumberYAxes();
        HasCustomerGrowthData = series.Count > 0;
    }

    private void LoadReturnsOverTimeChart(CompanyData data)
    {
        var (series, labels, dates) = _chartLoaderService.LoadReturnsOverTimeChart(data, StartDate, EndDate);
        ReturnsOverTimeSeries = series;
        ReturnsOverTimeXAxes = _chartLoaderService.CreateDateXAxes(dates);
        ReturnsOverTimeYAxes = _chartLoaderService.CreateNumberYAxes();
        HasReturnsOverTimeData = series.Count > 0;
    }

    private void LoadReturnReasonsChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadReturnReasonsChart(data, StartDate, EndDate);
        ReturnReasonsSeries = series;
        ReturnReasonsLegend = legend;
        HasReturnReasonsData = series.Count > 0;
    }

    private void LoadReturnsByCategoryChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadReturnsByCategoryChart(data, StartDate, EndDate);
        ReturnsByCategorySeries = series;
        ReturnsByCategoryLegend = legend;
        HasReturnsByCategoryData = series.Count > 0;
    }

    private void LoadReturnFinancialImpactChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadReturnFinancialImpactChart(data, StartDate, EndDate);
        ReturnFinancialImpactSeries = series;
        ReturnFinancialImpactXAxes = _chartLoaderService.CreateDateXAxes(dates);
        ReturnFinancialImpactYAxes = _chartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        HasReturnFinancialImpactData = series.Count > 0;
    }

    private void LoadLossesOverTimeChart(CompanyData data)
    {
        var (series, labels, dates) = _chartLoaderService.LoadLossesOverTimeChart(data, StartDate, EndDate);
        LossesOverTimeSeries = series;
        LossesOverTimeXAxes = _chartLoaderService.CreateDateXAxes(dates);
        LossesOverTimeYAxes = _chartLoaderService.CreateNumberYAxes();
        HasLossesOverTimeData = series.Count > 0;
    }

    private void LoadLossFinancialImpactChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadLossFinancialImpactChart(data, StartDate, EndDate);
        LossFinancialImpactSeries = series;
        LossFinancialImpactXAxes = _chartLoaderService.CreateDateXAxes(dates);
        LossFinancialImpactYAxes = _chartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        HasLossFinancialImpactData = series.Count > 0;
    }

    private void LoadCustomerPaymentStatusChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadCustomerPaymentStatusChart(data, StartDate, EndDate);
        CustomerPaymentStatusSeries = series;
        CustomerPaymentStatusLegend = legend;
        HasCustomerPaymentStatusData = series.Count > 0;
    }

    private void LoadActiveInactiveCustomersChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadActiveInactiveCustomersChart(data, StartDate, EndDate);
        ActiveInactiveCustomersSeries = series;
        ActiveInactiveCustomersLegend = legend;
        HasActiveInactiveCustomersData = series.Count > 0;
    }

    private void LoadLossReasonsChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadLossReasonsChart(data, StartDate, EndDate);
        LossReasonsSeries = series;
        LossReasonsLegend = legend;
        HasLossReasonsData = series.Count > 0;
    }

    private void LoadLossesByProductChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadLossesByProductChart(data, StartDate, EndDate);
        LossesByProductSeries = series;
        LossesByProductLegend = legend;
        HasLossesByProductData = series.Count > 0;
    }

    private void LoadReturnsByProductChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadReturnsByProductChart(data, StartDate, EndDate);
        ReturnsByProductSeries = series;
        ReturnsByProductLegend = legend;
        HasReturnsByProductData = series.Count > 0;
    }

    private void LoadExpenseVsRevenueReturnsChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadExpenseVsRevenueReturnsChart(data, StartDate, EndDate);
        ExpenseVsRevenueReturnsSeries = series;
        ExpenseVsRevenueReturnsXAxes = _chartLoaderService.CreateDateXAxes(dates);
        ExpenseVsRevenueReturnsYAxes = _chartLoaderService.CreateNumberYAxes();
        HasExpenseVsRevenueReturnsData = series.Count > 0;
    }

    private void LoadLossesByCategoryChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadLossesByCategoryChart(data, StartDate, EndDate);
        LossesByCategorySeries = series;
        LossesByCategoryLegend = legend;
        HasLossesByCategoryData = series.Count > 0;
    }

    private void LoadExpenseVsRevenueLossesChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadExpenseVsRevenueLossesChart(data, StartDate, EndDate);
        ExpenseVsRevenueLossesSeries = series;
        ExpenseVsRevenueLossesXAxes = _chartLoaderService.CreateDateXAxes(dates);
        ExpenseVsRevenueLossesYAxes = _chartLoaderService.CreateNumberYAxes();
        HasExpenseVsRevenueLossesData = series.Count > 0;
    }

    private void LoadTaxCollectedVsPaidChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadTaxCollectedVsPaidChart(data, StartDate, EndDate);
        TaxCollectedVsPaidSeries = series;
        TaxCollectedVsPaidXAxes = _chartLoaderService.CreateDateXAxes(dates);
        TaxCollectedVsPaidYAxes = _chartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        HasTaxCollectedVsPaidData = series.Count > 0;
    }

    private void LoadTaxLiabilityTrendChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadTaxLiabilityTrendChart(data, StartDate, EndDate);
        TaxLiabilityTrendSeries = series;
        TaxLiabilityTrendXAxes = _chartLoaderService.CreateDateXAxes(dates);
        TaxLiabilityTrendYAxes = _chartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        HasTaxLiabilityTrendData = series.Count > 0;
    }

    private void LoadTaxByCategoryChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadTaxByCategoryChart(data, StartDate, EndDate);
        TaxByCategorySeries = series;
        TaxByCategoryLegend = legend;
        HasTaxByCategoryData = series.Count > 0;
    }

    private void LoadTaxRateDistributionChart(CompanyData data)
    {
        var (series, xAxes, yAxes) = _chartLoaderService.LoadTaxRateDistributionChart(data, StartDate, EndDate);
        TaxRateDistributionSeries = series;
        TaxRateDistributionXAxes = xAxes;
        TaxRateDistributionYAxes = yAxes;
        HasTaxRateDistributionData = series.Count > 0;
    }

    private void LoadTaxByProductChart(CompanyData data)
    {
        var (series, legend) = _chartLoaderService.LoadTaxByProductChart(data, StartDate, EndDate);
        TaxByProductSeries = series;
        TaxByProductLegend = legend;
        HasTaxByProductData = series.Count > 0;
    }

    private void LoadExpenseVsRevenueTaxChart(CompanyData data)
    {
        var (series, dates) = _chartLoaderService.LoadExpenseVsRevenueTaxChart(data, StartDate, EndDate);
        ExpenseVsRevenueTaxSeries = series;
        ExpenseVsRevenueTaxXAxes = _chartLoaderService.CreateDateXAxes(dates);
        ExpenseVsRevenueTaxYAxes = _chartLoaderService.CreateCurrencyYAxes(CurrencyService.CurrentSymbol);
        HasExpenseVsRevenueTaxData = series.Count > 0;
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
        LoadTaxesStatistics(data);
    }

    private void LoadDashboardStatistics(CompanyData data)
    {
        // Filter transactions by date range
        var purchases = data.Expenses.Where(p => p.Date >= StartDate && p.Date <= EndDate).ToList();
        var sales = data.Revenues.Where(s => s.Date >= StartDate && s.Date <= EndDate).ToList();

        // Calculate totals (pre-tax, USD-normalized to match dashboard)
        var totalPurchasesUSD = purchases.Sum(p => p.EffectiveSubtotalUSD);
        var totalRevenueUSD = sales.Sum(s => s.EffectiveSubtotalUSD);
        var netProfitUSD = totalRevenueUSD - totalPurchasesUSD;
        var margin = totalRevenueUSD > 0 ? (netProfitUSD / totalRevenueUSD) * 100 : 0;

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        var prevPurchasesUSD = data.Expenses.Where(p => p.Date >= prevStartDate && p.Date <= prevEndDate).Sum(p => p.EffectiveSubtotalUSD);
        var prevSalesUSD = data.Revenues.Where(s => s.Date >= prevStartDate && s.Date <= prevEndDate).Sum(s => s.EffectiveSubtotalUSD);
        var prevNetProfit = prevSalesUSD - prevPurchasesUSD;
        var prevMargin = prevSalesUSD > 0 ? (prevNetProfit / prevSalesUSD) * 100 : 0;

        // Check if there's any previous period data to compare against
        var hasPrevPeriodData = prevPurchasesUSD > 0 || prevSalesUSD > 0;

        // Calculate change percentages (only meaningful if there's previous data)
        var purchasesChange = prevPurchasesUSD > 0 ? ((totalPurchasesUSD - prevPurchasesUSD) / prevPurchasesUSD) * 100 : 0;
        var revenueChange = prevSalesUSD > 0 ? ((totalRevenueUSD - prevSalesUSD) / prevSalesUSD) * 100 : 0;
        var profitChange = prevNetProfit != 0 ? ((netProfitUSD - prevNetProfit) / Math.Abs(prevNetProfit)) * 100 : 0;
        var marginChange = margin - prevMargin;

        // Update properties (convert from USD to display currency)
        TotalPurchases = CurrencyService.FormatWholeNumber(CurrencyService.GetDisplayAmount(totalPurchasesUSD, DateTime.Now));
        PurchasesChangeValue = hasPrevPeriodData && prevPurchasesUSD > 0 ? (double)purchasesChange : null;
        PurchasesChangeText = hasPrevPeriodData && prevPurchasesUSD > 0 ? $"{Math.Abs(purchasesChange):F1}%" : null;

        TotalRevenue = CurrencyService.FormatWholeNumber(CurrencyService.GetDisplayAmount(totalRevenueUSD, DateTime.Now));
        RevenueChangeValue = hasPrevPeriodData && prevSalesUSD > 0 ? (double)revenueChange : null;
        RevenueChangeText = hasPrevPeriodData && prevSalesUSD > 0 ? $"{Math.Abs(revenueChange):F1}%" : null;

        NetProfit = CurrencyService.FormatWholeNumber(CurrencyService.GetDisplayAmount(netProfitUSD, DateTime.Now));
        ProfitChangeValue = hasPrevPeriodData && prevNetProfit != 0 ? (double)profitChange : null;
        ProfitChangeText = hasPrevPeriodData && prevNetProfit != 0 ? $"{Math.Abs(profitChange):F1}%" : null;

        ProfitMargin = $"{margin:F1}%";
        ProfitMarginChangeValue = hasPrevPeriodData && prevSalesUSD > 0 ? (double)marginChange : null;
        ProfitMarginChangeText = hasPrevPeriodData && prevSalesUSD > 0 ? $"{Math.Abs(marginChange):F1}%" : null;
    }

    private void LoadOperationalStatistics(CompanyData data)
    {
        // Count all accountants (no Status property on Accountant)
        var accountantsCount = data.Accountants.Count;
        ActiveAccountants = accountantsCount.ToString();

        // Transactions processed in the date range
        var purchases = data.Expenses.Where(p => p.Date >= StartDate && p.Date <= EndDate).ToList();
        var sales = data.Revenues.Where(s => s.Date >= StartDate && s.Date <= EndDate).ToList();
        var totalTransactions = purchases.Count + sales.Count;

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        var prevPurchasesCount = data.Expenses.Count(p => p.Date >= prevStartDate && p.Date <= prevEndDate);
        var prevSalesCount = data.Revenues.Count(s => s.Date >= prevStartDate && s.Date <= prevEndDate);
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
        var sales = data.Revenues.Where(s => s.Date >= StartDate && s.Date <= EndDate).ToList();
        var purchases = data.Expenses.Where(p => p.Date >= StartDate && p.Date <= EndDate).ToList();

        var totalTransactionsCount = sales.Count + purchases.Count;
        var allTransactionValues = sales.Select(s => s.EffectiveSubtotalUSD).Concat(purchases.Select(p => p.EffectiveSubtotalUSD)).ToList();
        var avgTransactionValue = allTransactionValues.Count > 0 ? allTransactionValues.Average() : 0;

        // Shipping costs from purchases
        var avgShipping = purchases.Count > 0 ? purchases.Average(p => p.ShippingCost) : 0;

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        var prevSales = data.Revenues.Where(s => s.Date >= prevStartDate && s.Date <= prevEndDate).ToList();
        var prevPurchases = data.Expenses.Where(p => p.Date >= prevStartDate && p.Date <= prevEndDate).ToList();

        var prevTotalTransactionsCount = prevSales.Count + prevPurchases.Count;
        var prevAllTransactionValues = prevSales.Select(s => s.EffectiveSubtotalUSD).Concat(prevPurchases.Select(p => p.EffectiveSubtotalUSD)).ToList();
        var prevAvgTransactionValue = prevAllTransactionValues.Count > 0 ? prevAllTransactionValues.Average() : 0;
        var prevAvgShipping = prevPurchases.Count > 0 ? prevPurchases.Average(p => p.ShippingCost) : 0;

        // Check if there's any previous period data
        var hasPrevPeriodData = prevTotalTransactionsCount > 0;

        // Revenue growth (period over period)
        var currentRevenueTotal = sales.Sum(s => s.EffectiveSubtotalUSD);
        var prevRevenueTotal = prevSales.Sum(s => s.EffectiveSubtotalUSD);
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

        AvgTransactionValue = CurrencyService.FormatWholeNumber(CurrencyService.GetDisplayAmount(avgTransactionValue, DateTime.Now));
        AvgTransactionChangeValue = hasPrevPeriodData && prevAvgTransactionValue > 0 ? (double)avgTransactionChange : null;
        AvgTransactionChangeText = hasPrevPeriodData && prevAvgTransactionValue > 0 ? $"{(avgTransactionChange >= 0 ? "+" : "")}{avgTransactionChange:F1}%" : null;

        AvgShippingCost = CurrencyService.Format(avgShipping);
        AvgShippingChangeValue = hasPrevPeriodData && prevAvgShipping > 0 ? (double)shippingChange : null;
        AvgShippingChangeText = hasPrevPeriodData && prevAvgShipping > 0 ? $"{(shippingChange >= 0 ? "+" : "")}{shippingChange:F1}%" : null;
    }

    private void LoadCustomerStatistics(CompanyData data)
    {
        // Total customers
        var totalCustomersCount = data.Customers.Count;
        TotalCustomers = totalCustomersCount.ToString("N0");

        // New customers (created within date range)
        var newCustomersCount = data.Customers.Count(c => c.CreatedAt >= StartDate && c.CreatedAt <= EndDate);
        NewCustomers = newCustomersCount.ToString("N0");

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        // Previous new customers
        var prevNewCustomers = data.Customers.Count(c => c.CreatedAt >= prevStartDate && c.CreatedAt <= prevEndDate);
        var hasPrevNewCustomers = prevNewCustomers > 0;
        var newCustomersChange = prevNewCustomers > 0 ? ((double)(newCustomersCount - prevNewCustomers) / prevNewCustomers) * 100 : 0;

        CustomersChangeValue = null; // Total customers doesn't have a meaningful change calculation
        CustomersChangeText = null;

        NewCustomersChangeValue = hasPrevNewCustomers ? newCustomersChange : null;
        NewCustomersChangeText = hasPrevNewCustomers ? $"{(newCustomersChange >= 0 ? "+" : "")}{newCustomersChange:F1}%" : null;

        // Retention rate and avg customer value are complex calculations
        // For now, calculate avg customer value based on revenue per customer
        var sales = data.Revenues.Where(s => s.Date >= StartDate && s.Date <= EndDate).ToList();
        var customerIds = sales.Select(s => s.CustomerId).Distinct().ToList();
        var avgValueUSD = customerIds.Count > 0 ? sales.Sum(s => s.EffectiveSubtotalUSD) / customerIds.Count : 0;

        RetentionRate = "N/A";
        RetentionChangeValue = null;
        RetentionChangeText = null;

        AvgCustomerValue = CurrencyService.FormatWholeNumber(CurrencyService.GetDisplayAmount(avgValueUSD, DateTime.Now));
        AvgCustomerValueChangeValue = null;
        AvgCustomerValueChangeText = null;
    }

    private void LoadReturnsStatistics(CompanyData data)
    {
        // Filter returns by date range
        var returns = data.Returns.Where(r => r.ReturnDate >= StartDate && r.ReturnDate <= EndDate).ToList();

        var totalReturnsCount = returns.Count;
        var financialImpact = returns.Sum(r => r.RefundAmount);

        // Calculate return rate (returns / total sales transactions)
        var salesTransactions = data.Revenues.Count(s => s.Date >= StartDate && s.Date <= EndDate);
        var returnRate = salesTransactions > 0 ? ((double)totalReturnsCount / salesTransactions) * 100 : 0;

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        var prevReturns = data.Returns.Where(r => r.ReturnDate >= prevStartDate && r.ReturnDate <= prevEndDate).ToList();
        var prevReturnsCount = prevReturns.Count;
        var prevFinancialImpact = prevReturns.Sum(r => r.RefundAmount);
        var prevSalesTransactions = data.Revenues.Count(s => s.Date >= prevStartDate && s.Date <= prevEndDate);
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

        ReturnsFinancialImpact = CurrencyService.FormatWholeNumber(financialImpact);
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
        var losses = data.LostDamaged.Where(l => l.DateDiscovered >= StartDate && l.DateDiscovered <= EndDate).ToList();

        var totalLossesCount = losses.Count;
        var financialImpact = losses.Sum(l => l.ValueLost);

        // Calculate loss rate (losses / total transactions)
        var totalTransactions = data.Revenues.Count(s => s.Date >= StartDate && s.Date <= EndDate) +
                               data.Expenses.Count(p => p.Date >= StartDate && p.Date <= EndDate);
        var lossRate = totalTransactions > 0 ? ((double)totalLossesCount / totalTransactions) * 100 : 0;

        // Count losses with insurance claims filed
        var insuranceClaimsCount = losses.Count(l => l.InsuranceClaim);

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        var prevLosses = data.LostDamaged.Where(l => l.DateDiscovered >= prevStartDate && l.DateDiscovered <= prevEndDate).ToList();
        var prevLossesCount = prevLosses.Count;
        var prevFinancialImpact = prevLosses.Sum(l => l.ValueLost);
        var prevTotalTransactions = data.Revenues.Count(s => s.Date >= prevStartDate && s.Date <= prevEndDate) +
                                    data.Expenses.Count(p => p.Date >= prevStartDate && p.Date <= prevEndDate);
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

        LossesFinancialImpact = CurrencyService.FormatWholeNumber(financialImpact);
        LossesImpactChangeValue = hasPrevPeriodData && prevFinancialImpact > 0 ? (double)impactChange : null;
        LossesImpactChangeText = hasPrevPeriodData && prevFinancialImpact > 0 ? $"{(impactChange >= 0 ? "+" : "")}{impactChange:F1}%" : null;

        InsuranceClaims = insuranceClaimsCount.ToString("N0");
        InsuranceClaimsChangeValue = hasPrevPeriodData && prevInsuranceClaimsCount > 0 ? insuranceChange : null;
        InsuranceClaimsChangeText = hasPrevPeriodData && prevInsuranceClaimsCount > 0 ? $"{(insuranceChange >= 0 ? "+" : "")}{insuranceChange:F1}%" : null;
    }

    private void LoadTaxesStatistics(CompanyData data)
    {
        // Calculate tax collected from revenues and tax paid on expenses
        var revenues = data.Revenues.Where(r => r.Date >= StartDate && r.Date <= EndDate).ToList();
        var expenses = data.Expenses.Where(e => e.Date >= StartDate && e.Date <= EndDate).ToList();

        var taxCollectedUSD = revenues.Sum(r => r.TaxAmountUSD > 0 ? r.TaxAmountUSD : r.TaxAmount);
        var taxPaidUSD = expenses.Sum(e => e.TaxAmountUSD > 0 ? e.TaxAmountUSD : e.TaxAmount);
        var netLiability = taxCollectedUSD - taxPaidUSD;

        // Calculate effective tax rate (weighted average across all transactions)
        var totalPreTax = revenues.Sum(r => r.EffectiveSubtotalUSD) + expenses.Sum(e => e.EffectiveSubtotalUSD);
        var totalTax = taxCollectedUSD + taxPaidUSD;
        var effectiveRate = totalPreTax > 0 ? (totalTax / totalPreTax) * 100 : 0;

        // Calculate previous period for comparison
        var periodLength = EndDate - StartDate;
        var prevStartDate = StartDate - periodLength;
        var prevEndDate = StartDate.AddDays(-1);

        var prevRevenues = data.Revenues.Where(r => r.Date >= prevStartDate && r.Date <= prevEndDate).ToList();
        var prevExpenses = data.Expenses.Where(e => e.Date >= prevStartDate && e.Date <= prevEndDate).ToList();

        var prevTaxCollected = prevRevenues.Sum(r => r.TaxAmountUSD > 0 ? r.TaxAmountUSD : r.TaxAmount);
        var prevTaxPaid = prevExpenses.Sum(e => e.TaxAmountUSD > 0 ? e.TaxAmountUSD : e.TaxAmount);
        var prevNetLiability = prevTaxCollected - prevTaxPaid;
        var prevTotalPreTax = prevRevenues.Sum(r => r.EffectiveSubtotalUSD) + prevExpenses.Sum(e => e.EffectiveSubtotalUSD);
        var prevTotalTax = prevTaxCollected + prevTaxPaid;
        var prevEffectiveRate = prevTotalPreTax > 0 ? (prevTotalTax / prevTotalPreTax) * 100 : 0;

        var hasPrevPeriodData = prevTaxCollected > 0 || prevTaxPaid > 0;

        var collectedChange = prevTaxCollected > 0 ? ((taxCollectedUSD - prevTaxCollected) / prevTaxCollected) * 100 : 0;
        var paidChange = prevTaxPaid > 0 ? ((taxPaidUSD - prevTaxPaid) / prevTaxPaid) * 100 : 0;
        var liabilityChange = prevNetLiability != 0 ? ((netLiability - prevNetLiability) / Math.Abs(prevNetLiability)) * 100 : 0;
        var rateChange = effectiveRate - prevEffectiveRate;

        TotalTaxCollected = CurrencyService.FormatWholeNumber(CurrencyService.GetDisplayAmount(taxCollectedUSD, DateTime.Now));
        TaxCollectedChangeValue = hasPrevPeriodData && prevTaxCollected > 0 ? (double)collectedChange : null;
        TaxCollectedChangeText = hasPrevPeriodData && prevTaxCollected > 0 ? $"{Math.Abs(collectedChange):F1}%" : null;

        TotalTaxPaid = CurrencyService.FormatWholeNumber(CurrencyService.GetDisplayAmount(taxPaidUSD, DateTime.Now));
        TaxPaidChangeValue = hasPrevPeriodData && prevTaxPaid > 0 ? (double)paidChange : null;
        TaxPaidChangeText = hasPrevPeriodData && prevTaxPaid > 0 ? $"{Math.Abs(paidChange):F1}%" : null;

        NetTaxLiability = CurrencyService.FormatWholeNumber(CurrencyService.GetDisplayAmount(netLiability, DateTime.Now));
        TaxLiabilityChangeValue = hasPrevPeriodData && prevNetLiability != 0 ? (double)liabilityChange : null;
        TaxLiabilityChangeText = hasPrevPeriodData && prevNetLiability != 0 ? $"{Math.Abs(liabilityChange):F1}%" : null;

        EffectiveTaxRate = $"{effectiveRate:F1}%";
        EffectiveTaxRateChangeValue = hasPrevPeriodData && prevTotalPreTax > 0 ? (double)rateChange : null;
        EffectiveTaxRateChangeText = hasPrevPeriodData && prevTotalPreTax > 0 ? $"{Math.Abs(rateChange):F1}%" : null;
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
