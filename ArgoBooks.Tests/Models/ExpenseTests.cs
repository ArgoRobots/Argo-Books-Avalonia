using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the Expense model.
/// </summary>
public class ExpenseTests
{
    #region Calculation Tests

    [Fact]
    public void Expense_LineItemTotals_CalculateCorrectly()
    {
        var purchase = new Expense
        {
            LineItems =
            [
                new LineItem { ProductId = "P1", Description = "Raw Material A", Quantity = 10, UnitPrice = 50.00m },
                new LineItem { ProductId = "P2", Description = "Raw Material B", Quantity = 5, UnitPrice = 100.00m }
            ],
            Amount = 1000.00m,
            TaxRate = 0.08m,
            TaxAmount = 80.00m,
            Total = 1080.00m
        };

        Assert.Equal(2, purchase.LineItems.Count);
        Assert.Equal(1000.00m, purchase.LineItems.Sum(li => li.Amount));
    }

    [Fact]
    public void Expense_WithShipping_CalculatesCorrectly()
    {
        var purchase = new Expense
        {
            Amount = 500.00m,
            ShippingCost = 25.00m,
            TaxAmount = 40.00m,
            Total = 565.00m
        };

        Assert.Equal(25.00m, purchase.ShippingCost);
        Assert.Equal(565.00m, purchase.Total);
    }

    [Fact]
    public void Expense_WithDiscount_CalculatesCorrectly()
    {
        var purchase = new Expense
        {
            Amount = 1000.00m,
            Discount = 100.00m,  // 10% discount
            TaxAmount = 72.00m,  // Tax on discounted amount
            Total = 972.00m
        };

        Assert.Equal(100.00m, purchase.Discount);
        Assert.Equal(972.00m, purchase.Total);
    }

    #endregion

    #region Currency Tests

    [Fact]
    public void Expense_EffectiveTotalUSD_ReturnsConvertedValueWhenSet()
    {
        var purchase = new Expense
        {
            OriginalCurrency = "EUR",
            Total = 1000.00m,
            TotalUSD = 1100.00m
        };

        Assert.Equal(1100.00m, purchase.EffectiveTotalUSD);
    }

    [Fact]
    public void Expense_EffectiveTotalUSD_FallsBackToTotalWhenNotSet()
    {
        var purchase = new Expense
        {
            Total = 1000.00m,
            TotalUSD = 0m
        };

        Assert.Equal(1000.00m, purchase.EffectiveTotalUSD);
    }

    [Fact]
    public void Expense_EffectiveUnitPriceUSD_FallsBackToUnitPriceWhenNotSet()
    {
        var purchase = new Expense
        {
            UnitPrice = 50.00m,
            UnitPriceUSD = 0m
        };

        Assert.Equal(50.00m, purchase.EffectiveUnitPriceUSD);
    }

    [Fact]
    public void Expense_EffectiveShippingCostUSD_FallsBackToShippingCostWhenNotSet()
    {
        var purchase = new Expense
        {
            ShippingCost = 25.00m,
            ShippingCostUSD = 0m
        };

        Assert.Equal(25.00m, purchase.EffectiveShippingCostUSD);
    }

    #endregion

}
