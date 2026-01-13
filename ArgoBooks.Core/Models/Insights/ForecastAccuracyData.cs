namespace ArgoBooks.Core.Models.Insights;

/// <summary>
/// Represents a single historical forecast record for accuracy tracking.
/// </summary>
public class ForecastAccuracyRecord
{
    /// <summary>
    /// Unique identifier for this record.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// When this forecast was generated.
    /// </summary>
    public DateTime ForecastDate { get; set; }

    /// <summary>
    /// Start date of the period being forecasted.
    /// </summary>
    public DateTime PeriodStartDate { get; set; }

    /// <summary>
    /// End date of the period being forecasted.
    /// </summary>
    public DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// The forecasted revenue value.
    /// </summary>
    public decimal ForecastedRevenue { get; set; }

    /// <summary>
    /// The actual revenue observed (filled in after the period ends).
    /// </summary>
    public decimal? ActualRevenue { get; set; }

    /// <summary>
    /// The forecasted expenses value.
    /// </summary>
    public decimal ForecastedExpenses { get; set; }

    /// <summary>
    /// The actual expenses observed (filled in after the period ends).
    /// </summary>
    public decimal? ActualExpenses { get; set; }

    /// <summary>
    /// The forecasted profit value.
    /// </summary>
    public decimal ForecastedProfit { get; set; }

    /// <summary>
    /// The actual profit observed (filled in after the period ends).
    /// </summary>
    public decimal? ActualProfit { get; set; }

    /// <summary>
    /// Expected number of new customers.
    /// </summary>
    public int ForecastedNewCustomers { get; set; }

    /// <summary>
    /// Actual number of new customers (filled in after the period ends).
    /// </summary>
    public int? ActualNewCustomers { get; set; }

    /// <summary>
    /// The confidence score at time of forecast.
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// The forecasting method used (e.g., "HoltWinters", "ML.NET SSA", "LinearRegression").
    /// </summary>
    public string ForecastMethod { get; set; } = "Combined";

    /// <summary>
    /// Whether this forecast period has been validated against actuals.
    /// </summary>
    public bool IsValidated { get; set; }

    /// <summary>
    /// Calculate the revenue accuracy percentage (0-100, 100 = perfect).
    /// Returns null if actual data is not yet available.
    /// </summary>
    public double? RevenueAccuracyPercent
    {
        get
        {
            if (!ActualRevenue.HasValue || ActualRevenue.Value == 0)
                return null;

            var error = Math.Abs(ForecastedRevenue - ActualRevenue.Value);
            var accuracy = Math.Max(0, 100 - (double)(error / ActualRevenue.Value * 100));
            return accuracy;
        }
    }

    /// <summary>
    /// Calculate the expenses accuracy percentage (0-100, 100 = perfect).
    /// </summary>
    public double? ExpensesAccuracyPercent
    {
        get
        {
            if (!ActualExpenses.HasValue || ActualExpenses.Value == 0)
                return null;

            var error = Math.Abs(ForecastedExpenses - ActualExpenses.Value);
            var accuracy = Math.Max(0, 100 - (double)(error / ActualExpenses.Value * 100));
            return accuracy;
        }
    }

    /// <summary>
    /// Calculate the Mean Absolute Percentage Error (MAPE) for revenue.
    /// </summary>
    public double? RevenueMAPE
    {
        get
        {
            if (!ActualRevenue.HasValue || ActualRevenue.Value == 0)
                return null;

            return (double)(Math.Abs(ForecastedRevenue - ActualRevenue.Value) / ActualRevenue.Value * 100);
        }
    }
}

/// <summary>
/// Contains aggregated forecast accuracy statistics.
/// </summary>
public class ForecastAccuracyData
{
    /// <summary>
    /// List of historical forecast records.
    /// </summary>
    public List<ForecastAccuracyRecord> HistoricalRecords { get; set; } = [];

    /// <summary>
    /// Average revenue accuracy over validated forecasts (0-100%).
    /// </summary>
    public double AverageRevenueAccuracy { get; set; }

    /// <summary>
    /// Average expenses accuracy over validated forecasts (0-100%).
    /// </summary>
    public double AverageExpensesAccuracy { get; set; }

    /// <summary>
    /// Overall Mean Absolute Percentage Error for revenue forecasts.
    /// Lower is better (0% = perfect).
    /// </summary>
    public double OverallRevenueMAPE { get; set; }

    /// <summary>
    /// Number of forecasts that have been validated against actuals.
    /// </summary>
    public int ValidatedForecastCount { get; set; }

    /// <summary>
    /// Total number of forecasts recorded.
    /// </summary>
    public int TotalForecastCount { get; set; }

    /// <summary>
    /// The trend of accuracy over time (improving, declining, stable).
    /// </summary>
    public AccuracyTrend AccuracyTrend { get; set; } = AccuracyTrend.Stable;

    /// <summary>
    /// A human-readable description of the forecast accuracy performance.
    /// </summary>
    public string AccuracyDescription { get; set; } = string.Empty;

    /// <summary>
    /// Calculate aggregated statistics from historical records.
    /// </summary>
    public void CalculateStatistics()
    {
        var validatedRecords = HistoricalRecords.Where(r => r.IsValidated).ToList();
        ValidatedForecastCount = validatedRecords.Count;
        TotalForecastCount = HistoricalRecords.Count;

        if (ValidatedForecastCount == 0)
        {
            AccuracyDescription = "No validated forecasts yet. Check back after the current forecast period ends.";
            return;
        }

        // Calculate average accuracies
        var revenueAccuracies = validatedRecords
            .Where(r => r.RevenueAccuracyPercent.HasValue)
            .Select(r => r.RevenueAccuracyPercent!.Value)
            .ToList();

        var expenseAccuracies = validatedRecords
            .Where(r => r.ExpensesAccuracyPercent.HasValue)
            .Select(r => r.ExpensesAccuracyPercent!.Value)
            .ToList();

        var revenueMAPEs = validatedRecords
            .Where(r => r.RevenueMAPE.HasValue)
            .Select(r => r.RevenueMAPE!.Value)
            .ToList();

        AverageRevenueAccuracy = revenueAccuracies.Any() ? revenueAccuracies.Average() : 0;
        AverageExpensesAccuracy = expenseAccuracies.Any() ? expenseAccuracies.Average() : 0;
        OverallRevenueMAPE = revenueMAPEs.Any() ? revenueMAPEs.Average() : 0;

        // Determine trend (compare first half to second half)
        if (revenueAccuracies.Count >= 4)
        {
            var half = revenueAccuracies.Count / 2;
            var firstHalfAvg = revenueAccuracies.Take(half).Average();
            var secondHalfAvg = revenueAccuracies.Skip(half).Average();

            if (secondHalfAvg > firstHalfAvg + 5)
                AccuracyTrend = AccuracyTrend.Improving;
            else if (secondHalfAvg < firstHalfAvg - 5)
                AccuracyTrend = AccuracyTrend.Declining;
            else
                AccuracyTrend = AccuracyTrend.Stable;
        }

        // Generate description
        var overall = (AverageRevenueAccuracy + AverageExpensesAccuracy) / 2;
        AccuracyDescription = overall switch
        {
            >= 90 => $"Excellent accuracy! Forecasts are within ±{100 - overall:F0}% of actual values on average.",
            >= 80 => $"Good accuracy. Forecasts average ±{100 - overall:F0}% deviation from actual values.",
            >= 70 => $"Moderate accuracy. Forecasts average ±{100 - overall:F0}% deviation. Consider reviewing data patterns.",
            _ => $"Low accuracy (±{100 - overall:F0}% average error). More historical data may improve predictions."
        };
    }
}

/// <summary>
/// Indicates the trend direction of forecast accuracy over time.
/// </summary>
public enum AccuracyTrend
{
    /// <summary>Accuracy is improving over time.</summary>
    Improving,

    /// <summary>Accuracy is stable.</summary>
    Stable,

    /// <summary>Accuracy is declining over time.</summary>
    Declining
}

/// <summary>
/// Time series data point for ML forecasting.
/// </summary>
public class TimeSeriesDataPoint
{
    /// <summary>
    /// The date/time of this data point.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The value (e.g., revenue, expenses).
    /// </summary>
    public float Value { get; set; }
}

/// <summary>
/// Seasonal pattern information detected by Holt-Winters.
/// </summary>
public class SeasonalPattern
{
    /// <summary>
    /// The length of the seasonal cycle (e.g., 12 for monthly yearly pattern).
    /// </summary>
    public int SeasonLength { get; set; } = 12;

    /// <summary>
    /// Seasonal factors for each period in the cycle.
    /// </summary>
    public List<double> SeasonalFactors { get; set; } = [];

    /// <summary>
    /// Strength of the seasonal pattern (0-1, higher = stronger pattern).
    /// </summary>
    public double SeasonalStrength { get; set; }

    /// <summary>
    /// The detected trend direction.
    /// </summary>
    public TrendDirection Trend { get; set; } = TrendDirection.Stable;

    /// <summary>
    /// The trend slope (units per period).
    /// </summary>
    public double TrendSlope { get; set; }

    /// <summary>
    /// Description of the seasonal pattern.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Trend direction for time series.
/// </summary>
public enum TrendDirection
{
    /// <summary>Upward trend.</summary>
    Increasing,

    /// <summary>No significant trend.</summary>
    Stable,

    /// <summary>Downward trend.</summary>
    Decreasing
}
