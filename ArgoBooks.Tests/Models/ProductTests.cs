using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the Product model.
/// </summary>
public class ProductTests
{
    #region Default Value Tests

    [Fact]
    public void Product_HasExpectedDefaults()
    {
        var product = new Product();

        Assert.Equal(string.Empty, product.Id);
        Assert.Equal(string.Empty, product.Name);
        Assert.Equal(string.Empty, product.Sku);
        Assert.Equal("Product", product.ItemType);
        Assert.Equal(CategoryType.Revenue, product.Type);
        Assert.Equal(0m, product.UnitPrice);
        Assert.Equal(0m, product.CostPrice);
        Assert.Equal(0m, product.TaxRate);
        Assert.False(product.TrackInventory);
        Assert.Null(product.CategoryId);
        Assert.Null(product.SupplierId);
        Assert.Equal(EntityStatus.Active, product.Status);
    }

    #endregion

    #region Profit Margin Tests

    [Theory]
    [InlineData(100, 50, 0.5)]    // 50% margin
    [InlineData(100, 75, 0.25)]   // 25% margin
    [InlineData(100, 100, 0)]     // 0% margin (break even)
    [InlineData(100, 0, 1)]       // 100% margin (no cost)
    [InlineData(50, 75, -0.5)]    // -50% margin (loss)
    public void Product_ProfitMargin_CalculatesCorrectly(decimal unitPrice, decimal costPrice, decimal expectedMargin)
    {
        var product = new Product
        {
            UnitPrice = unitPrice,
            CostPrice = costPrice
        };

        Assert.Equal(expectedMargin, product.ProfitMargin);
    }

    [Fact]
    public void Product_ProfitMargin_ReturnsZeroWhenUnitPriceIsZero()
    {
        var product = new Product
        {
            UnitPrice = 0m,
            CostPrice = 50.00m
        };

        // Avoid division by zero
        Assert.Equal(0m, product.ProfitMargin);
    }

    #endregion
}
