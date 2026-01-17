using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Tracking;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the Return model calculations.
/// </summary>
public class ReturnTests
{
    #region NetRefund Tests

    [Fact]
    public void NetRefund_RefundMinusRestockingFee_CalculatesCorrectly()
    {
        var returnItem = new Return
        {
            RefundAmount = 100.00m,
            RestockingFee = 15.00m
        };

        Assert.Equal(85.00m, returnItem.NetRefund);
    }

    [Fact]
    public void NetRefund_NoRestockingFee_EqualsRefundAmount()
    {
        var returnItem = new Return
        {
            RefundAmount = 250.00m,
            RestockingFee = 0m
        };

        Assert.Equal(250.00m, returnItem.NetRefund);
    }

    [Fact]
    public void NetRefund_ZeroRefund_ReturnsNegativeFee()
    {
        var returnItem = new Return
        {
            RefundAmount = 0m,
            RestockingFee = 10.00m
        };

        Assert.Equal(-10.00m, returnItem.NetRefund);
    }

    [Fact]
    public void NetRefund_RestockingFeeExceedsRefund_ReturnsNegative()
    {
        var returnItem = new Return
        {
            RefundAmount = 50.00m,
            RestockingFee = 75.00m
        };

        Assert.Equal(-25.00m, returnItem.NetRefund);
    }

    [Theory]
    [InlineData(100, 10, 90)]      // 10% fee
    [InlineData(100, 15, 85)]      // 15% fee
    [InlineData(100, 20, 80)]      // 20% fee
    [InlineData(500, 50, 450)]     // $50 flat fee
    [InlineData(1000, 150, 850)]   // 15% of large refund
    public void NetRefund_VariousFeeScenarios_CalculatesCorrectly(
        decimal refundAmount, decimal restockingFee, decimal expectedNet)
    {
        var returnItem = new Return
        {
            RefundAmount = refundAmount,
            RestockingFee = restockingFee
        };

        Assert.Equal(expectedNet, returnItem.NetRefund);
    }

    [Fact]
    public void NetRefund_DecimalPrecision_HandlesCorrectly()
    {
        var returnItem = new Return
        {
            RefundAmount = 99.99m,
            RestockingFee = 14.99m
        };

        Assert.Equal(85.00m, returnItem.NetRefund);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void Return_HasExpectedDefaults()
    {
        var returnItem = new Return();

        Assert.Equal(ReturnStatus.Pending, returnItem.Status);
        Assert.Equal("Customer", returnItem.ReturnType);
        Assert.Equal(0m, returnItem.RefundAmount);
        Assert.Equal(0m, returnItem.RestockingFee);
        Assert.Equal(0m, returnItem.NetRefund);
    }

    #endregion
}
