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
    /// Available date range options.
    /// </summary>
    public ObservableCollection<string> DateRangeOptions { get; } = new(DatePresetNames.StandardDateRangeOptions);

    [ObservableProperty]
    private string _selectedDateRange = "This Month";

    [ObservableProperty]
    private DateTime _startDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Now;

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
    /// Gets or sets whether a custom date range has been applied.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AppliedDateRangeText))]
    private bool _hasAppliedCustomRange;

    /// <summary>
    /// Gets the formatted text showing the applied custom date range.
    /// </summary>
    public string AppliedDateRangeText => HasAppliedCustomRange
        ? $"{StartDate:MMM d, yyyy} - {EndDate:MMM d, yyyy}"
        : string.Empty;

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

    partial void OnSelectedDateRangeChanged(string value)
    {
        OnPropertyChanged(nameof(IsCustomDateRange));

        if (value == "Custom Range")
        {
            OpenCustomDateRangeModal();
        }
        else
        {
            HasAppliedCustomRange = false;
            UpdateDateRangeFromSelection();
            _ = RefreshInsightsAsync();
        }
    }

    /// <summary>
    /// Opens the custom date range modal.
    /// </summary>
    [RelayCommand]
    private void OpenCustomDateRangeModal()
    {
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
                (ModalStartDate, ModalEndDate) = (ModalEndDate, ModalStartDate);
                OnPropertyChanged(nameof(ModalStartDateOffset));
                OnPropertyChanged(nameof(ModalEndDateOffset));
            }
            else
            {
                return;
            }
        }

        StartDate = ModalStartDate;
        EndDate = ModalEndDate;
        HasAppliedCustomRange = true;
        OnPropertyChanged(nameof(AppliedDateRangeText));
        IsCustomDateRangeModalOpen = false;
        await RefreshInsightsAsync();
    }

    /// <summary>
    /// Cancels the custom date range modal.
    /// </summary>
    [RelayCommand]
    private void CancelCustomDateRange()
    {
        IsCustomDateRangeModalOpen = false;

        if (!HasAppliedCustomRange)
        {
            SelectedDateRange = "This Month";
        }
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

        // Load insights on initialization
        _ = RefreshInsightsAsync();
    }

    /// <summary>
    /// Constructor for dependency injection.
    /// </summary>
    public InsightsPageViewModel(IInsightsService insightsService)
    {
        _insightsService = insightsService;
        _ = RefreshInsightsAsync();
    }

    /// <summary>
    /// Refreshes the insights data using the InsightsService.
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
            // Create date range for analysis
            var dateRange = HasAppliedCustomRange
                ? AnalysisDateRange.Custom(StartDate, EndDate)
                : AnalysisDateRange.FromPreset(SelectedDateRange);

            // Generate insights using the service
            var insights = await _insightsService.GenerateInsightsAsync(companyData, dateRange);

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

            // Update forecast data
            UpdateForecastDisplay(insights.Forecast);

            // Update insight collections
            UpdateInsightCollection(RevenueTrends, insights.RevenueTrends);
            UpdateInsightCollection(Anomalies, insights.Anomalies);
            UpdateInsightCollection(Forecasts, insights.Forecasts);
            UpdateInsightCollection(Recommendations, insights.Recommendations);

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
