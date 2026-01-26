using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the Expense model.
/// </summary>
public class ExpenseTests
{
    #region Default Value Tests

    [Fact]
    public void Expense_DefaultValues_AreCorrect()
    {
        var purchase = new Expense();

        Assert.Equal(string.Empty, purchase.Id);
        Assert.Null(purchase.SupplierId);
        Assert.Equal(0m, purchase.Amount);
        Assert.Equal(0m, purchase.Total);
        Assert.Equal(0m, purchase.TaxAmount);
        Assert.Equal(0m, purchase.TaxRate);
        Assert.Equal(0m, purchase.Discount);
        Assert.Equal(0m, purchase.ShippingCost);
        Assert.Empty(purchase.LineItems);
        Assert.Equal("USD", purchase.OriginalCurrency);
    }

    #endregion

    #region Supplier Association Tests

    [Fact]
    public void Expense_SupplierAssociation_WorksCorrectly()
    {
        var purchase = new Expense
        {
            Id = "PUR-2024-00001",
            SupplierId = "SUP-001"
        };

        Assert.Equal("PUR-2024-00001", purchase.Id);
        Assert.Equal("SUP-001", purchase.SupplierId);
    }

    [Fact]
    public void Expense_WithoutSupplier_IsValid()
    {
        var purchase = new Expense
        {
            Id = "PUR-2024-00001",
            SupplierId = null,
            Total = 500.00m
        };

        Assert.Null(purchase.SupplierId);
        Assert.Equal(500.00m, purchase.Total);
    }

    #endregion

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
    public void Expense_Currency_DefaultsToUSD()
    {
        var purchase = new Expense();

        Assert.Equal("USD", purchase.OriginalCurrency);
    }

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

    #region Receipt Association Tests

    [Fact]
    public void Expense_ReceiptAssociation_WorksCorrectly()
    {
        var purchase = new Expense
        {
            Id = "PUR-2024-00001",
            ReceiptId = "RCP-001"
        };

        Assert.Equal("RCP-001", purchase.ReceiptId);
    }

    [Fact]
    public void Expense_WithoutReceipt_IsValid()
    {
        var purchase = new Expense
        {
            Id = "PUR-2024-00001",
            ReceiptId = null
        };

        Assert.Null(purchase.ReceiptId);
    }

    #endregion

    #region Reference Number Tests

    [Fact]
    public void Expense_ReferenceNumber_WorksCorrectly()
    {
        var purchase = new Expense
        {
            ReferenceNumber = "INV-SUP-2024-001"
        };

        Assert.Equal("INV-SUP-2024-001", purchase.ReferenceNumber);
    }

    [Theory]
    [InlineData("PO-12345")]
    [InlineData("VENDOR-INV-001")]
    [InlineData("2024/Q1/001")]
    public void Expense_ReferenceNumber_SupportsVariousFormats(string refNumber)
    {
        var purchase = new Expense
        {
            ReferenceNumber = refNumber
        };

        Assert.Equal(refNumber, purchase.ReferenceNumber);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void Expense_Timestamps_AreSetCorrectly()
    {
        var before = DateTime.UtcNow;
        var purchase = new Expense();
        var after = DateTime.UtcNow;

        Assert.InRange(purchase.CreatedAt, before, after);
        Assert.InRange(purchase.UpdatedAt, before, after);
    }

    #endregion
}
