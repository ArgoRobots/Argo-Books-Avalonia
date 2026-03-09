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
    public void RevenueAccuracyPercent_10PercentOver_ReturnsAbout90()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 1100m,
            ActualRevenue = 1000m
        };

        // SMAPE: error=100, denom=(1100+1000)/2=1050, accuracy=100-100/1050*100≈90.48
        Assert.InRange(record.RevenueAccuracyPercent!.Value, 90, 91);
    }

    [Fact]
    public void RevenueAccuracyPercent_10PercentUnder_ReturnsAbout90()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 900m,
            ActualRevenue = 1000m
        };

        // SMAPE: error=100, denom=(900+1000)/2=950, accuracy=100-100/950*100≈89.47
        Assert.InRange(record.RevenueAccuracyPercent!.Value, 89, 91);
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
    public void RevenueAccuracyPercent_ZeroActual_ReturnsZero()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 1000m,
            ActualRevenue = 0m
        };

        // SMAPE: error=1000, denom=(1000+0)/2=500, accuracy=100-1000/500*100=0
        Assert.Equal(0.0, record.RevenueAccuracyPercent);
    }

    [Fact]
    public void RevenueAccuracyPercent_VeryBadForecast_ReturnsLowOrZero()
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = 5000m,
            ActualRevenue = 1000m
        };

        // SMAPE: error=4000, denom=(5000+1000)/2=3000, accuracy=100-4000/3000*100≈-33 → 0
        Assert.Equal(0.0, record.RevenueAccuracyPercent);
    }

    [Theory]
    [InlineData(1000, 1000, 100, 100)]  // Perfect
    [InlineData(950, 1000, 94, 96)]     // 5% under → SMAPE ≈94.87
    [InlineData(1050, 1000, 94, 96)]    // 5% over → SMAPE ≈95.12
    [InlineData(800, 1000, 77, 79)]     // 20% under → SMAPE ≈77.78
    [InlineData(1200, 1000, 81, 83)]    // 20% over → SMAPE ≈81.82
    [InlineData(500, 1000, 33, 34)]     // 50% under → SMAPE ≈33.33
    public void RevenueAccuracyPercent_VariousScenarios(
        decimal forecasted, decimal actual, double expectedMin, double expectedMax)
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedRevenue = forecasted,
            ActualRevenue = actual
        };

        Assert.InRange(record.RevenueAccuracyPercent!.Value, expectedMin, expectedMax);
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
    [InlineData(500, 500, 100, 100)]   // Perfect
    [InlineData(550, 500, 90, 91)]     // 10% over → SMAPE ≈90.48
    [InlineData(450, 500, 89, 91)]     // 10% under → SMAPE ≈89.47
    [InlineData(600, 500, 81, 83)]     // 20% over → SMAPE ≈81.82
    public void ExpensesAccuracyPercent_VariousScenarios(
        decimal forecasted, decimal actual, double expectedMin, double expectedMax)
    {
        var record = new ForecastAccuracyRecord
        {
            ForecastedExpenses = forecasted,
            ActualExpenses = actual
        };

        Assert.InRange(record.ExpensesAccuracyPercent!.Value, expectedMin, expectedMax);
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
                    ActualRevenue = 1000m, // 100% accuracy
                    ForecastedExpenses = 500m,
                    ActualExpenses = 500m  // 100% accuracy
                },
                new ForecastAccuracyRecord
                {
                    IsValidated = true,
                    ForecastedRevenue = 900m,
                    ActualRevenue = 1000m, // SMAPE ≈89.47%
                    ForecastedExpenses = 550m,
                    ActualExpenses = 500m  // SMAPE ≈90.48%
                }
            ]
        };

        data.CalculateStatistics();

        Assert.Equal(2, data.ValidatedForecastCount);
        Assert.InRange(data.AverageRevenueAccuracy, 94, 96);   // (100 + ~89.47) / 2 ≈ 94.74
        Assert.InRange(data.AverageExpensesAccuracy, 94, 96);   // (100 + ~90.48) / 2 ≈ 95.24
        Assert.Equal(5.0, data.OverallRevenueMAPE);              // MAPE unchanged (0 + 10) / 2
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
