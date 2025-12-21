using System.Collections.ObjectModel;
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
            StatusColor = new SolidColorBrush(Color.Parse("#22C55E"))
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
            StatusColor = new SolidColorBrush(Color.Parse("#EF4444"))
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
            StatusColor = new SolidColorBrush(Color.Parse("#F59E0B"))
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
            StatusColor = new SolidColorBrush(Color.Parse("#22C55E"))
        });
    }

    /// <summary>
    /// Refreshes the insights data.
    /// </summary>
    [RelayCommand]
    private void RefreshInsights()
    {
        // In a real implementation, this would call an AI service to regenerate insights
        LastUpdated = DateTime.Now.ToString("h:mm tt");
    }
}
