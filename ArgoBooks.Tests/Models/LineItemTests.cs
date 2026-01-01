using ArgoBooks.Core.Models.Common;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the LineItem model calculations.
/// </summary>
public class LineItemTests
{
    #region Subtotal Tests

    [Fact]
    public void Subtotal_QuantityTimesUnitPrice_CalculatesCorrectly()
    {
        var lineItem = new LineItem
        {
            Quantity = 5,
            UnitPrice = 10.00m
        };

        Assert.Equal(50.00m, lineItem.Subtotal);
    }

    [Fact]
    public void Subtotal_WithDiscount_SubtractsDiscount()
    {
        var lineItem = new LineItem
        {
            Quantity = 5,
            UnitPrice = 10.00m,
            Discount = 5.00m
        };

        Assert.Equal(45.00m, lineItem.Subtotal);
    }

    [Fact]
    public void Subtotal_ZeroQuantity_ReturnsZero()
    {
        var lineItem = new LineItem
        {
            Quantity = 0,
            UnitPrice = 10.00m
        };

        Assert.Equal(0m, lineItem.Subtotal);
    }

    [Fact]
    public void Subtotal_ZeroPrice_ReturnsNegativeDiscount()
    {
        var lineItem = new LineItem
        {
            Quantity = 5,
            UnitPrice = 0m,
            Discount = 10.00m
        };

        Assert.Equal(-10.00m, lineItem.Subtotal);
    }

    [Fact]
    public void Subtotal_DecimalValues_CalculatesCorrectly()
    {
        var lineItem = new LineItem
        {
            Quantity = 2.5m,
            UnitPrice = 19.99m,
            Discount = 2.50m
        };

        Assert.Equal(47.475m, lineItem.Subtotal);
    }

    [Fact]
    public void Subtotal_LargeNumbers_CalculatesCorrectly()
    {
        var lineItem = new LineItem
        {
            Quantity = 1000,
            UnitPrice = 999.99m,
            Discount = 500.00m
        };

        Assert.Equal(999490.00m, lineItem.Subtotal);
    }

    #endregion

    #region TaxAmount Tests

    [Fact]
    public void TaxAmount_SubtotalTimesTaxRate_CalculatesCorrectly()
    {
        var lineItem = new LineItem
        {
            Quantity = 10,
            UnitPrice = 100.00m,
            TaxRate = 0.10m
        };

        Assert.Equal(100.00m, lineItem.TaxAmount);
    }

    [Fact]
    public void TaxAmount_ZeroTaxRate_ReturnsZero()
    {
        var lineItem = new LineItem
        {
            Quantity = 10,
            UnitPrice = 100.00m,
            TaxRate = 0m
        };

        Assert.Equal(0m, lineItem.TaxAmount);
    }

    [Fact]
    public void TaxAmount_WithDiscount_CalculatesTaxOnDiscountedSubtotal()
    {
        var lineItem = new LineItem
        {
            Quantity = 10,
            UnitPrice = 100.00m,
            Discount = 100.00m,
            TaxRate = 0.10m
        };

        // Subtotal = (10 * 100) - 100 = 900
        // TaxAmount = 900 * 0.10 = 90
        Assert.Equal(90.00m, lineItem.TaxAmount);
    }

    [Fact]
    public void TaxAmount_StandardUSRate_CalculatesCorrectly()
    {
        var lineItem = new LineItem
        {
            Quantity = 1,
            UnitPrice = 100.00m,
            TaxRate = 0.0825m // 8.25%
        };

        Assert.Equal(8.25m, lineItem.TaxAmount);
    }

    [Fact]
    public void TaxAmount_MaxTaxRate_CalculatesCorrectly()
    {
        var lineItem = new LineItem
        {
            Quantity = 1,
            UnitPrice = 100.00m,
            TaxRate = 1.0m // 100%
        };

        Assert.Equal(100.00m, lineItem.TaxAmount);
    }

    #endregion

    #region Amount Tests

    [Fact]
    public void Amount_SubtotalPlusTax_CalculatesCorrectly()
    {
        var lineItem = new LineItem
        {
            Quantity = 1,
            UnitPrice = 100.00m,
            TaxRate = 0.10m
        };

        Assert.Equal(110.00m, lineItem.Amount);
    }

    [Fact]
    public void Amount_NoTax_EqualsSubtotal()
    {
        var lineItem = new LineItem
        {
            Quantity = 5,
            UnitPrice = 20.00m,
            TaxRate = 0m
        };

        Assert.Equal(100.00m, lineItem.Amount);
        Assert.Equal(lineItem.Subtotal, lineItem.Amount);
    }

    [Fact]
    public void Amount_WithDiscountAndTax_CalculatesCorrectly()
    {
        var lineItem = new LineItem
        {
            Quantity = 10,
            UnitPrice = 50.00m,
            Discount = 50.00m,
            TaxRate = 0.08m
        };

        // Subtotal = (10 * 50) - 50 = 450
        // TaxAmount = 450 * 0.08 = 36
        // Amount = 450 + 36 = 486
        Assert.Equal(486.00m, lineItem.Amount);
    }

    [Fact]
    public void Amount_ComplexCalculation_AllPartsCorrect()
    {
        var lineItem = new LineItem
        {
            Quantity = 3,
            UnitPrice = 29.99m,
            Discount = 10.00m,
            TaxRate = 0.0625m
        };

        var expectedSubtotal = (3 * 29.99m) - 10.00m; // 79.97
        var expectedTax = expectedSubtotal * 0.0625m; // 4.998125
        var expectedAmount = expectedSubtotal + expectedTax; // 84.968125

        Assert.Equal(expectedSubtotal, lineItem.Subtotal);
        Assert.Equal(expectedTax, lineItem.TaxAmount);
        Assert.Equal(expectedAmount, lineItem.Amount);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Calculations_DefaultValues_AllZero()
    {
        var lineItem = new LineItem();

        Assert.Equal(0m, lineItem.Subtotal);
        Assert.Equal(0m, lineItem.TaxAmount);
        Assert.Equal(0m, lineItem.Amount);
    }

    [Fact]
    public void Calculations_NegativeQuantity_CalculatesWithNegative()
    {
        var lineItem = new LineItem
        {
            Quantity = -5,
            UnitPrice = 10.00m,
            TaxRate = 0.10m
        };

        Assert.Equal(-50.00m, lineItem.Subtotal);
        Assert.Equal(-5.00m, lineItem.TaxAmount);
        Assert.Equal(-55.00m, lineItem.Amount);
    }

    [Fact]
    public void Calculations_DiscountGreaterThanSubtotal_NegativeSubtotal()
    {
        var lineItem = new LineItem
        {
            Quantity = 1,
            UnitPrice = 50.00m,
            Discount = 100.00m,
            TaxRate = 0.10m
        };

        Assert.Equal(-50.00m, lineItem.Subtotal);
        Assert.Equal(-5.00m, lineItem.TaxAmount);
        Assert.Equal(-55.00m, lineItem.Amount);
    }

    [Fact]
    public void Calculations_VerySmallValues_HandlesDecimalPrecision()
    {
        var lineItem = new LineItem
        {
            Quantity = 0.001m,
            UnitPrice = 0.01m,
            TaxRate = 0.05m
        };

        Assert.Equal(0.00001m, lineItem.Subtotal);
        Assert.Equal(0.0000005m, lineItem.TaxAmount);
    }

    #endregion
}
