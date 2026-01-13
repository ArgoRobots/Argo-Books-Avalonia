using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Insights;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Local machine learning forecasting service using ML.NET.
/// Combines SSA (Singular Spectrum Analysis) with Holt-Winters for robust forecasting.
/// </summary>
public class LocalMLForecastingService : ILocalMLForecastingService
{
    private readonly MLContext _mlContext;
    private readonly HoltWintersForecasting _holtWinters;

    // Minimum data requirements
    private const int MinimumDataPointsForSSA = 24;
    private const int MinimumDataPointsForHoltWinters = 12;
    private const int DefaultWindowSize = 6;
    private const int DefaultSeriesLength = 12;

    public LocalMLForecastingService()
    {
        _mlContext = new MLContext(seed: 42);
        _holtWinters = new HoltWintersForecasting();
    }

    /// <inheritdoc />
    public EnhancedForecastResult GenerateEnhancedForecast(
        List<decimal> monthlyData,
        int periodsToForecast = 1,
        ForecastMethod preferredMethod = ForecastMethod.Auto)
    {
        var result = new EnhancedForecastResult
        {
            DataPointsUsed = monthlyData.Count,
            PeriodsForecasted = periodsToForecast
        };

        if (monthlyData.Count < 2)
        {
            result.ForecastedValues = Enumerable.Repeat(monthlyData.FirstOrDefault(), periodsToForecast).ToList();
            result.MethodUsed = "Insufficient Data";
            result.ConfidenceScore = 0;
            return result;
        }

        // Select method based on data availability and preference
        var method = SelectMethod(monthlyData.Count, preferredMethod);
        result.MethodUsed = method.ToString();

        switch (method)
        {
            case ForecastMethod.SSA:
                return GenerateSSAForecast(monthlyData, periodsToForecast, result);

            case ForecastMethod.HoltWinters:
                return GenerateHoltWintersForecast(monthlyData, periodsToForecast, result);

            case ForecastMethod.Combined:
            default:
                return GenerateCombinedForecast(monthlyData, periodsToForecast, result);
        }
    }

    /// <inheritdoc />
    public SeasonalPattern DetectSeasonality(List<decimal> monthlyData)
    {
        if (monthlyData.Count < MinimumDataPointsForHoltWinters)
        {
            return new SeasonalPattern
            {
                SeasonalStrength = 0,
                Description = "Insufficient data to detect seasonal patterns."
            };
        }

        // Detect optimal season length
        var candidateLengths = new[] { 12, 6, 4, 3 }; // Yearly, semi-annual, quarterly, etc.
        var seasonLength = _holtWinters.DetectSeasonLength(monthlyData, candidateLengths);

        // Use Holt-Winters to extract seasonal pattern
        var hwResult = _holtWinters.AutoForecast(monthlyData, seasonLength, 1);
        return hwResult.SeasonalPattern;
    }

    /// <inheritdoc />
    public double CalculateConfidenceScore(
        List<decimal> historicalData,
        SeasonalPattern? seasonalPattern,
        double? historicalAccuracy)
    {
        double score = 0;

        // Data quantity score (0-35 points)
        var dataScore = Math.Min(35, historicalData.Count * 1.5);
        score += dataScore;

        // Data stability score (0-25 points)
        if (historicalData.Count >= 3)
        {
            var cv = CalculateCoefficientOfVariation(historicalData);
            var stabilityScore = cv < 0.1 ? 25 : cv < 0.3 ? 20 : cv < 0.5 ? 15 : cv < 0.8 ? 10 : 5;
            score += stabilityScore;
        }

        // Seasonal pattern score (0-20 points)
        if (seasonalPattern != null && seasonalPattern.SeasonalStrength > 0.1)
        {
            // Strong seasonal pattern = more predictable = higher score
            score += seasonalPattern.SeasonalStrength * 20;
        }
        else if (historicalData.Count >= MinimumDataPointsForHoltWinters)
        {
            // Enough data but weak seasonality - still okay
            score += 10;
        }

        // Historical accuracy bonus (0-20 points)
        if (historicalAccuracy.HasValue && historicalAccuracy.Value > 0)
        {
            score += (historicalAccuracy.Value / 100) * 20;
        }

        return Math.Min(100, Math.Max(0, score));
    }

    #region Private Methods

    private ForecastMethod SelectMethod(int dataPoints, ForecastMethod preferred)
    {
        if (preferred != ForecastMethod.Auto)
        {
            // Check if preferred method has enough data
            return preferred switch
            {
                ForecastMethod.SSA when dataPoints >= MinimumDataPointsForSSA => ForecastMethod.SSA,
                ForecastMethod.HoltWinters when dataPoints >= MinimumDataPointsForHoltWinters => ForecastMethod.HoltWinters,
                ForecastMethod.Combined when dataPoints >= MinimumDataPointsForSSA => ForecastMethod.Combined,
                _ => ForecastMethod.HoltWinters // Fallback
            };
        }

        // Auto-select based on data availability
        if (dataPoints >= MinimumDataPointsForSSA)
        {
            return ForecastMethod.Combined;
        }
        else if (dataPoints >= MinimumDataPointsForHoltWinters)
        {
            return ForecastMethod.HoltWinters;
        }

        return ForecastMethod.HoltWinters; // Will use fallback internally
    }

    private EnhancedForecastResult GenerateSSAForecast(
        List<decimal> data, int periodsToForecast, EnhancedForecastResult result)
    {
        try
        {
            // Prepare data for ML.NET
            var trainingData = data.Select((value, index) => new TimeSeriesInput
            {
                Value = (float)value
            }).ToList();

            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Calculate appropriate window and series length
            var windowSize = Math.Min(DefaultWindowSize, data.Count / 4);
            var seriesLength = Math.Min(DefaultSeriesLength, data.Count / 2);

            windowSize = Math.Max(2, windowSize);
            seriesLength = Math.Max(windowSize + 1, seriesLength);

            // Create SSA forecasting pipeline
            var pipeline = _mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(TimeSeriesOutput.Forecast),
                inputColumnName: nameof(TimeSeriesInput.Value),
                windowSize: windowSize,
                seriesLength: seriesLength,
                trainSize: data.Count,
                horizon: periodsToForecast,
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: nameof(TimeSeriesOutput.LowerBound),
                confidenceUpperBoundColumn: nameof(TimeSeriesOutput.UpperBound));

            // Train the model
            var model = pipeline.Fit(dataView);

            // Create forecasting engine and predict
            var forecastEngine = model.CreateTimeSeriesEngine<TimeSeriesInput, TimeSeriesOutput>(_mlContext);
            var forecast = forecastEngine.Predict();

            result.ForecastedValues = forecast.Forecast.Select(f => (decimal)Math.Max(0, f)).ToList();
            result.LowerBounds = forecast.LowerBound.Select(f => (decimal)Math.Max(0, f)).ToList();
            result.UpperBounds = forecast.UpperBound.Select(f => (decimal)f).ToList();
            result.MethodUsed = "ML.NET SSA";

            // Calculate confidence based on prediction interval width
            var avgValue = result.ForecastedValues.Average();
            var avgWidth = result.UpperBounds.Zip(result.LowerBounds, (u, l) => u - l).Average();
            var relativeWidth = avgValue > 0 ? (double)(avgWidth / avgValue) : 1;

            result.ConfidenceScore = CalculateConfidenceScore(data, null, null);

            // Detect seasonality for the result
            result.SeasonalPattern = DetectSeasonality(data);
        }
        catch (Exception)
        {
            // Fallback to Holt-Winters if SSA fails
            return GenerateHoltWintersForecast(data, periodsToForecast, result);
        }

        return result;
    }

    private EnhancedForecastResult GenerateHoltWintersForecast(
        List<decimal> data, int periodsToForecast, EnhancedForecastResult result)
    {
        // Detect optimal season length
        var candidateLengths = new[] { 12, 6, 4, 3 };
        var seasonLength = data.Count >= 12
            ? _holtWinters.DetectSeasonLength(data, candidateLengths)
            : Math.Min(4, data.Count / 2);

        seasonLength = Math.Max(2, seasonLength);

        var hwResult = _holtWinters.AutoForecast(data, seasonLength, periodsToForecast);

        result.ForecastedValues = hwResult.ForecastedValues;
        result.SeasonalPattern = hwResult.SeasonalPattern;
        result.MethodUsed = hwResult.Method;
        result.ConfidenceScore = CalculateConfidenceScore(data, hwResult.SeasonalPattern, null);

        // Estimate confidence bounds (±15% for medium confidence)
        var boundsPercent = result.ConfidenceScore >= 70 ? 0.10m : 0.20m;
        result.LowerBounds = hwResult.ForecastedValues.Select(v => v * (1 - boundsPercent)).ToList();
        result.UpperBounds = hwResult.ForecastedValues.Select(v => v * (1 + boundsPercent)).ToList();

        return result;
    }

    private EnhancedForecastResult GenerateCombinedForecast(
        List<decimal> data, int periodsToForecast, EnhancedForecastResult result)
    {
        // Generate forecasts from both methods
        var ssaResult = new EnhancedForecastResult { DataPointsUsed = data.Count };
        var hwResult = new EnhancedForecastResult { DataPointsUsed = data.Count };

        ssaResult = GenerateSSAForecast(data, periodsToForecast, ssaResult);
        hwResult = GenerateHoltWintersForecast(data, periodsToForecast, hwResult);

        // Combine forecasts with weighted average
        // Weight SSA higher when we have more data (it's more sophisticated)
        var ssaWeight = data.Count >= 36 ? 0.6 : 0.5;
        var hwWeight = 1 - ssaWeight;

        var combinedForecasts = new List<decimal>();
        var combinedLower = new List<decimal>();
        var combinedUpper = new List<decimal>();

        for (int i = 0; i < periodsToForecast; i++)
        {
            var ssaValue = i < ssaResult.ForecastedValues.Count ? ssaResult.ForecastedValues[i] : 0;
            var hwValue = i < hwResult.ForecastedValues.Count ? hwResult.ForecastedValues[i] : 0;

            combinedForecasts.Add((decimal)(ssaWeight * (double)ssaValue + hwWeight * (double)hwValue));

            // Combine bounds
            var ssaLower = i < ssaResult.LowerBounds.Count ? ssaResult.LowerBounds[i] : ssaValue * 0.8m;
            var hwLower = i < hwResult.LowerBounds.Count ? hwResult.LowerBounds[i] : hwValue * 0.8m;
            combinedLower.Add(Math.Min(ssaLower, hwLower));

            var ssaUpper = i < ssaResult.UpperBounds.Count ? ssaResult.UpperBounds[i] : ssaValue * 1.2m;
            var hwUpper = i < hwResult.UpperBounds.Count ? hwResult.UpperBounds[i] : hwValue * 1.2m;
            combinedUpper.Add(Math.Max(ssaUpper, hwUpper));
        }

        result.ForecastedValues = combinedForecasts;
        result.LowerBounds = combinedLower;
        result.UpperBounds = combinedUpper;
        result.SeasonalPattern = hwResult.SeasonalPattern; // Use HW for seasonal pattern
        result.MethodUsed = "Combined (SSA + Holt-Winters)";

        // Confidence is boosted when methods agree
        var methodAgreement = CalculateMethodAgreement(ssaResult.ForecastedValues, hwResult.ForecastedValues);
        var baseConfidence = (ssaResult.ConfidenceScore + hwResult.ConfidenceScore) / 2;
        result.ConfidenceScore = Math.Min(100, baseConfidence + (methodAgreement * 10));

        return result;
    }

    private double CalculateMethodAgreement(List<decimal> forecast1, List<decimal> forecast2)
    {
        if (forecast1.Count == 0 || forecast2.Count == 0) return 0;

        var minCount = Math.Min(forecast1.Count, forecast2.Count);
        var differences = new List<double>();

        for (int i = 0; i < minCount; i++)
        {
            var avg = ((double)forecast1[i] + (double)forecast2[i]) / 2;
            if (avg > 0)
            {
                var diff = Math.Abs((double)forecast1[i] - (double)forecast2[i]) / avg;
                differences.Add(diff);
            }
        }

        if (!differences.Any()) return 0;

        // Agreement score: 1 = perfect agreement, 0 = large disagreement
        var avgDiff = differences.Average();
        return Math.Max(0, 1 - avgDiff);
    }

    private double CalculateCoefficientOfVariation(List<decimal> data)
    {
        if (data.Count < 2) return 0;
        var mean = data.Average();
        if (mean == 0) return 1;

        var variance = data.Select(x => (double)((x - mean) * (x - mean))).Average();
        var stdDev = Math.Sqrt(variance);
        return stdDev / (double)mean;
    }

    #endregion

    #region ML.NET Data Classes

    private class TimeSeriesInput
    {
        public float Value { get; set; }
    }

    private class TimeSeriesOutput
    {
        public float[] Forecast { get; set; } = [];
        public float[] LowerBound { get; set; } = [];
        public float[] UpperBound { get; set; } = [];
    }

    #endregion
}

/// <summary>
/// Interface for the local ML forecasting service.
/// </summary>
public interface ILocalMLForecastingService
{
    /// <summary>
    /// Generates an enhanced forecast using ML.NET and/or Holt-Winters.
    /// </summary>
    EnhancedForecastResult GenerateEnhancedForecast(
        List<decimal> monthlyData,
        int periodsToForecast = 1,
        ForecastMethod preferredMethod = ForecastMethod.Auto);

    /// <summary>
    /// Detects seasonal patterns in the data.
    /// </summary>
    SeasonalPattern DetectSeasonality(List<decimal> monthlyData);

    /// <summary>
    /// Calculates a confidence score for the forecast.
    /// </summary>
    double CalculateConfidenceScore(
        List<decimal> historicalData,
        SeasonalPattern? seasonalPattern,
        double? historicalAccuracy);
}

/// <summary>
/// Result of an enhanced forecast operation.
/// </summary>
public class EnhancedForecastResult
{
    /// <summary>
    /// The primary forecasted values.
    /// </summary>
    public List<decimal> ForecastedValues { get; set; } = [];

    /// <summary>
    /// Lower confidence bounds for the forecast.
    /// </summary>
    public List<decimal> LowerBounds { get; set; } = [];

    /// <summary>
    /// Upper confidence bounds for the forecast.
    /// </summary>
    public List<decimal> UpperBounds { get; set; } = [];

    /// <summary>
    /// Detected seasonal pattern.
    /// </summary>
    public SeasonalPattern SeasonalPattern { get; set; } = new();

    /// <summary>
    /// Confidence score for the forecast (0-100).
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// The forecasting method used.
    /// </summary>
    public string MethodUsed { get; set; } = string.Empty;

    /// <summary>
    /// Number of historical data points used.
    /// </summary>
    public int DataPointsUsed { get; set; }

    /// <summary>
    /// Number of periods forecasted.
    /// </summary>
    public int PeriodsForecasted { get; set; }

    /// <summary>
    /// Gets the first forecasted value.
    /// </summary>
    public decimal ForecastedValue => ForecastedValues.FirstOrDefault();

    /// <summary>
    /// Gets the confidence level description.
    /// </summary>
    public string ConfidenceLevel => ConfidenceScore switch
    {
        >= 80 => "High",
        >= 50 => "Medium",
        _ => "Low"
    };
}

/// <summary>
/// Forecasting method selection.
/// </summary>
public enum ForecastMethod
{
    /// <summary>Auto-select based on data characteristics.</summary>
    Auto,

    /// <summary>Use ML.NET SSA (Singular Spectrum Analysis).</summary>
    SSA,

    /// <summary>Use Holt-Winters triple exponential smoothing.</summary>
    HoltWinters,

    /// <summary>Combine multiple methods for better accuracy.</summary>
    Combined
}
