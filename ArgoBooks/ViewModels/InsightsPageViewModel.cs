using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Reports;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Represents a single insight item.
/// </summary>
public partial class InsightItem : ObservableObject
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
/// </summary>
public partial class InsightsPageViewModel : ViewModelBase
{
    #region Statistics

    [ObservableProperty]
    private string _totalInsights = "12";

    [ObservableProperty]
    private string _trendsDetected = "4";

    [ObservableProperty]
    private string _anomaliesDetected = "2";

    [ObservableProperty]
    private string _opportunities = "6";

    [ObservableProperty]
    private string _lastUpdated = "Just now";

    [ObservableProperty]
    private bool _isRefreshing;

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
            RefreshForecastData();
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
        RefreshForecastData();
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

    /// <summary>
    /// Refreshes the forecast data based on the selected date range.
    /// </summary>
    private void RefreshForecastData()
    {
        // In a real implementation, this would recalculate forecasts based on the date range
        // For now, this is a placeholder for future functionality
    }

    #endregion

    #region Forecasted Growth

    [ObservableProperty]
    private string _forecastedRevenue = "$48,500";

    [ObservableProperty]
    private double _revenueGrowthValue = 12.3;

    [ObservableProperty]
    private string _revenueGrowth = "12.3%";

    [ObservableProperty]
    private string _forecastedExpenses = "$32,200";

    [ObservableProperty]
    private double _expenseGrowthValue = -5.8;  // Negative because increased expenses is bad

    [ObservableProperty]
    private string _expenseGrowth = "5.8%";

    [ObservableProperty]
    private string _forecastedProfit = "$16,300";

    [ObservableProperty]
    private double _profitGrowthValue = 24.1;

    [ObservableProperty]
    private string _profitGrowth = "24.1%";

    [ObservableProperty]
    private string _forecastedCustomers = "28";

    [ObservableProperty]
    private double _customerGrowthValue = 18.5;

    [ObservableProperty]
    private string _customerGrowth = "18.5%";

    [ObservableProperty]
    private string _predictionConfidence = "87% High";

    #endregion

    #region Insight Collections

    public ObservableCollection<InsightItem> RevenueTrends { get; } = [];
    public ObservableCollection<InsightItem> Anomalies { get; } = [];
    public ObservableCollection<InsightItem> Forecasts { get; } = [];
    public ObservableCollection<InsightItem> Recommendations { get; } = [];

    #endregion

    /// <summary>
    /// Creates a new InsightsPageViewModel with sample data.
    /// </summary>
    public InsightsPageViewModel()
    {
        LoadSampleData();
    }

    /// <summary>
    /// Loads sample data for demonstration purposes.
    /// </summary>
    private void LoadSampleData()
    {
        // Revenue Trends
        RevenueTrends.Add(new InsightItem
        {
            Title = "Revenue Growth Detected",
            Description = "Your revenue has increased by 15% compared to last month. This trend has been consistent for the past 3 months.",
            Recommendation = "Consider increasing inventory for your best-selling products.",
            StatusColor = new SolidColorBrush(Color.Parse("#22C55E"))
        });

        RevenueTrends.Add(new InsightItem
        {
            Title = "Seasonal Pattern Identified",
            Description = "Sales typically increase 25% during November-December based on historical data.",
            Recommendation = "Prepare inventory and staffing for the upcoming holiday season.",
            StatusColor = new SolidColorBrush(Color.Parse("#3B82F6"))
        });

        RevenueTrends.Add(new InsightItem
        {
            Title = "Weekend Sales Performance",
            Description = "Weekend sales are 40% higher than weekday sales on average.",
            StatusColor = new SolidColorBrush(Color.Parse("#22C55E")),
            IsLastItem = true
        });

        // Anomalies
        Anomalies.Add(new InsightItem
        {
            Title = "Unusual Expense Spike",
            Description = "Operating expenses increased 45% this week compared to your typical weekly average.",
            Recommendation = "Review recent expense entries for any errors or unexpected costs.",
            StatusColor = new SolidColorBrush(Color.Parse("#F59E0B"))
        });

        Anomalies.Add(new InsightItem
        {
            Title = "Customer Returns Above Normal",
            Description = "Return rate is 8% higher than usual this month. Most returns are for Product Category: Electronics.",
            Recommendation = "Investigate product quality or description accuracy for electronics items.",
            StatusColor = new SolidColorBrush(Color.Parse("#EF4444")),
            IsLastItem = true
        });

        // Forecasts
        Forecasts.Add(new InsightItem
        {
            Title = "Next Month Revenue Forecast",
            Description = "Based on current trends and historical data, expected revenue for next month is $45,000 - $52,000.",
            StatusColor = new SolidColorBrush(Color.Parse("#8B5CF6"))
        });

        Forecasts.Add(new InsightItem
        {
            Title = "Cash Flow Projection",
            Description = "Projected cash flow for the next 30 days is positive. Expected surplus: $8,500.",
            StatusColor = new SolidColorBrush(Color.Parse("#22C55E"))
        });

        Forecasts.Add(new InsightItem
        {
            Title = "Inventory Depletion Alert",
            Description = "At current sales velocity, 3 products will reach reorder point within 2 weeks.",
            Recommendation = "Review and place orders for low-stock items.",
            StatusColor = new SolidColorBrush(Color.Parse("#F59E0B")),
            IsLastItem = true
        });

        // Recommendations
        Recommendations.Add(new InsightItem
        {
            Title = "Top Performing Product",
            Description = "\"Premium Widget\" has the highest profit margin at 42%. Consider featuring it more prominently.",
            Recommendation = "Add to featured products or promotional campaigns.",
            StatusColor = new SolidColorBrush(Color.Parse("#3B82F6"))
        });

        Recommendations.Add(new InsightItem
        {
            Title = "Customer Retention Opportunity",
            Description = "15 customers haven't made a purchase in over 60 days but were previously active.",
            Recommendation = "Send re-engagement emails or special offers to inactive customers.",
            StatusColor = new SolidColorBrush(Color.Parse("#8B5CF6"))
        });

        Recommendations.Add(new InsightItem
        {
            Title = "Payment Collection",
            Description = "3 invoices totaling $2,340 are overdue by more than 30 days.",
            Recommendation = "Send payment reminders or follow up with these customers.",
            StatusColor = new SolidColorBrush(Color.Parse("#F59E0B"))
        });

        Recommendations.Add(new InsightItem
        {
            Title = "Supplier Optimization",
            Description = "Switching to Supplier B for raw materials could save approximately $200/month based on recent price comparisons.",
            Recommendation = "Review supplier contracts and consider renegotiation.",
            StatusColor = new SolidColorBrush(Color.Parse("#22C55E")),
            IsLastItem = true
        });
    }

    /// <summary>
    /// Refreshes the insights data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshInsightsAsync()
    {
        if (IsRefreshing) return;

        IsRefreshing = true;
        try
        {
            // In a real implementation, this would call an AI service to regenerate insights
            await Task.Delay(1500); // Simulate network delay
            LastUpdated = DateTime.Now.ToString("h:mm tt");
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}
