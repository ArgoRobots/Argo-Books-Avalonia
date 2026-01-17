using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Rentals;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the RentalRecord model calculations.
/// </summary>
public class RentalRecordTests
{
    #region IsOverdue Tests

    [Fact]
    public void IsOverdue_ActiveAndPastDue_ReturnsTrue()
    {
        var rental = new RentalRecord
        {
            Status = RentalStatus.Active,
            DueDate = DateTime.Today.AddDays(-1)
        };

        Assert.True(rental.IsOverdue);
    }

    [Fact]
    public void IsOverdue_ActiveAndDueToday_ReturnsFalse()
    {
        var rental = new RentalRecord
        {
            Status = RentalStatus.Active,
            DueDate = DateTime.Today
        };

        Assert.False(rental.IsOverdue);
    }

    [Fact]
    public void IsOverdue_ActiveAndFutureDue_ReturnsFalse()
    {
        var rental = new RentalRecord
        {
            Status = RentalStatus.Active,
            DueDate = DateTime.Today.AddDays(7)
        };

        Assert.False(rental.IsOverdue);
    }

    [Theory]
    [InlineData(RentalStatus.Returned)]
    [InlineData(RentalStatus.Overdue)]
    [InlineData(RentalStatus.Cancelled)]
    public void IsOverdue_NonActiveStatus_ReturnsFalse(RentalStatus status)
    {
        var rental = new RentalRecord
        {
            Status = status,
            DueDate = DateTime.Today.AddDays(-10)
        };

        Assert.False(rental.IsOverdue);
    }

    #endregion

    #region DaysOverdue Tests

    [Fact]
    public void DaysOverdue_OneDayOverdue_ReturnsOne()
    {
        var rental = new RentalRecord
        {
            Status = RentalStatus.Active,
            DueDate = DateTime.Today.AddDays(-1)
        };

        Assert.Equal(1, rental.DaysOverdue);
    }

    [Fact]
    public void DaysOverdue_TenDaysOverdue_ReturnsTen()
    {
        var rental = new RentalRecord
        {
            Status = RentalStatus.Active,
            DueDate = DateTime.Today.AddDays(-10)
        };

        Assert.Equal(10, rental.DaysOverdue);
    }

    [Fact]
    public void DaysOverdue_NotOverdue_ReturnsZero()
    {
        var rental = new RentalRecord
        {
            Status = RentalStatus.Active,
            DueDate = DateTime.Today.AddDays(5)
        };

        Assert.Equal(0, rental.DaysOverdue);
    }

    [Fact]
    public void DaysOverdue_DueToday_ReturnsZero()
    {
        var rental = new RentalRecord
        {
            Status = RentalStatus.Active,
            DueDate = DateTime.Today
        };

        Assert.Equal(0, rental.DaysOverdue);
    }

    [Fact]
    public void DaysOverdue_ReturnedButPastDue_ReturnsZero()
    {
        var rental = new RentalRecord
        {
            Status = RentalStatus.Returned,
            DueDate = DateTime.Today.AddDays(-30)
        };

        // Not overdue because status is not Active
        Assert.Equal(0, rental.DaysOverdue);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(-5, 5)]
    [InlineData(-30, 30)]
    [InlineData(-100, 100)]
    public void DaysOverdue_VariousPastDueDays_CalculatesCorrectly(int daysFromToday, int expectedDaysOverdue)
    {
        var rental = new RentalRecord
        {
            Status = RentalStatus.Active,
            DueDate = DateTime.Today.AddDays(daysFromToday)
        };

        Assert.Equal(expectedDaysOverdue, rental.DaysOverdue);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void RentalRecord_HasExpectedDefaults()
    {
        var rental = new RentalRecord();

        Assert.Equal(RentalStatus.Active, rental.Status);
        Assert.False(rental.IsOverdue); // Default DueDate is DateTime.MinValue
        Assert.Equal(0, rental.DaysOverdue);
    }

    #endregion
}
