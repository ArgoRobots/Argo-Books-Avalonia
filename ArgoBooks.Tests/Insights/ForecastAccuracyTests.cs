using ArgoBooks.Core.Models.Insights;
using Xunit;

namespace ArgoBooks.Tests.Insights;

/// <summary>
/// Tests for ForecastAccuracyRecord and ForecastAccuracyData calculations.
/// </summary>
public class ForecastAccuracyTests
{
    #region RevenueAccuracyPercent Tests

    [Fact]
    public void RevenueAccuracyPercent_PerfectForecast_Returns100()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 1000m,
            ActualRevenue = 1000m
        };

        Assert.Equal(100.0, record.RevenueAccuracyPercent);
    }

    [Fact]
    public void RevenueAccuracyPercent_10PercentOver_Returns90()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 1100m,
            ActualRevenue = 1000m
        };

        // Error = |1100 - 1000| / 1000 = 10%
        // Accuracy = 100 - 10 = 90%
        Assert.Equal(90.0, record.RevenueAccuracyPercent);
    }

    [Fact]
    public void RevenueAccuracyPercent_10PercentUnder_Returns90()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 900m,
            ActualRevenue = 1000m
        };

        Assert.Equal(90.0, record.RevenueAccuracyPercent);
    }

    [Fact]
    public void RevenueAccuracyPercent_NoActualData_ReturnsNull()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 1000m,
            ActualRevenue = null
        };

        Assert.Null(record.RevenueAccuracyPercent);
    }

    [Fact]
    public void RevenueAccuracyPercent_ZeroActual_ReturnsNull()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 1000m,
            ActualRevenue = 0m
        };

        Assert.Null(record.RevenueAccuracyPercent);
    }

    [Fact]
    public void RevenueAccuracyPercent_VeryBadForecast_ReturnsZero()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 5000m,
            ActualRevenue = 1000m
        };

        // Error = |5000 - 1000| / 1000 = 400%
        // Accuracy = Max(0, 100 - 400) = 0%
        Assert.Equal(0.0, record.RevenueAccuracyPercent);
    }

    [Theory]
    [InlineData(1000, 1000, 100)]   // Perfect
    [InlineData(950, 1000, 95)]     // 5% under
    [InlineData(1050, 1000, 95)]    // 5% over
    [InlineData(800, 1000, 80)]     // 20% under
    [InlineData(1200, 1000, 80)]    // 20% over
    [InlineData(500, 1000, 50)]     // 50% under
    public void RevenueAccuracyPercent_VariousScenarios(
        decimal forecasted, decimal actual, double expectedAccuracy)
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = forecasted,
            ActualRevenue = actual
        };

        Assert.Equal(expectedAccuracy, record.RevenueAccuracyPercent);
    }

    #endregion

    #region ExpensesAccuracyPercent Tests

    [Fact]
    public void ExpensesAccuracyPercent_PerfectForecast_Returns100()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedExpenses = 500m,
            ActualExpenses = 500m
        };

        Assert.Equal(100.0, record.ExpensesAccuracyPercent);
    }

    [Fact]
    public void ExpensesAccuracyPercent_NoActualData_ReturnsNull()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedExpenses = 500m,
            ActualExpenses = null
        };

        Assert.Null(record.ExpensesAccuracyPercent);
    }

    [Theory]
    [InlineData(500, 500, 100)]    // Perfect
    [InlineData(550, 500, 90)]     // 10% over
    [InlineData(450, 500, 90)]     // 10% under
    [InlineData(600, 500, 80)]     // 20% over
    public void ExpensesAccuracyPercent_VariousScenarios(
        decimal forecasted, decimal actual, double expectedAccuracy)
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedExpenses = forecasted,
            ActualExpenses = actual
        };

        Assert.Equal(expectedAccuracy, record.ExpensesAccuracyPercent);
    }

    #endregion

    #region RevenueMAPE Tests

    [Fact]
    public void RevenueMAPE_PerfectForecast_ReturnsZero()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 1000m,
            ActualRevenue = 1000m
        };

        Assert.Equal(0.0, record.RevenueMAPE);
    }

    [Fact]
    public void RevenueMAPE_10PercentError_Returns10()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 1100m,
            ActualRevenue = 1000m
        };

        Assert.Equal(10.0, record.RevenueMAPE);
    }

    [Fact]
    public void RevenueMAPE_NoActualData_ReturnsNull()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 1000m,
            ActualRevenue = null
        };

        Assert.Null(record.RevenueMAPE);
    }

    [Theory]
    [InlineData(1000, 1000, 0)]     // Perfect
    [InlineData(1100, 1000, 10)]    // 10% over
    [InlineData(900, 1000, 10)]     // 10% under
    [InlineData(1500, 1000, 50)]    // 50% over
    [InlineData(2000, 1000, 100)]   // 100% over
    public void RevenueMAPE_VariousScenarios(
        decimal forecasted, decimal actual, double expectedMAPE)
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = forecasted,
            ActualRevenue = actual
        };

        Assert.Equal(expectedMAPE, record.RevenueMAPE);
    }

    #endregion

    #region CalculateStatistics Tests

    [Fact]
    public void CalculateStatistics_NoRecords_SetsAppropriateDefaults()
    {
        var data = new ForecastAccuracyData();

        data.CalculateStatistics();

        Assert.Equal(0, data.ValidatedForecastCount);
        Assert.Equal(0, data.TotalForecastCount);
        Assert.Contains("No validated forecasts", data.AccuracyDescription);
    }

    [Fact]
    public void CalculateStatistics_NoValidatedRecords_SetsAppropriateMessage()
    {
        var data = new ForecastAccuracyData
        {
            HistoricalRecords =
            [
                new ForecastAccuracyRecord { IsValidated = false, ForecastedRevenue = 1000 },
                new ForecastAccuracyRecord { IsValidated = false, ForecastedRevenue = 2000 }
            ]
        };

        data.CalculateStatistics();

        Assert.Equal(0, data.ValidatedForecastCount);
        Assert.Equal(2, data.TotalForecastCount);
        Assert.Contains("No validated forecasts", data.AccuracyDescription);
    }

    [Fact]
    public void CalculateStatistics_SingleValidatedRecord_CalculatesAverages()
    {
        var data = new ForecastAccuracyData
        {
            HistoricalRecords =
            [
                new ForecastAccuracyRecord
                {
                    IsValidated = true,
                    ForecastedRevenue = 1000m,
                    ActualRevenue = 1000m,
                    ForecastedExpenses = 500m,
                    ActualExpenses = 500m
                }
            ]
        };

        data.CalculateStatistics();

        Assert.Equal(1, data.ValidatedForecastCount);
        Assert.Equal(100.0, data.AverageRevenueAccuracy);
        Assert.Equal(100.0, data.AverageExpensesAccuracy);
        Assert.Equal(0.0, data.OverallRevenueMAPE);
    }

    [Fact]
    public void CalculateStatistics_MultipleRecords_CalculatesAveragesCorrectly()
    {
        var data = new ForecastAccuracyData
        {
            HistoricalRecords =
            [
                new ForecastAccuracyRecord
                {
                    IsValidated = true,
                    ForecastedRevenue = 1000m,
                    ActualRevenue = 1000m, // 100% accuracy, 0% MAPE
                    ForecastedExpenses = 500m,
                    ActualExpenses = 500m  // 100% accuracy
                },
                new ForecastAccuracyRecord
                {
                    IsValidated = true,
                    ForecastedRevenue = 900m,
                    ActualRevenue = 1000m, // 90% accuracy, 10% MAPE
                    ForecastedExpenses = 550m,
                    ActualExpenses = 500m  // 90% accuracy
                }
            ]
        };

        data.CalculateStatistics();

        Assert.Equal(2, data.ValidatedForecastCount);
        Assert.Equal(95.0, data.AverageRevenueAccuracy);  // (100 + 90) / 2
        Assert.Equal(95.0, data.AverageExpensesAccuracy); // (100 + 90) / 2
        Assert.Equal(5.0, data.OverallRevenueMAPE);       // (0 + 10) / 2
    }

    [Fact]
    public void CalculateStatistics_ExcellentAccuracy_SetsCorrectDescription()
    {
        var data = new ForecastAccuracyData
        {
            HistoricalRecords =
            [
                new ForecastAccuracyRecord
                {
                    IsValidated = true,
                    ForecastedRevenue = 1000m,
                    ActualRevenue = 1000m,
                    ForecastedExpenses = 500m,
                    ActualExpenses = 500m
                }
            ]
        };

        data.CalculateStatistics();

        Assert.Contains("Excellent", data.AccuracyDescription);
    }

    [Fact]
    public void CalculateStatistics_GoodAccuracy_SetsCorrectDescription()
    {
        var data = new ForecastAccuracyData
        {
            HistoricalRecords =
            [
                new ForecastAccuracyRecord
                {
                    IsValidated = true,
                    ForecastedRevenue = 850m,
                    ActualRevenue = 1000m, // 85%
                    ForecastedExpenses = 425m,
                    ActualExpenses = 500m  // 85%
                }
            ]
        };

        data.CalculateStatistics();

        Assert.Contains("Good", data.AccuracyDescription);
    }

    [Fact]
    public void CalculateStatistics_ModerateAccuracy_SetsCorrectDescription()
    {
        var data = new ForecastAccuracyData
        {
            HistoricalRecords =
            [
                new ForecastAccuracyRecord
                {
                    IsValidated = true,
                    ForecastedRevenue = 750m,
                    ActualRevenue = 1000m, // 75%
                    ForecastedExpenses = 375m,
                    ActualExpenses = 500m  // 75%
                }
            ]
        };

        data.CalculateStatistics();

        Assert.Contains("Moderate", data.AccuracyDescription);
    }

    [Fact]
    public void CalculateStatistics_LowAccuracy_SetsCorrectDescription()
    {
        var data = new ForecastAccuracyData
        {
            HistoricalRecords =
            [
                new ForecastAccuracyRecord
                {
                    IsValidated = true,
                    ForecastedRevenue = 500m,
                    ActualRevenue = 1000m, // 50%
                    ForecastedExpenses = 250m,
                    ActualExpenses = 500m  // 50%
                }
            ]
        };

        data.CalculateStatistics();

        Assert.Contains("Low", data.AccuracyDescription);
    }

    #endregion

    #region AccuracyTrend Tests

    [Fact]
    public void CalculateStatistics_ImprovingTrend_DetectsCorrectly()
    {
        var data = new ForecastAccuracyData
        {
            HistoricalRecords =
            [
                // First half: worse accuracy
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 700m, ActualRevenue = 1000m },
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 700m, ActualRevenue = 1000m },
                // Second half: better accuracy
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 950m, ActualRevenue = 1000m },
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 950m, ActualRevenue = 1000m }
            ]
        };

        data.CalculateStatistics();

        Assert.Equal(AccuracyTrend.Improving, data.AccuracyTrend);
    }

    [Fact]
    public void CalculateStatistics_DecliningTrend_DetectsCorrectly()
    {
        var data = new ForecastAccuracyData
        {
            HistoricalRecords =
            [
                // First half: better accuracy
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 950m, ActualRevenue = 1000m },
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 950m, ActualRevenue = 1000m },
                // Second half: worse accuracy
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 700m, ActualRevenue = 1000m },
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 700m, ActualRevenue = 1000m }
            ]
        };

        data.CalculateStatistics();

        Assert.Equal(AccuracyTrend.Declining, data.AccuracyTrend);
    }

    [Fact]
    public void CalculateStatistics_StableTrend_DetectsCorrectly()
    {
        var data = new ForecastAccuracyData
        {
            HistoricalRecords =
            [
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 900m, ActualRevenue = 1000m },
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 900m, ActualRevenue = 1000m },
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 900m, ActualRevenue = 1000m },
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 900m, ActualRevenue = 1000m }
            ]
        };

        data.CalculateStatistics();

        Assert.Equal(AccuracyTrend.Stable, data.AccuracyTrend);
    }

    [Fact]
    public void CalculateStatistics_TooFewRecordsForTrend_StaysStable()
    {
        var data = new ForecastAccuracyData
        {
            HistoricalRecords =
            [
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 1000m, ActualRevenue = 1000m },
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 1000m, ActualRevenue = 1000m }
            ]
        };

        data.CalculateStatistics();

        // With < 4 records, trend calculation is skipped
        Assert.Equal(AccuracyTrend.Stable, data.AccuracyTrend);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void RevenueAccuracyPercent_DecimalPrecision_HandlesCorrectly()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 999.99m,
            ActualRevenue = 1000.00m
        };

        // Error is tiny, accuracy should be very close to 100
        Assert.True(record.RevenueAccuracyPercent > 99.9);
    }

    [Fact]
    public void CalculateStatistics_MixedValidatedStatus_OnlyCountsValidated()
    {
        var data = new ForecastAccuracyData
        {
            HistoricalRecords =
            [
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 1000m, ActualRevenue = 1000m, ForecastedExpenses = 500m, ActualExpenses = 500m },
                new ForecastAccuracyRecord { IsValidated = false, ForecastedRevenue = 500m, ActualRevenue = 1000m, ForecastedExpenses = 250m, ActualExpenses = 500m },
                new ForecastAccuracyRecord { IsValidated = true, ForecastedRevenue = 1000m, ActualRevenue = 1000m, ForecastedExpenses = 500m, ActualExpenses = 500m }
            ]
        };

        data.CalculateStatistics();

        Assert.Equal(2, data.ValidatedForecastCount);
        Assert.Equal(3, data.TotalForecastCount);
        Assert.Equal(100.0, data.AverageRevenueAccuracy); // Only validated records counted
    }

    #endregion
}
