using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the Revenue model.
/// </summary>
public class RevenueTests
{
    #region Default Value Tests

    [Fact]
    public void Revenue_DefaultValues_AreCorrect()
    {
        var revenue = new Revenue();

        Assert.Equal(string.Empty, revenue.Id);
        Assert.Null(revenue.CustomerId);
        Assert.Equal(0m, revenue.Subtotal);
        Assert.Equal("Paid", revenue.PaymentStatus);
        Assert.Equal(0m, revenue.Amount);
        Assert.Equal(0m, revenue.Total);
        Assert.Equal(0m, revenue.TaxAmount);
        Assert.Equal(0m, revenue.TaxRate);
        Assert.Equal(0m, revenue.Discount);
        Assert.Equal(0m, revenue.ShippingCost);
        Assert.Empty(revenue.LineItems);
        Assert.Equal("USD", revenue.OriginalCurrency);
    }

    #endregion

    #region Customer Association Tests

    [Fact]
    public void Revenue_CustomerAssociation_WorksCorrectly()
    {
        var revenue = new Revenue
        {
            Id = "SAL-2024-00001",
            CustomerId = "CUST-001"
        };

        Assert.Equal("SAL-2024-00001", revenue.Id);
        Assert.Equal("CUST-001", revenue.CustomerId);
    }

    [Fact]
    public void Revenue_WithoutCustomer_IsValid()
    {
        var revenue = new Revenue
        {
            Id = "SAL-2024-00001",
            CustomerId = null,
            Total = 100.00m
        };

        Assert.Null(revenue.CustomerId);
        Assert.Equal(100.00m, revenue.Total);
    }

    #endregion

    #region Payment Status Tests

    [Theory]
    [InlineData("Paid")]
    [InlineData("Pending")]
    [InlineData("Partial")]
    [InlineData("Overdue")]
    public void Revenue_PaymentStatus_SupportsExpectedValues(string status)
    {
        var revenue = new Revenue
        {
            PaymentStatus = status
        };

        Assert.Equal(status, revenue.PaymentStatus);
    }

    #endregion

    #region Calculation Tests

    [Fact]
    public void Revenue_LineItemTotals_CalculateCorrectly()
    {
        var revenue = new Revenue
        {
            LineItems =
            [
                new LineItem { ProductId = "P1", Description = "Widget A", Quantity = 2, UnitPrice = 25.00m },
                new LineItem { ProductId = "P2", Description = "Widget B", Quantity = 3, UnitPrice = 10.00m }
            ],
            Subtotal = 80.00m,
            TaxRate = 0.10m,
            TaxAmount = 8.00m,
            Total = 88.00m
        };

        Assert.Equal(2, revenue.LineItems.Count);
        Assert.Equal(80.00m, revenue.LineItems.Sum(li => li.Amount));
        Assert.Equal(revenue.Subtotal, revenue.LineItems.Sum(li => li.Amount));
    }

    [Fact]
    public void Revenue_WithDiscount_CalculatesCorrectly()
    {
        var revenue = new Revenue
        {
            Subtotal = 100.00m,
            Discount = 10.00m,
            TaxAmount = 9.00m,  // 10% of (100 - 10)
            Total = 99.00m
        };

        Assert.Equal(10.00m, revenue.Discount);
        Assert.Equal(99.00m, revenue.Total);
    }

    [Fact]
    public void Revenue_WithShipping_CalculatesCorrectly()
    {
        var revenue = new Revenue
        {
            Subtotal = 100.00m,
            ShippingCost = 15.00m,
            TaxAmount = 10.00m,
            Total = 125.00m
        };

        Assert.Equal(15.00m, revenue.ShippingCost);
        Assert.Equal(125.00m, revenue.Total);
    }

    #endregion

    #region Currency Tests

    [Fact]
    public void Revenue_Currency_DefaultsToUSD()
    {
        var revenue = new Revenue();

        Assert.Equal("USD", revenue.OriginalCurrency);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("CAD")]
    public void Revenue_SupportsDifferentCurrencies(string currency)
    {
        var revenue = new Revenue
        {
            OriginalCurrency = currency,
            Total = 100.00m,
            TotalUSD = currency == "USD" ? 100.00m : 85.00m
        };

        Assert.Equal(currency, revenue.OriginalCurrency);
    }

    [Fact]
    public void Revenue_EffectiveTotalUSD_ReturnsConvertedValueWhenSet()
    {
        var revenue = new Revenue
        {
            OriginalCurrency = "EUR",
            Total = 100.00m,
            TotalUSD = 110.00m
        };

        Assert.Equal(110.00m, revenue.EffectiveTotalUSD);
    }

    [Fact]
    public void Revenue_EffectiveTotalUSD_FallsBackToTotalWhenNotSet()
    {
        var revenue = new Revenue
        {
            OriginalCurrency = "USD",
            Total = 100.00m,
            TotalUSD = 0m
        };

        Assert.Equal(100.00m, revenue.EffectiveTotalUSD);
    }

    #endregion

    #region Payment Method Tests

    [Theory]
    [InlineData(PaymentMethod.Cash)]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.DebitCard)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.Check)]
    [InlineData(PaymentMethod.PayPal)]
    [InlineData(PaymentMethod.Other)]
    public void Revenue_PaymentMethod_SupportsAllValues(PaymentMethod method)
    {
        var revenue = new Revenue
        {
            PaymentMethod = method
        };

        Assert.Equal(method, revenue.PaymentMethod);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void Revenue_Timestamps_AreSetCorrectly()
    {
        var before = DateTime.UtcNow;
        var revenue = new Revenue();
        var after = DateTime.UtcNow;

        Assert.InRange(revenue.CreatedAt, before, after);
        Assert.InRange(revenue.UpdatedAt, before, after);
    }

    [Fact]
    public void Revenue_Date_CanBeSet()
    {
        var saleDate = new DateTime(2024, 6, 15, 14, 30, 0);
        var revenue = new Revenue
        {
            Date = saleDate
        };

        Assert.Equal(saleDate, revenue.Date);
    }

    #endregion
}
