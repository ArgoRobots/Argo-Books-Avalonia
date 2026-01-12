using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.Insights;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Represents a single insight item for display in the UI.
/// </summary>
public partial class InsightItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string? _recommendation;

    [ObservableProperty]
    private IBrush _statusColor = Brushes.Gray;

    [ObservableProperty]
    private bool _isLastItem;

    public bool HasRecommendation => !string.IsNullOrEmpty(Recommendation);
}

/// <summary>
/// ViewModel for the Insights page displaying AI-powered business insights.
/// Uses the InsightsService for real statistical analysis of business data.
/// </summary>
public partial class InsightsPageViewModel : ViewModelBase
{
    #region Services

    private readonly IInsightsService _insightsService;

    #endregion

    #region Statistics

    [ObservableProperty]
    private string _totalInsights = "0";

    [ObservableProperty]
    private string _trendsDetected = "0";

    [ObservableProperty]
    private string _anomaliesDetected = "0";

    [ObservableProperty]
    private string _opportunities = "0";

    [ObservableProperty]
    private string _lastUpdated = "Never";

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _hasInsufficientData;

    [ObservableProperty]
    private string _insufficientDataMessage = string.Empty;

    #endregion

    #region Date Range

    /// <summary>
    /// Available date range options for insights (future-focused).
    /// </summary>
    public ObservableCollection<string> DateRangeOptions { get; } = new(DatePresetNames.FutureDateRangeOptions);

    [ObservableProperty]
    private int _selectedDateRangeIndex = 0;

    [ObservableProperty]
    private DateTime _startDate;

    [ObservableProperty]
    private DateTime _endDate;

    /// <summary>
    /// Gets the currently selected date range string.
    /// </summary>
    public string SelectedDateRange => DateRangeOptions.Count > SelectedDateRangeIndex
        ? DateRangeOptions[SelectedDateRangeIndex]
        : "Next Month";

    partial void OnSelectedDateRangeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(SelectedDateRange));
        UpdateDateRangeFromSelection();
        // Only refresh the forecast cards, not the insights
        _ = RefreshForecastAsync();
    }

    /// <summary>
    /// Updates the start and end dates based on the selected future date range option.
    /// </summary>
    private void UpdateDateRangeFromSelection()
    {
        var (start, end) = DatePresetNames.GetDateRange(SelectedDateRange switch
        {
            "Next Month" => DatePresetNames.NextMonth,
            "Next Quarter" => DatePresetNames.NextQuarter,
            "Next Year" => DatePresetNames.NextYear,
            "Next 30 Days" => DatePresetNames.NextMonthToDate,
            "Next 90 Days" => DatePresetNames.NextQuarterToDate,
            "Next 365 Days" => DatePresetNames.NextYearToDate,
            _ => DatePresetNames.NextMonth
        });

        StartDate = start;
        EndDate = end;

        // Update forecast date range label to show exact dates
        ForecastDateRangeLabel = $"{start:MMM d, yyyy} - {end:MMM d, yyyy}";

        // Update forecast card labels based on selected period
        switch (SelectedDateRange)
        {
            case "Next Quarter":
                RevenueLabel = "Next Quarter Revenue";
                ExpensesLabel = "Next Quarter Expenses";
                ProfitLabel = "Projected Quarterly Profit";
                CustomersLabel = "Expected New Customers (Quarter)";
                ComparisonLabel = "vs last quarter";
                break;
            case "Next Year":
                RevenueLabel = "Next Year Revenue";
                ExpensesLabel = "Next Year Expenses";
                ProfitLabel = "Projected Annual Profit";
                CustomersLabel = "Expected New Customers (Year)";
                ComparisonLabel = "vs last year";
                break;
            case "Next 30 Days":
                RevenueLabel = "Next 30 Days Revenue";
                ExpensesLabel = "Next 30 Days Expenses";
                ProfitLabel = "Projected Profit (30 Days)";
                CustomersLabel = "Expected New Customers (30 Days)";
                ComparisonLabel = "vs last 30 days";
                break;
            case "Next 90 Days":
                RevenueLabel = "Next 90 Days Revenue";
                ExpensesLabel = "Next 90 Days Expenses";
                ProfitLabel = "Projected Profit (90 Days)";
                CustomersLabel = "Expected New Customers (90 Days)";
                ComparisonLabel = "vs last 90 days";
                break;
            case "Next 365 Days":
                RevenueLabel = "Next 365 Days Revenue";
                ExpensesLabel = "Next 365 Days Expenses";
                ProfitLabel = "Projected Annual Profit";
                CustomersLabel = "Expected New Customers (Year)";
                ComparisonLabel = "vs last 365 days";
                break;
            default: // Next Month
                RevenueLabel = "Next Month Revenue";
                ExpensesLabel = "Next Month Expenses";
                ProfitLabel = "Projected Profit";
                CustomersLabel = "Expected New Customers";
                ComparisonLabel = "vs last month";
                break;
        }
    }

    #endregion

    #region Forecasted Growth

    [ObservableProperty]
    private string _forecastedRevenue = "$0";

    [ObservableProperty]
    private double _revenueGrowthValue;

    [ObservableProperty]
    private string _revenueGrowth = "0%";

    [ObservableProperty]
    private string _forecastedExpenses = "$0";

    [ObservableProperty]
    private double _expenseGrowthValue;

    [ObservableProperty]
    private string _expenseGrowth = "0%";

    [ObservableProperty]
    private string _forecastedProfit = "$0";

    [ObservableProperty]
    private double _profitGrowthValue;

    [ObservableProperty]
    private string _profitGrowth = "0%";

    [ObservableProperty]
    private string _forecastedCustomers = "0";

    [ObservableProperty]
    private double _customerGrowthValue;

    [ObservableProperty]
    private string _customerGrowth = "0%";

    [ObservableProperty]
    private string _predictionConfidence = "-- --";

    [ObservableProperty]
    private string _dataMonthsNote = "Insufficient data for forecasting";

    // Dynamic labels based on selected forecast period
    [ObservableProperty]
    private string _revenueLabel = "Next Month Revenue";

    [ObservableProperty]
    private string _expensesLabel = "Next Month Expenses";

    [ObservableProperty]
    private string _profitLabel = "Projected Profit";

    [ObservableProperty]
    private string _customersLabel = "Expected New Customers";

    /// <summary>
    /// Dynamic comparison label that shows what period the forecast is compared against.
    /// </summary>
    [ObservableProperty]
    private string _comparisonLabel = "vs last month";

    /// <summary>
    /// Shows the exact date range being forecasted.
    /// </summary>
    [ObservableProperty]
    private string _forecastDateRangeLabel = string.Empty;

    #endregion

    #region Info Modal

    /// <summary>
    /// Opens the prediction methodology info modal.
    /// </summary>
    [RelayCommand]
    private void ShowPredictionInfo()
    {
        App.PredictionInfoModalViewModel?.OpenCommand.Execute(null);
    }

    #endregion

    #region Insights Period

    /// <summary>
    /// Description of the analysis period used for insights (always historical).
    /// </summary>
    [ObservableProperty]
    private string _insightsAnalysisPeriod = "Based on last 3 months";

    #endregion

    #region Insight Collections

    public ObservableCollection<InsightItemViewModel> RevenueTrends { get; } = [];
    public ObservableCollection<InsightItemViewModel> Anomalies { get; } = [];
    public ObservableCollection<InsightItemViewModel> Forecasts { get; } = [];
    public ObservableCollection<InsightItemViewModel> Recommendations { get; } = [];

    #endregion

    /// <summary>
    /// Creates a new InsightsPageViewModel with the InsightsService.
    /// </summary>
    public InsightsPageViewModel()
    {
        // Instantiate the InsightsService directly
        _insightsService = new InsightsService();

        // Initialize date range and load insights
        UpdateDateRangeFromSelection();
        _ = RefreshInsightsAsync();
    }

    /// <summary>
    /// Constructor for dependency injection.
    /// </summary>
    public InsightsPageViewModel(IInsightsService insightsService)
    {
        _insightsService = insightsService;

        // Initialize date range and load insights
        UpdateDateRangeFromSelection();
        _ = RefreshInsightsAsync();
    }

    /// <summary>
    /// Refreshes only the forecast cards based on the selected date range.
    /// </summary>
    private async Task RefreshForecastAsync()
    {
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        try
        {
            // Create date range for forecast using the future date preset
            var dateRange = AnalysisDateRange.Custom(StartDate, EndDate);

            // Generate forecast only
            var forecast = await _insightsService.GenerateForecastAsync(companyData, dateRange);

            // Update forecast display
            UpdateForecastDisplay(forecast);
        }
        catch
        {
            // Silently fail - the insights are still valid
        }
    }

    /// <summary>
    /// Refreshes the insights data using the InsightsService.
    /// Insights use a standard historical period, not affected by date range selection.
    /// </summary>
    [RelayCommand]
    private async Task RefreshInsightsAsync()
    {
        if (IsRefreshing) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null)
        {
            HasInsufficientData = true;
            InsufficientDataMessage = "No company data loaded. Please open or create a company file.";
            return;
        }

        IsRefreshing = true;
        HasInsufficientData = false;

        try
        {
            // Insights always use a standard historical analysis period (last 3 months)
            var insightsStartDate = DateTime.Today.AddMonths(-3);
            var insightsEndDate = DateTime.Today;
            var insightsDateRange = AnalysisDateRange.Custom(insightsStartDate, insightsEndDate);

            // Update the analysis period description
            InsightsAnalysisPeriod = $"Based on {insightsStartDate:MMM d} - {insightsEndDate:MMM d, yyyy}";

            // Generate insights using the service with historical data
            var insights = await _insightsService.GenerateInsightsAsync(companyData, insightsDateRange);

            // Check for sufficient data
            if (!insights.HasSufficientData)
            {
                HasInsufficientData = true;
                InsufficientDataMessage = insights.InsufficientDataMessage ?? "Insufficient data for analysis.";
                ClearInsights();
                return;
            }

            // Update summary statistics
            TotalInsights = insights.Summary.TotalInsights.ToString();
            TrendsDetected = insights.Summary.TrendsDetected.ToString();
            AnomaliesDetected = insights.Summary.AnomaliesDetected.ToString();
            Opportunities = insights.Summary.Opportunities.ToString();

            // Update insight collections (these don't change with date range)
            UpdateInsightCollection(RevenueTrends, insights.RevenueTrends);
            UpdateInsightCollection(Anomalies, insights.Anomalies);
            UpdateInsightCollection(Forecasts, insights.Forecasts);
            UpdateInsightCollection(Recommendations, insights.Recommendations);

            // Now update forecast based on selected date range
            var forecastDateRange = AnalysisDateRange.Custom(StartDate, EndDate);
            var forecast = await _insightsService.GenerateForecastAsync(companyData, forecastDateRange);
            UpdateForecastDisplay(forecast);

            LastUpdated = DateTime.Now.ToString("h:mm tt");
        }
        catch (Exception ex)
        {
            HasInsufficientData = true;
            InsufficientDataMessage = $"Error generating insights: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Updates the forecast display with the generated data.
    /// </summary>
    private void UpdateForecastDisplay(ForecastData forecast)
    {
        ForecastedRevenue = forecast.ForecastedRevenue.ToString("C0");
        RevenueGrowthValue = (double)forecast.RevenueGrowthPercent;
        RevenueGrowth = $"{Math.Abs(forecast.RevenueGrowthPercent):F1}%";

        ForecastedExpenses = forecast.ForecastedExpenses.ToString("C0");
        ExpenseGrowthValue = -(double)forecast.ExpenseGrowthPercent; // Negative because increased expenses is bad
        ExpenseGrowth = $"{Math.Abs(forecast.ExpenseGrowthPercent):F1}%";

        ForecastedProfit = forecast.ForecastedProfit.ToString("C0");
        ProfitGrowthValue = (double)forecast.ProfitGrowthPercent;
        ProfitGrowth = $"{Math.Abs(forecast.ProfitGrowthPercent):F1}%";

        ForecastedCustomers = forecast.ExpectedNewCustomers.ToString();
        CustomerGrowthValue = (double)forecast.CustomerGrowthPercent;
        CustomerGrowth = $"{Math.Abs(forecast.CustomerGrowthPercent):F1}%";

        PredictionConfidence = $"{forecast.ConfidenceScore:F0}% {forecast.ConfidenceLevel}";
        DataMonthsNote = forecast.DataMonthsUsed > 0
            ? $"Based on {forecast.DataMonthsUsed} months of historical data"
            : "Insufficient data for forecasting";
    }

    /// <summary>
    /// Updates an insight collection with new items.
    /// </summary>
    private void UpdateInsightCollection(ObservableCollection<InsightItemViewModel> collection, List<InsightItem> items)
    {
        collection.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            collection.Add(new InsightItemViewModel
            {
                Title = item.Title,
                Description = item.Description,
                Recommendation = item.Recommendation,
                StatusColor = GetStatusColor(item.Severity),
                IsLastItem = i == items.Count - 1
            });
        }
    }

    /// <summary>
    /// Gets the color brush for an insight severity level.
    /// </summary>
    private static IBrush GetStatusColor(InsightSeverity severity)
    {
        return severity switch
        {
            InsightSeverity.Success => new SolidColorBrush(Color.Parse("#22C55E")),  // Green
            InsightSeverity.Info => new SolidColorBrush(Color.Parse("#3B82F6")),     // Blue
            InsightSeverity.Warning => new SolidColorBrush(Color.Parse("#F59E0B")),  // Orange
            InsightSeverity.Critical => new SolidColorBrush(Color.Parse("#EF4444")), // Red
            _ => new SolidColorBrush(Color.Parse("#8B5CF6"))                          // Purple (default)
        };
    }

    /// <summary>
    /// Clears all insight collections when data is insufficient.
    /// </summary>
    private void ClearInsights()
    {
        TotalInsights = "0";
        TrendsDetected = "0";
        AnomaliesDetected = "0";
        Opportunities = "0";

        ForecastedRevenue = "$0";
        RevenueGrowthValue = 0;
        RevenueGrowth = "0%";
        ForecastedExpenses = "$0";
        ExpenseGrowthValue = 0;
        ExpenseGrowth = "0%";
        ForecastedProfit = "$0";
        ProfitGrowthValue = 0;
        ProfitGrowth = "0%";
        ForecastedCustomers = "0";
        CustomerGrowthValue = 0;
        CustomerGrowth = "0%";
        PredictionConfidence = "-- --";
        DataMonthsNote = "Insufficient data for forecasting";

        RevenueTrends.Clear();
        Anomalies.Clear();
        Forecasts.Clear();
        Recommendations.Clear();
    }
}
