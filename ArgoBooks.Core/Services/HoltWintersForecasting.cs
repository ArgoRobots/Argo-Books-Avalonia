using ArgoBooks.Core.Models.Insights;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Holt-Winters triple exponential smoothing for time series forecasting.
/// Handles level, trend, and seasonal components.
/// </summary>
public class HoltWintersForecasting
{
    // Smoothing parameters (can be tuned for different data characteristics)
    private const double DefaultAlpha = 0.3; // Level smoothing
    private const double DefaultBeta = 0.1;  // Trend smoothing
    private const double DefaultGamma = 0.2; // Seasonal smoothing

    /// <summary>
    /// Generates a forecast using the Holt-Winters additive method.
    /// Best for data where seasonal variations are roughly constant over time.
    /// </summary>
    /// <param name="data">Historical time series data.</param>
    /// <param name="seasonLength">Length of one seasonal cycle (e.g., 12 for monthly data with yearly seasonality).</param>
    /// <param name="periodsToForecast">Number of periods to forecast ahead.</param>
    /// <returns>Forecast result with values and seasonal pattern information.</returns>
    public HoltWintersForecastResult ForecastAdditive(
        List<decimal> data,
        int seasonLength = 12,
        int periodsToForecast = 1)
    {
        var result = new HoltWintersForecastResult();

        if (data.Count < seasonLength * 2)
        {
            // Not enough data for seasonal decomposition, fall back to simple exponential smoothing
            return FallbackForecast(data, periodsToForecast, result);
        }

        var values = data.Select(d => (double)d).ToList();
        var n = values.Count;

        // Initialize components
        var (level, trend, seasonals) = InitializeComponents(values, seasonLength);

        // Apply Holt-Winters smoothing
        var smoothedLevel = new double[n];
        var smoothedTrend = new double[n];
        var smoothedSeasonals = new double[n + seasonLength];

        // Copy initial seasonal factors
        for (int i = 0; i < seasonLength; i++)
        {
            smoothedSeasonals[i] = seasonals[i];
        }

        smoothedLevel[0] = level;
        smoothedTrend[0] = trend;

        // Smooth through the data
        for (int t = 1; t < n; t++)
        {
            int seasonIndex = t % seasonLength;
            double prevSeasonal = smoothedSeasonals[t - seasonLength + seasonLength]; // Previous year's seasonal

            // Update level: alpha * (Y_t - S_{t-m}) + (1 - alpha) * (L_{t-1} + T_{t-1})
            smoothedLevel[t] = DefaultAlpha * (values[t] - prevSeasonal) +
                               (1 - DefaultAlpha) * (smoothedLevel[t - 1] + smoothedTrend[t - 1]);

            // Update trend: beta * (L_t - L_{t-1}) + (1 - beta) * T_{t-1}
            smoothedTrend[t] = DefaultBeta * (smoothedLevel[t] - smoothedLevel[t - 1]) +
                               (1 - DefaultBeta) * smoothedTrend[t - 1];

            // Update seasonal: gamma * (Y_t - L_t) + (1 - gamma) * S_{t-m}
            smoothedSeasonals[t + seasonLength] = DefaultGamma * (values[t] - smoothedLevel[t]) +
                                                   (1 - DefaultGamma) * prevSeasonal;
        }

        // Generate forecasts
        var lastLevel = smoothedLevel[n - 1];
        var lastTrend = smoothedTrend[n - 1];

        var forecasts = new List<double>();
        for (int h = 1; h <= periodsToForecast; h++)
        {
            int seasonIndex = (n + h - 1) % seasonLength;
            double seasonalFactor = smoothedSeasonals[n - seasonLength + seasonIndex + seasonLength];
            double forecast = lastLevel + (h * lastTrend) + seasonalFactor;
            forecasts.Add(Math.Max(0, forecast)); // Ensure non-negative
        }

        // Extract final seasonal factors
        var finalSeasonals = new List<double>();
        for (int i = 0; i < seasonLength; i++)
        {
            finalSeasonals.Add(smoothedSeasonals[n + i]);
        }

        // Calculate seasonal strength
        var seasonalVariance = finalSeasonals.Select(s => s * s).Average();
        var dataVariance = CalculateVariance(values);
        var seasonalStrength = dataVariance > 0 ? Math.Min(1, seasonalVariance / dataVariance) : 0;

        result.ForecastedValues = forecasts.Select(f => (decimal)f).ToList();
        result.SeasonalPattern = new SeasonalPattern
        {
            SeasonLength = seasonLength,
            SeasonalFactors = finalSeasonals,
            SeasonalStrength = seasonalStrength,
            Trend = lastTrend > 0.01 ? TrendDirection.Increasing :
                    lastTrend < -0.01 ? TrendDirection.Decreasing : TrendDirection.Stable,
            TrendSlope = lastTrend,
            Description = GenerateSeasonalDescription(finalSeasonals, seasonLength, seasonalStrength)
        };
        result.FinalLevel = (decimal)lastLevel;
        result.FinalTrend = (decimal)lastTrend;
        result.Method = "Holt-Winters Additive";

        return result;
    }

    /// <summary>
    /// Generates a forecast using the Holt-Winters multiplicative method.
    /// Best for data where seasonal variations scale with the level.
    /// </summary>
    public HoltWintersForecastResult ForecastMultiplicative(
        List<decimal> data,
        int seasonLength = 12,
        int periodsToForecast = 1)
    {
        var result = new HoltWintersForecastResult();

        if (data.Count < seasonLength * 2)
        {
            return FallbackForecast(data, periodsToForecast, result);
        }

        var values = data.Select(d => (double)d).ToList();

        // Check for zero or negative values (multiplicative doesn't work with these)
        if (values.Any(v => v <= 0))
        {
            return ForecastAdditive(data, seasonLength, periodsToForecast);
        }

        var n = values.Count;

        // Initialize components for multiplicative
        var (level, trend, seasonals) = InitializeComponentsMultiplicative(values, seasonLength);

        // Apply Holt-Winters multiplicative smoothing
        var smoothedLevel = new double[n];
        var smoothedTrend = new double[n];
        var smoothedSeasonals = new double[n + seasonLength];

        // Copy initial seasonal factors
        for (int i = 0; i < seasonLength; i++)
        {
            smoothedSeasonals[i] = seasonals[i];
        }

        smoothedLevel[0] = level;
        smoothedTrend[0] = trend;

        for (int t = 1; t < n; t++)
        {
            double prevSeasonal = smoothedSeasonals[t - seasonLength + seasonLength];

            // Avoid division by zero
            if (Math.Abs(prevSeasonal) < 0.0001) prevSeasonal = 0.0001;

            // Update level: alpha * (Y_t / S_{t-m}) + (1 - alpha) * (L_{t-1} + T_{t-1})
            smoothedLevel[t] = DefaultAlpha * (values[t] / prevSeasonal) +
                               (1 - DefaultAlpha) * (smoothedLevel[t - 1] + smoothedTrend[t - 1]);

            // Update trend: beta * (L_t - L_{t-1}) + (1 - beta) * T_{t-1}
            smoothedTrend[t] = DefaultBeta * (smoothedLevel[t] - smoothedLevel[t - 1]) +
                               (1 - DefaultBeta) * smoothedTrend[t - 1];

            // Avoid division by zero for seasonal update
            var levelForSeasonal = Math.Abs(smoothedLevel[t]) < 0.0001 ? 0.0001 : smoothedLevel[t];

            // Update seasonal: gamma * (Y_t / L_t) + (1 - gamma) * S_{t-m}
            smoothedSeasonals[t + seasonLength] = DefaultGamma * (values[t] / levelForSeasonal) +
                                                   (1 - DefaultGamma) * prevSeasonal;
        }

        // Generate forecasts
        var lastLevel = smoothedLevel[n - 1];
        var lastTrend = smoothedTrend[n - 1];

        var forecasts = new List<double>();
        for (int h = 1; h <= periodsToForecast; h++)
        {
            int seasonIndex = (n + h - 1) % seasonLength;
            double seasonalFactor = smoothedSeasonals[n - seasonLength + seasonIndex + seasonLength];
            double forecast = (lastLevel + (h * lastTrend)) * seasonalFactor;
            forecasts.Add(Math.Max(0, forecast));
        }

        // Extract final seasonal factors
        var finalSeasonals = new List<double>();
        for (int i = 0; i < seasonLength; i++)
        {
            finalSeasonals.Add(smoothedSeasonals[n + i]);
        }

        // Calculate seasonal strength
        var seasonalDeviation = finalSeasonals.Select(s => Math.Abs(s - 1)).Average();
        var seasonalStrength = Math.Min(1, seasonalDeviation * 5); // Scale to 0-1

        result.ForecastedValues = forecasts.Select(f => (decimal)f).ToList();
        result.SeasonalPattern = new SeasonalPattern
        {
            SeasonLength = seasonLength,
            SeasonalFactors = finalSeasonals,
            SeasonalStrength = seasonalStrength,
            Trend = lastTrend > 0.01 ? TrendDirection.Increasing :
                    lastTrend < -0.01 ? TrendDirection.Decreasing : TrendDirection.Stable,
            TrendSlope = lastTrend,
            Description = GenerateSeasonalDescription(finalSeasonals, seasonLength, seasonalStrength)
        };
        result.FinalLevel = (decimal)lastLevel;
        result.FinalTrend = (decimal)lastTrend;
        result.Method = "Holt-Winters Multiplicative";

        return result;
    }

    /// <summary>
    /// Automatically selects the best method based on data characteristics.
    /// </summary>
    public HoltWintersForecastResult AutoForecast(
        List<decimal> data,
        int seasonLength = 12,
        int periodsToForecast = 1)
    {
        if (data.Count < seasonLength)
        {
            return FallbackForecast(data, periodsToForecast, new HoltWintersForecastResult());
        }

        // Check if multiplicative is appropriate (no zeros, seasonal variation scales with level)
        var hasZeros = data.Any(d => d <= 0);
        if (hasZeros)
        {
            return ForecastAdditive(data, seasonLength, periodsToForecast);
        }

        // Compute coefficient of variation for each season to detect multiplicative pattern
        var values = data.Select(d => (double)d).ToList();
        var seasonGroups = values.Select((v, i) => new { Value = v, Season = i % seasonLength })
            .GroupBy(x => x.Season)
            .Select(g => new { Mean = g.Average(x => x.Value), Std = Math.Sqrt(g.Select(x => x.Value).Variance()) })
            .ToList();

        var cvs = seasonGroups.Where(g => g.Mean > 0).Select(g => g.Std / g.Mean).ToList();
        var cvVariation = cvs.Any() ? cvs.StandardDeviation() : 0;

        // If CV is relatively constant, multiplicative is better
        if (cvVariation < 0.3)
        {
            return ForecastMultiplicative(data, seasonLength, periodsToForecast);
        }

        return ForecastAdditive(data, seasonLength, periodsToForecast);
    }

    /// <summary>
    /// Detects the optimal season length from data.
    /// </summary>
    public int DetectSeasonLength(List<decimal> data, int[] candidateLengths)
    {
        if (data.Count < 24) return candidateLengths.FirstOrDefault(l => l <= data.Count / 2);

        var values = data.Select(d => (double)d).ToArray();
        var bestLength = candidateLengths[0];
        var bestAutocorrelation = double.MinValue;

        foreach (var length in candidateLengths.Where(l => l <= values.Length / 2))
        {
            var autocorr = CalculateAutocorrelation(values, length);
            if (autocorr > bestAutocorrelation)
            {
                bestAutocorrelation = autocorr;
                bestLength = length;
            }
        }

        return bestLength;
    }

    #region Private Methods

    private (double level, double trend, double[] seasonals) InitializeComponents(
        List<double> values, int seasonLength)
    {
        // Initial level: average of first season
        var firstSeasonAvg = values.Take(seasonLength).Average();

        // Initial trend: average change between first two seasons
        var secondSeasonAvg = values.Skip(seasonLength).Take(seasonLength).Average();
        var initialTrend = (secondSeasonAvg - firstSeasonAvg) / seasonLength;

        // Initial seasonal factors (additive): deviation from first season average
        var seasonals = new double[seasonLength];
        for (int i = 0; i < seasonLength; i++)
        {
            seasonals[i] = values[i] - firstSeasonAvg;
        }

        return (firstSeasonAvg, initialTrend, seasonals);
    }

    private (double level, double trend, double[] seasonals) InitializeComponentsMultiplicative(
        List<double> values, int seasonLength)
    {
        // Initial level: average of first season
        var firstSeasonAvg = values.Take(seasonLength).Average();
        if (firstSeasonAvg <= 0) firstSeasonAvg = 0.0001;

        // Initial trend: average change between first two seasons
        var secondSeasonAvg = values.Skip(seasonLength).Take(seasonLength).Average();
        var initialTrend = (secondSeasonAvg - firstSeasonAvg) / seasonLength;

        // Initial seasonal factors (multiplicative): ratio to first season average
        var seasonals = new double[seasonLength];
        for (int i = 0; i < seasonLength; i++)
        {
            seasonals[i] = values[i] / firstSeasonAvg;
            if (seasonals[i] <= 0) seasonals[i] = 0.0001;
        }

        return (firstSeasonAvg, initialTrend, seasonals);
    }

    private HoltWintersForecastResult FallbackForecast(
        List<decimal> data, int periodsToForecast, HoltWintersForecastResult result)
    {
        if (data.Count == 0)
        {
            result.ForecastedValues = Enumerable.Repeat(0m, periodsToForecast).ToList();
            result.Method = "No Data";
            return result;
        }

        // Simple exponential smoothing
        var values = data.Select(d => (double)d).ToList();
        var smoothed = values[0];

        foreach (var v in values.Skip(1))
        {
            smoothed = DefaultAlpha * v + (1 - DefaultAlpha) * smoothed;
        }

        // Simple trend calculation
        var trend = data.Count > 1
            ? ((double)data[^1] - (double)data[0]) / (data.Count - 1)
            : 0;

        var forecasts = new List<decimal>();
        for (int h = 1; h <= periodsToForecast; h++)
        {
            forecasts.Add((decimal)Math.Max(0, smoothed + h * trend));
        }

        result.ForecastedValues = forecasts;
        result.FinalLevel = (decimal)smoothed;
        result.FinalTrend = (decimal)trend;
        result.SeasonalPattern = new SeasonalPattern
        {
            SeasonalStrength = 0,
            Description = "Insufficient data for seasonal analysis."
        };
        result.Method = "Simple Exponential Smoothing";

        return result;
    }

    private double CalculateVariance(List<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        return values.Select(v => (v - mean) * (v - mean)).Sum() / (values.Count - 1);
    }

    private double CalculateAutocorrelation(double[] values, int lag)
    {
        var n = values.Length;
        if (lag >= n) return 0;

        var mean = values.Average();
        var variance = values.Select(v => (v - mean) * (v - mean)).Sum();

        if (variance == 0) return 0;

        double autocovariance = 0;
        for (int i = 0; i < n - lag; i++)
        {
            autocovariance += (values[i] - mean) * (values[i + lag] - mean);
        }

        return autocovariance / variance;
    }

    private string GenerateSeasonalDescription(List<double> seasonals, int seasonLength, double strength)
    {
        if (strength < 0.1)
        {
            return "No significant seasonal pattern detected.";
        }

        // Find peak and trough seasons
        var maxIndex = seasonals.IndexOf(seasonals.Max());
        var minIndex = seasonals.IndexOf(seasonals.Min());

        string peakPeriod, troughPeriod;
        string cycleDescription;

        if (seasonLength == 12)
        {
            var months = new[] { "January", "February", "March", "April", "May", "June",
                                 "July", "August", "September", "October", "November", "December" };
            peakPeriod = months[maxIndex % 12];
            troughPeriod = months[minIndex % 12];
            cycleDescription = "yearly";
        }
        else if (seasonLength == 6)
        {
            var biAnnual = new[] { "Jan-Feb", "Mar-Apr", "May-Jun", "Jul-Aug", "Sep-Oct", "Nov-Dec" };
            peakPeriod = biAnnual[maxIndex % 6];
            troughPeriod = biAnnual[minIndex % 6];
            cycleDescription = "bi-monthly";
        }
        else if (seasonLength == 4)
        {
            var quarters = new[] { "Q1 (Jan-Mar)", "Q2 (Apr-Jun)", "Q3 (Jul-Sep)", "Q4 (Oct-Dec)" };
            peakPeriod = quarters[maxIndex % 4];
            troughPeriod = quarters[minIndex % 4];
            cycleDescription = "quarterly";
        }
        else if (seasonLength == 3)
        {
            // For 3-period cycles, describe as ordinal within cycle
            peakPeriod = maxIndex == 0 ? "beginning" : maxIndex == 1 ? "middle" : "end";
            troughPeriod = minIndex == 0 ? "beginning" : minIndex == 1 ? "middle" : "end";
            cycleDescription = "3-month";
        }
        else if (seasonLength == 2)
        {
            peakPeriod = maxIndex == 0 ? "first month" : "second month";
            troughPeriod = minIndex == 0 ? "first month" : "second month";
            cycleDescription = "bi-monthly";
        }
        else
        {
            peakPeriod = $"period {maxIndex + 1}";
            troughPeriod = $"period {minIndex + 1}";
            cycleDescription = $"{seasonLength}-period";
        }

        var strengthDesc = strength > 0.5 ? "strong" : strength > 0.25 ? "moderate" : "mild";

        return $"A {strengthDesc} {cycleDescription} pattern detected. Peak at {peakPeriod} of cycle, lowest at {troughPeriod}.";
    }

    #endregion
}

/// <summary>
/// Result of a Holt-Winters forecast operation.
/// </summary>
public class HoltWintersForecastResult
{
    /// <summary>
    /// The forecasted values for future periods.
    /// </summary>
    public List<decimal> ForecastedValues { get; set; } = [];

    /// <summary>
    /// The detected seasonal pattern.
    /// </summary>
    public SeasonalPattern SeasonalPattern { get; set; } = new();

    /// <summary>
    /// The final level component.
    /// </summary>
    public decimal FinalLevel { get; set; }

    /// <summary>
    /// The final trend component.
    /// </summary>
    public decimal FinalTrend { get; set; }

    /// <summary>
    /// The method used for forecasting.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets the first forecasted value (for single-period forecasts).
    /// </summary>
    public decimal ForecastedValue => ForecastedValues.FirstOrDefault();
}

/// <summary>
/// Extension methods for statistical calculations.
/// </summary>
internal static class StatisticsExtensions
{
    public static double Variance(this IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count < 2) return 0;
        var mean = list.Average();
        return list.Select(v => (v - mean) * (v - mean)).Sum() / (list.Count - 1);
    }

    public static double StandardDeviation(this IEnumerable<double> values)
    {
        return Math.Sqrt(values.Variance());
    }
}
