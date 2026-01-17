using ArgoBooks.Core.Models.Insights;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the HoltWintersForecasting service.
/// </summary>
public class HoltWintersTests
{
    private readonly HoltWintersForecasting _forecasting = new();

    #region Fallback Forecast Tests

    [Fact]
    public void ForecastAdditive_EmptyData_ReturnsFallbackForecast()
    {
        var result = _forecasting.ForecastAdditive([], periodsToForecast: 3);

        Assert.Equal("No Data", result.Method);
        Assert.Equal(3, result.ForecastedValues.Count);
        Assert.All(result.ForecastedValues, v => Assert.Equal(0m, v));
    }

    [Fact]
    public void ForecastAdditive_InsufficientData_UsesFallback()
    {
        var data = new List<decimal> { 100, 110, 120, 130 }; // Less than 2 seasons (24 points for seasonLength=12)
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 1);

        Assert.Equal("Simple Exponential Smoothing", result.Method);
        Assert.Single(result.ForecastedValues);
    }

    [Fact]
    public void FallbackForecast_SingleDataPoint_UsesSimpleSmoothing()
    {
        var data = new List<decimal> { 100 };
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 1);

        Assert.Equal("Simple Exponential Smoothing", result.Method);
        Assert.Single(result.ForecastedValues);
        Assert.Equal(100m, result.ForecastedValues[0]);
    }

    #endregion

    #region Additive Forecast Tests

    [Fact]
    public void ForecastAdditive_SufficientData_ReturnsHoltWintersMethod()
    {
        // Create 24 months of data (2 full seasonal cycles)
        var data = GenerateSeasonalData(24, baseLevel: 1000, trend: 10, seasonalAmplitude: 100);
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 1);

        Assert.Equal("Holt-Winters Additive", result.Method);
        Assert.Single(result.ForecastedValues);
    }

    [Fact]
    public void ForecastAdditive_MultiplePeriodsAhead_ReturnsCorrectCount()
    {
        var data = GenerateSeasonalData(24, baseLevel: 1000, trend: 10, seasonalAmplitude: 100);
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 6);

        Assert.Equal(6, result.ForecastedValues.Count);
    }

    [Fact]
    public void ForecastAdditive_WithTrend_ReflectsTrendInForecast()
    {
        // Strong upward trend
        var data = GenerateSeasonalData(24, baseLevel: 1000, trend: 50, seasonalAmplitude: 50);
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 3);

        // Final trend should be positive
        Assert.True(result.FinalTrend > 0);
    }

    [Fact]
    public void ForecastAdditive_ReturnsNonNegativeValues()
    {
        // Data with potential negative forecast
        var data = GenerateSeasonalData(24, baseLevel: 100, trend: -5, seasonalAmplitude: 50);
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 12);

        // All forecasts should be non-negative
        Assert.All(result.ForecastedValues, v => Assert.True(v >= 0));
    }

    #endregion

    #region Multiplicative Forecast Tests

    [Fact]
    public void ForecastMultiplicative_SufficientData_ReturnsHoltWintersMethod()
    {
        var data = GenerateSeasonalData(24, baseLevel: 1000, trend: 10, seasonalAmplitude: 100);
        var result = _forecasting.ForecastMultiplicative(data, seasonLength: 12, periodsToForecast: 1);

        Assert.Equal("Holt-Winters Multiplicative", result.Method);
    }

    [Fact]
    public void ForecastMultiplicative_WithZeroValues_FallsBackToAdditive()
    {
        var data = new List<decimal> { 100, 0, 120, 130 };
        // Extend to have enough data
        for (int i = 0; i < 20; i++)
        {
            data.Add(100 + i * 5);
        }

        var result = _forecasting.ForecastMultiplicative(data, seasonLength: 12, periodsToForecast: 1);

        // Should fall back to additive due to zero values
        Assert.Contains("Additive", result.Method);
    }

    [Fact]
    public void ForecastMultiplicative_InsufficientData_UsesFallback()
    {
        var data = new List<decimal> { 100, 110, 120 };
        var result = _forecasting.ForecastMultiplicative(data, seasonLength: 12, periodsToForecast: 1);

        Assert.Equal("Simple Exponential Smoothing", result.Method);
    }

    #endregion

    #region AutoForecast Tests

    [Fact]
    public void AutoForecast_InsufficientData_UsesFallback()
    {
        var data = new List<decimal> { 100, 110, 120 };
        var result = _forecasting.AutoForecast(data, seasonLength: 12, periodsToForecast: 1);

        Assert.Equal("Simple Exponential Smoothing", result.Method);
    }

    [Fact]
    public void AutoForecast_WithZeroValues_UsesAdditive()
    {
        var data = GenerateSeasonalData(24, baseLevel: 1000, trend: 10, seasonalAmplitude: 100);
        data[5] = 0; // Insert zero value

        var result = _forecasting.AutoForecast(data, seasonLength: 12, periodsToForecast: 1);

        Assert.Contains("Additive", result.Method);
    }

    [Fact]
    public void AutoForecast_PositiveData_SelectsAppropriateMethod()
    {
        var data = GenerateSeasonalData(24, baseLevel: 1000, trend: 10, seasonalAmplitude: 100);
        var result = _forecasting.AutoForecast(data, seasonLength: 12, periodsToForecast: 1);

        // Should select either additive or multiplicative
        Assert.True(result.Method.Contains("Holt-Winters") || result.Method.Contains("Exponential"));
    }

    #endregion

    #region Seasonal Pattern Tests

    [Fact]
    public void ForecastAdditive_DetectsSeasonalPattern()
    {
        var data = GenerateSeasonalData(24, baseLevel: 1000, trend: 0, seasonalAmplitude: 200);
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 1);

        Assert.NotNull(result.SeasonalPattern);
        Assert.Equal(12, result.SeasonalPattern.SeasonLength);
        Assert.Equal(12, result.SeasonalPattern.SeasonalFactors.Count);
    }

    [Fact]
    public void ForecastAdditive_NoSeasonality_LowSeasonalStrength()
    {
        // Flat data with no seasonality
        var data = Enumerable.Repeat(1000m, 24).ToList();
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 1);

        // Seasonal strength should be low for flat data
        Assert.True(result.SeasonalPattern.SeasonalStrength < 0.3);
    }

    [Fact]
    public void SeasonalPattern_TrendDirection_DetectsIncreasing()
    {
        var data = GenerateSeasonalData(24, baseLevel: 1000, trend: 50, seasonalAmplitude: 50);
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 1);

        Assert.Equal(TrendDirection.Increasing, result.SeasonalPattern.Trend);
    }

    [Fact]
    public void SeasonalPattern_TrendDirection_DetectsDecreasing()
    {
        var data = GenerateSeasonalData(24, baseLevel: 2000, trend: -50, seasonalAmplitude: 50);
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 1);

        Assert.Equal(TrendDirection.Decreasing, result.SeasonalPattern.Trend);
    }

    [Fact]
    public void SeasonalPattern_TrendDirection_DetectsStable()
    {
        var data = GenerateSeasonalData(24, baseLevel: 1000, trend: 0, seasonalAmplitude: 50);
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 1);

        Assert.Equal(TrendDirection.Stable, result.SeasonalPattern.Trend);
    }

    [Fact]
    public void SeasonalPattern_Description_IsGenerated()
    {
        var data = GenerateSeasonalData(24, baseLevel: 1000, trend: 10, seasonalAmplitude: 200);
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 1);

        Assert.False(string.IsNullOrEmpty(result.SeasonalPattern.Description));
    }

    #endregion

    #region Season Length Detection Tests

    [Fact]
    public void DetectSeasonLength_InsufficientData_ReturnsFirstValidCandidate()
    {
        // With only 10 data points, data.Count / 2 = 5
        // Only candidates <= 5 are valid, so it should return 4
        var data = new List<decimal> { 100, 110, 120, 130, 140, 150, 160, 170, 180, 190 };
        var candidates = new[] { 12, 4, 6 };

        var detected = _forecasting.DetectSeasonLength(data, candidates);

        // Should return 4 (first candidate that fits data.Count / 2 = 5)
        Assert.Equal(4, detected);
    }

    [Fact]
    public void DetectSeasonLength_VeryShortData_ReturnsZeroWhenNoCandidateFits()
    {
        var data = new List<decimal> { 100, 110, 120 };
        var candidates = new[] { 12, 4, 6 };

        var detected = _forecasting.DetectSeasonLength(data, candidates);

        // With data.Count / 2 = 1, no candidates fit, returns default (0)
        Assert.Equal(0, detected);
    }

    [Fact]
    public void DetectSeasonLength_QuarterlyCycle_DetectsCorrectly()
    {
        // Generate quarterly seasonal data
        var data = new List<decimal>();
        for (int year = 0; year < 3; year++)
        {
            data.AddRange(new decimal[] { 100, 150, 200, 120 }); // Q1, Q2, Q3, Q4
        }

        var candidates = new[] { 4, 6, 12 };
        var detected = _forecasting.DetectSeasonLength(data, candidates);

        // Should detect quarterly (4) pattern
        Assert.Equal(4, detected);
    }

    #endregion

    #region ForecastedValue Property Tests

    [Fact]
    public void ForecastedValue_ReturnFirstValue()
    {
        var data = GenerateSeasonalData(24, baseLevel: 1000, trend: 10, seasonalAmplitude: 100);
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 3);

        Assert.Equal(result.ForecastedValues[0], result.ForecastedValue);
    }

    [Fact]
    public void ForecastedValue_EmptyList_ReturnsDefault()
    {
        var result = new HoltWintersForecastResult
        {
            ForecastedValues = []
        };

        Assert.Equal(0m, result.ForecastedValue);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void ForecastAdditive_AllSameValues_Handles()
    {
        var data = Enumerable.Repeat(500m, 24).ToList();
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 1);

        // Should handle constant data without errors
        Assert.NotNull(result);
        Assert.Single(result.ForecastedValues);
    }

    [Fact]
    public void ForecastAdditive_VerySmallValues_Handles()
    {
        var data = Enumerable.Range(0, 24).Select(i => 0.001m + i * 0.0001m).ToList();
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 1);

        Assert.NotNull(result);
        Assert.True(result.ForecastedValues[0] >= 0);
    }

    [Fact]
    public void ForecastAdditive_LargeValues_Handles()
    {
        var data = Enumerable.Range(0, 24).Select(i => 1000000m + i * 10000m).ToList();
        var result = _forecasting.ForecastAdditive(data, seasonLength: 12, periodsToForecast: 1);

        Assert.NotNull(result);
        Assert.True(result.ForecastedValues[0] > 0);
    }

    #endregion

    #region Different Season Lengths

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(12)]
    public void ForecastAdditive_VariousSeasonLengths_Works(int seasonLength)
    {
        var data = GenerateSeasonalData(seasonLength * 2, baseLevel: 1000, trend: 10, seasonalAmplitude: 100);
        var result = _forecasting.ForecastAdditive(data, seasonLength: seasonLength, periodsToForecast: 1);

        Assert.NotNull(result);
        Assert.Single(result.ForecastedValues);
        Assert.Equal(seasonLength, result.SeasonalPattern.SeasonLength);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generates synthetic seasonal time series data for testing.
    /// </summary>
    private static List<decimal> GenerateSeasonalData(int periods, decimal baseLevel, decimal trend, decimal seasonalAmplitude)
    {
        var data = new List<decimal>();
        for (int t = 0; t < periods; t++)
        {
            // Seasonal component (sinusoidal pattern)
            var seasonal = seasonalAmplitude * (decimal)Math.Sin(2 * Math.PI * t / 12.0);
            // Trend component
            var trendComponent = trend * t;
            // Combined value
            var value = baseLevel + trendComponent + seasonal;
            data.Add(Math.Max(0, value)); // Ensure non-negative
        }
        return data;
    }

    #endregion
}
