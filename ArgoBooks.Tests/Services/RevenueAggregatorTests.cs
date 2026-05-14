using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for RevenueAggregator: the paid-only filter and the date-range
/// gross / pre-tax sum helpers. These enforce the cash-basis rule from
/// docs/Calculations.md §2 Rule 2 and §7.
/// </summary>
public class RevenueAggregatorTests
{
    #region IsCollected

    [Theory]
    [InlineData(RevenuePaymentStatus.Paid, true)]
    [InlineData(RevenuePaymentStatus.Complete, true)]
    [InlineData(RevenuePaymentStatus.Partial, false)]
    [InlineData(RevenuePaymentStatus.Pending, false)]
    [InlineData(RevenuePaymentStatus.Unpaid, false)]
    [InlineData(RevenuePaymentStatus.Overdue, false)]
    public void IsCollected_MatchesStandard(RevenuePaymentStatus status, bool expected)
    {
        var revenue = new Revenue { PaymentStatus = status };
        Assert.Equal(expected, RevenueAggregator.IsCollected(revenue));
    }

    #endregion

    #region SumCollectedRevenueUSD

    [Fact]
    public void SumCollectedRevenueUSD_OnlyCountsPaidRows()
    {
        var revenues = new[]
        {
            Revenue(100m, RevenuePaymentStatus.Paid, new DateTime(2026, 5, 1)),
            Revenue(200m, RevenuePaymentStatus.Pending, new DateTime(2026, 5, 1)),
            Revenue(50m, RevenuePaymentStatus.Complete, new DateTime(2026, 5, 1)),
            Revenue(300m, RevenuePaymentStatus.Partial, new DateTime(2026, 5, 1))
        };

        var sum = RevenueAggregator.SumCollectedRevenueUSD(
            revenues, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));

        Assert.Equal(150m, sum);
    }

    [Fact]
    public void SumCollectedRevenueUSD_RespectsDateRange()
    {
        var revenues = new[]
        {
            Revenue(100m, RevenuePaymentStatus.Paid, new DateTime(2026, 4, 30)),
            Revenue(200m, RevenuePaymentStatus.Paid, new DateTime(2026, 5, 15)),
            Revenue(300m, RevenuePaymentStatus.Paid, new DateTime(2026, 6, 1))
        };

        var sum = RevenueAggregator.SumCollectedRevenueUSD(
            revenues, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));

        Assert.Equal(200m, sum);
    }

    [Fact]
    public void SumCollectedRevenueUSD_EmptyRangeReturnsZero()
    {
        var revenues = new[] { Revenue(100m, RevenuePaymentStatus.Paid, new DateTime(2026, 5, 1)) };
        var sum = RevenueAggregator.SumCollectedRevenueUSD(
            revenues, new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));
        Assert.Equal(0m, sum);
    }

    #endregion

    #region SumCollectedRevenuePreTaxUSD

    [Fact]
    public void SumCollectedRevenuePreTaxUSD_StripsTax()
    {
        // Total 119, TaxAmount 32.09 → Subtotal 86.91
        var revenue = new Revenue
        {
            Date = new DateTime(2026, 5, 1),
            PaymentStatus = RevenuePaymentStatus.Paid,
            Total = 119m,
            TaxAmount = 32.09m,
            OriginalCurrency = "USD"
        };

        var sum = RevenueAggregator.SumCollectedRevenuePreTaxUSD(
            new[] { revenue }, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));

        Assert.Equal(86.91m, sum);
    }

    #endregion

    private static Revenue Revenue(decimal totalUSD, RevenuePaymentStatus status, DateTime date) =>
        new()
        {
            Date = date,
            PaymentStatus = status,
            Total = totalUSD,
            TaxAmount = 0m,
            OriginalCurrency = "USD"
        };
}
