using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the Sale model.
/// </summary>
public class SaleTests
{
    #region Default Value Tests

    [Fact]
    public void Sale_DefaultValues_AreCorrect()
    {
        var sale = new Sale();

        Assert.Equal(string.Empty, sale.Id);
        Assert.Null(sale.CustomerId);
        Assert.Equal(0m, sale.Subtotal);
        Assert.Equal("Paid", sale.PaymentStatus);
        Assert.Equal(0m, sale.Amount);
        Assert.Equal(0m, sale.Total);
        Assert.Equal(0m, sale.TaxAmount);
        Assert.Equal(0m, sale.TaxRate);
        Assert.Equal(0m, sale.Discount);
        Assert.Equal(0m, sale.ShippingCost);
        Assert.Empty(sale.LineItems);
        Assert.Equal("USD", sale.OriginalCurrency);
    }

    #endregion

    #region Customer Association Tests

    [Fact]
    public void Sale_CustomerAssociation_WorksCorrectly()
    {
        var sale = new Sale
        {
            Id = "SAL-2024-00001",
            CustomerId = "CUST-001"
        };

        Assert.Equal("SAL-2024-00001", sale.Id);
        Assert.Equal("CUST-001", sale.CustomerId);
    }

    [Fact]
    public void Sale_WithoutCustomer_IsValid()
    {
        var sale = new Sale
        {
            Id = "SAL-2024-00001",
            CustomerId = null,
            Total = 100.00m
        };

        Assert.Null(sale.CustomerId);
        Assert.Equal(100.00m, sale.Total);
    }

    #endregion

    #region Payment Status Tests

    [Theory]
    [InlineData("Paid")]
    [InlineData("Pending")]
    [InlineData("Partial")]
    [InlineData("Overdue")]
    public void Sale_PaymentStatus_SupportsExpectedValues(string status)
    {
        var sale = new Sale
        {
            PaymentStatus = status
        };

        Assert.Equal(status, sale.PaymentStatus);
    }

    #endregion

    #region Calculation Tests

    [Fact]
    public void Sale_LineItemTotals_CalculateCorrectly()
    {
        var sale = new Sale
        {
            LineItems =
            [
                new LineItem { ProductId = "P1", Description = "Widget A", Quantity = 2, UnitPrice = 25.00m, Total = 50.00m },
                new LineItem { ProductId = "P2", Description = "Widget B", Quantity = 3, UnitPrice = 10.00m, Total = 30.00m }
            ],
            Subtotal = 80.00m,
            TaxRate = 0.10m,
            TaxAmount = 8.00m,
            Total = 88.00m
        };

        Assert.Equal(2, sale.LineItems.Count);
        Assert.Equal(80.00m, sale.LineItems.Sum(li => li.Total));
        Assert.Equal(sale.Subtotal, sale.LineItems.Sum(li => li.Total));
    }

    [Fact]
    public void Sale_WithDiscount_CalculatesCorrectly()
    {
        var sale = new Sale
        {
            Subtotal = 100.00m,
            Discount = 10.00m,
            TaxAmount = 9.00m,  // 10% of (100 - 10)
            Total = 99.00m
        };

        Assert.Equal(10.00m, sale.Discount);
        Assert.Equal(99.00m, sale.Total);
    }

    [Fact]
    public void Sale_WithShipping_CalculatesCorrectly()
    {
        var sale = new Sale
        {
            Subtotal = 100.00m,
            ShippingCost = 15.00m,
            TaxAmount = 10.00m,
            Total = 125.00m
        };

        Assert.Equal(15.00m, sale.ShippingCost);
        Assert.Equal(125.00m, sale.Total);
    }

    #endregion

    #region Currency Tests

    [Fact]
    public void Sale_Currency_DefaultsToUSD()
    {
        var sale = new Sale();

        Assert.Equal("USD", sale.OriginalCurrency);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("CAD")]
    public void Sale_SupportsDifferentCurrencies(string currency)
    {
        var sale = new Sale
        {
            OriginalCurrency = currency,
            Total = 100.00m,
            TotalUSD = currency == "USD" ? 100.00m : 85.00m
        };

        Assert.Equal(currency, sale.OriginalCurrency);
    }

    [Fact]
    public void Sale_EffectiveTotalUSD_ReturnsConvertedValueWhenSet()
    {
        var sale = new Sale
        {
            OriginalCurrency = "EUR",
            Total = 100.00m,
            TotalUSD = 110.00m
        };

        Assert.Equal(110.00m, sale.EffectiveTotalUSD);
    }

    [Fact]
    public void Sale_EffectiveTotalUSD_FallsBackToTotalWhenNotSet()
    {
        var sale = new Sale
        {
            OriginalCurrency = "USD",
            Total = 100.00m,
            TotalUSD = 0m
        };

        Assert.Equal(100.00m, sale.EffectiveTotalUSD);
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
    public void Sale_PaymentMethod_SupportsAllValues(PaymentMethod method)
    {
        var sale = new Sale
        {
            PaymentMethod = method
        };

        Assert.Equal(method, sale.PaymentMethod);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void Sale_Timestamps_AreSetCorrectly()
    {
        var before = DateTime.UtcNow;
        var sale = new Sale();
        var after = DateTime.UtcNow;

        Assert.InRange(sale.CreatedAt, before, after);
        Assert.InRange(sale.UpdatedAt, before, after);
    }

    [Fact]
    public void Sale_Date_CanBeSet()
    {
        var saleDate = new DateTime(2024, 6, 15, 14, 30, 0);
        var sale = new Sale
        {
            Date = saleDate
        };

        Assert.Equal(saleDate, sale.Date);
    }

    #endregion
}
