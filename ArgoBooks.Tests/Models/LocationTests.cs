using ArgoBooks.Core.Models.Entities;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the Location model calculations.
/// </summary>
public class LocationTests
{
    #region UtilizationPercentage Tests

    [Fact]
    public void UtilizationPercentage_HalfFull_Returns50()
    {
        var location = new Location
        {
            Capacity = 100,
            CurrentUtilization = 50
        };

        Assert.Equal(50.0, location.UtilizationPercentage);
    }

    [Fact]
    public void UtilizationPercentage_Full_Returns100()
    {
        var location = new Location
        {
            Capacity = 200,
            CurrentUtilization = 200
        };

        Assert.Equal(100.0, location.UtilizationPercentage);
    }

    [Fact]
    public void UtilizationPercentage_Empty_ReturnsZero()
    {
        var location = new Location
        {
            Capacity = 500,
            CurrentUtilization = 0
        };

        Assert.Equal(0.0, location.UtilizationPercentage);
    }

    [Fact]
    public void UtilizationPercentage_ZeroCapacity_ReturnsZero()
    {
        var location = new Location
        {
            Capacity = 0,
            CurrentUtilization = 0
        };

        // Avoid division by zero
        Assert.Equal(0.0, location.UtilizationPercentage);
    }

    [Fact]
    public void UtilizationPercentage_OverCapacity_ReturnsOver100()
    {
        var location = new Location
        {
            Capacity = 100,
            CurrentUtilization = 150
        };

        Assert.Equal(150.0, location.UtilizationPercentage);
    }

    [Theory]
    [InlineData(1000, 250, 25.0)]   // 25%
    [InlineData(1000, 750, 75.0)]   // 75%
    [InlineData(1000, 100, 10.0)]   // 10%
    [InlineData(1000, 900, 90.0)]   // 90%
    [InlineData(1000, 333, 33.3)]   // ~33.3%
    public void UtilizationPercentage_VariousScenarios(int capacity, int utilization, double expectedPercent)
    {
        var location = new Location
        {
            Capacity = capacity,
            CurrentUtilization = utilization
        };

        Assert.Equal(expectedPercent, location.UtilizationPercentage, precision: 1);
    }

    [Fact]
    public void UtilizationPercentage_SmallCapacity_CalculatesCorrectly()
    {
        var location = new Location
        {
            Capacity = 3,
            CurrentUtilization = 1
        };

        // 1/3 = 33.333...%
        Assert.True(location.UtilizationPercentage > 33.3);
        Assert.True(location.UtilizationPercentage < 33.4);
    }

    [Fact]
    public void UtilizationPercentage_LargeNumbers_CalculatesCorrectly()
    {
        var location = new Location
        {
            Capacity = 1_000_000,
            CurrentUtilization = 750_000
        };

        Assert.Equal(75.0, location.UtilizationPercentage);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void Location_HasExpectedDefaults()
    {
        var location = new Location();

        Assert.Equal(0, location.Capacity);
        Assert.Equal(0, location.CurrentUtilization);
        Assert.Equal(0.0, location.UtilizationPercentage);
    }

    #endregion
}
