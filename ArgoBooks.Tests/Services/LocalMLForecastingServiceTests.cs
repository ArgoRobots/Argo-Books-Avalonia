using ArgoBooks.Core.Models.Insights;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the LocalMLForecastingService class.
/// </summary>
public class LocalMLForecastingServiceTests
{
    private readonly LocalMLForecastingService _service = new();

    #region GenerateEnhancedForecast Tests

    [Fact]
    public void GenerateEnhancedForecast_InsufficientData_ReturnsSingleValue()
    {
        var data = new List<decimal> { 100m };

        var result = _service.GenerateEnhancedForecast(data);

        Assert.Equal("Insufficient Data", result.MethodUsed);
        Assert.Equal(0, result.ConfidenceScore);
        Assert.Single(result.ForecastedValues);
        Assert.Equal(100m, result.ForecastedValues[0]);
    }

    [Fact]
    public void GenerateEnhancedForecast_EmptyData_ReturnsDefault()
    {
        var data = new List<decimal>();

        var result = _service.GenerateEnhancedForecast(data);

        Assert.Equal("Insufficient Data", result.MethodUsed);
        Assert.Single(result.ForecastedValues);
        Assert.Equal(0m, result.ForecastedValues[0]);
    }

    [Fact]
    public void GenerateEnhancedForecast_SufficientData_ReturnsForecasts()
    {
        var data = new List<decimal>
        {
            100m, 110m, 105m, 115m, 120m, 125m,
            130m, 135m, 140m, 145m, 150m, 155m
        };

        var result = _service.GenerateEnhancedForecast(data);

        Assert.NotNull(result);
        Assert.NotEmpty(result.ForecastedValues);
        Assert.Equal(12, result.DataPointsUsed);
        Assert.Equal(1, result.PeriodsForecasted);
    }

    [Fact]
    public void GenerateEnhancedForecast_MultiplePeriodsForecasted()
    {
        var data = new List<decimal>
        {
            100m, 110m, 105m, 115m, 120m, 125m,
            130m, 135m, 140m, 145m, 150m, 155m
        };

        var result = _service.GenerateEnhancedForecast(data, 3);

        Assert.Equal(3, result.PeriodsForecasted);
        Assert.Equal(3, result.ForecastedValues.Count);
    }

    [Fact]
    public void GenerateEnhancedForecast_AutoMethod_UsesHoltWintersForSmallData()
    {
        var data = new List<decimal>
        {
            100m, 110m, 105m, 115m, 120m, 125m,
            130m, 135m, 140m, 145m, 150m, 155m
        };

        var result = _service.GenerateEnhancedForecast(data);

        Assert.Contains("Holt-Winters", result.MethodUsed);
    }

    [Fact]
    public void GenerateEnhancedForecast_PreferredHoltWinters_UsesHoltWinters()
    {
        var data = new List<decimal>
        {
            100m, 110m, 105m, 115m, 120m, 125m,
            130m, 135m, 140m, 145m, 150m, 155m
        };

        var result = _service.GenerateEnhancedForecast(data, 1, ForecastMethod.HoltWinters);

        Assert.Contains("Holt-Winters", result.MethodUsed);
    }

    #endregion

    #region DetectSeasonality Tests

    [Fact]
    public void DetectSeasonality_InsufficientData_ReturnsZeroStrength()
    {
        var data = new List<decimal> { 100m, 200m, 300m };

        var result = _service.DetectSeasonality(data);

        Assert.Equal(0, result.SeasonalStrength);
        Assert.Contains("Insufficient", result.Description);
    }

    [Fact]
    public void DetectSeasonality_WithSeasonalData_DetectsPattern()
    {
        // Create data with a clear seasonal pattern (12-month cycle)
        var data = new List<decimal>();
        for (var i = 0; i < 24; i++)
        {
            data.Add(100m + 50m * (decimal)Math.Sin(2 * Math.PI * i / 12));
        }

        var result = _service.DetectSeasonality(data);

        Assert.NotNull(result);
    }

    #endregion

    #region CalculateConfidenceScore Tests

    [Fact]
    public void CalculateConfidenceScore_FewDataPoints_LowScore()
    {
        var data = new List<decimal> { 100m, 110m };

        var score = _service.CalculateConfidenceScore(data, null, null);

        Assert.True(score < 50);
    }

    [Fact]
    public void CalculateConfidenceScore_ManyDataPoints_HigherScore()
    {
        var data = Enumerable.Range(0, 30).Select(i => 100m + i).ToList();

        var score = _service.CalculateConfidenceScore(data, null, null);

        Assert.True(score > 20);
    }

    [Fact]
    public void CalculateConfidenceScore_WithSeasonalPattern_IncludesSeasonalBonus()
    {
        var data = Enumerable.Range(0, 15).Select(_ => 100m).ToList();
        var pattern = new SeasonalPattern { SeasonalStrength = 0.8 };

        var scoreWithPattern = _service.CalculateConfidenceScore(data, pattern, null);
        var scoreWithoutPattern = _service.CalculateConfidenceScore(data, null, null);

        Assert.True(scoreWithPattern > scoreWithoutPattern);
    }

    [Fact]
    public void CalculateConfidenceScore_WithHistoricalAccuracy_IncludesBonus()
    {
        var data = Enumerable.Range(0, 15).Select(_ => 100m).ToList();

        var scoreWithAccuracy = _service.CalculateConfidenceScore(data, null, 80.0);
        var scoreWithoutAccuracy = _service.CalculateConfidenceScore(data, null, null);

        Assert.True(scoreWithAccuracy > scoreWithoutAccuracy);
    }

    [Fact]
    public void CalculateConfidenceScore_ReturnsMaximum100()
    {
        var data = Enumerable.Range(0, 100).Select(_ => 100m).ToList();
        var pattern = new SeasonalPattern { SeasonalStrength = 1.0 };

        var score = _service.CalculateConfidenceScore(data, pattern, 100.0);

        Assert.True(score <= 100);
    }

    [Fact]
    public void CalculateConfidenceScore_ReturnsMinimum0()
    {
        var data = new List<decimal> { 100m };

        var score = _service.CalculateConfidenceScore(data, null, null);

        Assert.True(score >= 0);
    }

    #endregion

    #region EnhancedForecastResult Tests

    [Fact]
    public void EnhancedForecastResult_ForecastedValue_ReturnsFirst()
    {
        var result = new EnhancedForecastResult
        {
            ForecastedValues = new List<decimal> { 100m, 200m, 300m }
        };

        Assert.Equal(100m, result.ForecastedValue);
    }

    [Fact]
    public void EnhancedForecastResult_ForecastedValue_EmptyList_ReturnsDefault()
    {
        var result = new EnhancedForecastResult();

        Assert.Equal(0m, result.ForecastedValue);
    }

    [Theory]
    [InlineData(80, "High")]
    [InlineData(90, "High")]
    [InlineData(50, "Medium")]
    [InlineData(79, "Medium")]
    [InlineData(0, "Low")]
    [InlineData(49, "Low")]
    public void EnhancedForecastResult_ConfidenceLevel_ReturnsCorrectDescription(
        double score, string expected)
    {
        var result = new EnhancedForecastResult { ConfidenceScore = score };

        Assert.Equal(expected, result.ConfidenceLevel);
    }

    #endregion

}
