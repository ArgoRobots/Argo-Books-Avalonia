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
    public void Product_DefaultValues_AreCorrect()
    {
        var product = new Product();

        Assert.Equal(string.Empty, product.Id);
        Assert.Equal(string.Empty, product.Name);
        Assert.Equal(string.Empty, product.Sku);
        Assert.Equal(string.Empty, product.Description);
        Assert.Null(product.CategoryId);
        Assert.Equal("Product", product.ItemType);
        Assert.Equal(CategoryType.Sales, product.Type);
        Assert.Equal(0m, product.UnitPrice);
        Assert.Equal(0m, product.CostPrice);
        Assert.Equal(0m, product.TaxRate);
        Assert.False(product.TrackInventory);
        Assert.Null(product.SupplierId);
        Assert.Null(product.ImageUrl);
        Assert.Equal(EntityStatus.Active, product.Status);
    }

    [Fact]
    public void Product_Timestamps_AreSetToUtcNow()
    {
        var before = DateTime.UtcNow;
        var product = new Product();
        var after = DateTime.UtcNow;

        Assert.InRange(product.CreatedAt, before, after);
        Assert.InRange(product.UpdatedAt, before, after);
    }

    #endregion

    #region Pricing Tests

    [Fact]
    public void Product_Pricing_CanBeSet()
    {
        var product = new Product
        {
            UnitPrice = 99.99m,
            CostPrice = 50.00m
        };

        Assert.Equal(99.99m, product.UnitPrice);
        Assert.Equal(50.00m, product.CostPrice);
    }

    [Theory]
    [InlineData(100, 50, 0.5)]   // 50% margin
    [InlineData(100, 75, 0.25)]  // 25% margin
    [InlineData(100, 100, 0)]    // 0% margin
    [InlineData(100, 0, 1)]      // 100% margin
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

        Assert.Equal(0m, product.ProfitMargin);
    }

    [Fact]
    public void Product_NegativeMargin_WhenCostExceedsPrice()
    {
        var product = new Product
        {
            UnitPrice = 50.00m,
            CostPrice = 75.00m
        };

        // (50 - 75) / 50 = -0.5 (50% loss)
        Assert.Equal(-0.5m, product.ProfitMargin);
    }

    #endregion

    #region Tax Rate Tests

    [Fact]
    public void Product_TaxRate_DefaultsToZero()
    {
        var product = new Product();

        Assert.Equal(0m, product.TaxRate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.05)]  // 5%
    [InlineData(0.08)]  // 8%
    [InlineData(0.10)]  // 10%
    [InlineData(0.20)]  // 20%
    public void Product_TaxRate_SupportsCommonRates(decimal rate)
    {
        var product = new Product
        {
            TaxRate = rate
        };

        Assert.Equal(rate, product.TaxRate);
    }

    #endregion

    #region Item Type Tests

    [Theory]
    [InlineData("Product")]
    [InlineData("Service")]
    public void Product_ItemType_SupportsExpectedValues(string itemType)
    {
        var product = new Product
        {
            ItemType = itemType
        };

        Assert.Equal(itemType, product.ItemType);
    }

    [Fact]
    public void Product_ItemType_DefaultsToProduct()
    {
        var product = new Product();

        Assert.Equal("Product", product.ItemType);
    }

    #endregion

    #region Category Type Tests

    [Theory]
    [InlineData(CategoryType.Sales)]
    [InlineData(CategoryType.Expenses)]
    [InlineData(CategoryType.Rental)]
    public void Product_Type_SupportsAllCategoryTypes(CategoryType type)
    {
        var product = new Product
        {
            Type = type
        };

        Assert.Equal(type, product.Type);
    }

    [Fact]
    public void Product_Type_DefaultsToSales()
    {
        var product = new Product();

        Assert.Equal(CategoryType.Sales, product.Type);
    }

    #endregion

    #region Inventory Tracking Tests

    [Fact]
    public void Product_TrackInventory_DefaultsToFalse()
    {
        var product = new Product();

        Assert.False(product.TrackInventory);
    }

    [Fact]
    public void Product_TrackInventory_CanBeEnabled()
    {
        var product = new Product
        {
            TrackInventory = true
        };

        Assert.True(product.TrackInventory);
    }

    [Fact]
    public void Product_Service_ShouldNotTrackInventory()
    {
        var service = new Product
        {
            ItemType = "Service",
            TrackInventory = false
        };

        Assert.Equal("Service", service.ItemType);
        Assert.False(service.TrackInventory);
    }

    #endregion

    #region Status Tests

    [Theory]
    [InlineData(EntityStatus.Active)]
    [InlineData(EntityStatus.Inactive)]
    public void Product_Status_SupportsExpectedValues(EntityStatus status)
    {
        var product = new Product
        {
            Status = status
        };

        Assert.Equal(status, product.Status);
    }

    [Fact]
    public void Product_Status_DefaultsToActive()
    {
        var product = new Product();

        Assert.Equal(EntityStatus.Active, product.Status);
    }

    #endregion

    #region SKU Tests

    [Fact]
    public void Product_Sku_DefaultsToEmpty()
    {
        var product = new Product();

        Assert.Equal(string.Empty, product.Sku);
    }

    [Theory]
    [InlineData("SKU-001")]
    [InlineData("WIDGET-A-2024")]
    [InlineData("123456789")]
    public void Product_Sku_SupportsVariousFormats(string sku)
    {
        var product = new Product
        {
            Sku = sku
        };

        Assert.Equal(sku, product.Sku);
    }

    #endregion

    #region Category Association Tests

    [Fact]
    public void Product_CategoryId_IsOptional()
    {
        var product = new Product
        {
            CategoryId = null
        };

        Assert.Null(product.CategoryId);
    }

    [Fact]
    public void Product_CategoryId_CanBeSet()
    {
        var product = new Product
        {
            CategoryId = "CAT-001"
        };

        Assert.Equal("CAT-001", product.CategoryId);
    }

    #endregion

    #region Supplier Association Tests

    [Fact]
    public void Product_SupplierId_IsOptional()
    {
        var product = new Product
        {
            SupplierId = null
        };

        Assert.Null(product.SupplierId);
    }

    [Fact]
    public void Product_SupplierId_CanBeSet()
    {
        var product = new Product
        {
            SupplierId = "SUP-001"
        };

        Assert.Equal("SUP-001", product.SupplierId);
    }

    #endregion

    #region Image URL Tests

    [Fact]
    public void Product_ImageUrl_IsOptional()
    {
        var product = new Product
        {
            ImageUrl = null
        };

        Assert.Null(product.ImageUrl);
    }

    [Theory]
    [InlineData("https://example.com/images/product.jpg")]
    [InlineData("/assets/images/widget.png")]
    [InlineData("data:image/png;base64,iVBORw0KGgo...")]
    public void Product_ImageUrl_SupportsVariousFormats(string imageUrl)
    {
        var product = new Product
        {
            ImageUrl = imageUrl
        };

        Assert.Equal(imageUrl, product.ImageUrl);
    }

    #endregion

    #region Complete Product Tests

    [Fact]
    public void Product_CompleteProduct_HasAllProperties()
    {
        var product = new Product
        {
            Id = "PRD-001",
            Name = "Premium Widget",
            Sku = "WIDGET-PREM-001",
            Description = "A high-quality premium widget",
            CategoryId = "CAT-ELECTRONICS",
            ItemType = "Product",
            Type = CategoryType.Sales,
            UnitPrice = 149.99m,
            CostPrice = 75.00m,
            TaxRate = 0.08m,
            TrackInventory = true,
            SupplierId = "SUP-ACME",
            ImageUrl = "https://example.com/widget.jpg",
            Status = EntityStatus.Active
        };

        Assert.Equal("PRD-001", product.Id);
        Assert.Equal("Premium Widget", product.Name);
        Assert.Equal("WIDGET-PREM-001", product.Sku);
        Assert.Equal("A high-quality premium widget", product.Description);
        Assert.Equal("CAT-ELECTRONICS", product.CategoryId);
        Assert.Equal("Product", product.ItemType);
        Assert.Equal(CategoryType.Sales, product.Type);
        Assert.Equal(149.99m, product.UnitPrice);
        Assert.Equal(75.00m, product.CostPrice);
        Assert.Equal(0.08m, product.TaxRate);
        Assert.True(product.TrackInventory);
        Assert.Equal("SUP-ACME", product.SupplierId);
        Assert.Equal("https://example.com/widget.jpg", product.ImageUrl);
        Assert.Equal(EntityStatus.Active, product.Status);

        // Verify calculated profit margin: (149.99 - 75) / 149.99 ≈ 0.4999
        Assert.True(product.ProfitMargin > 0.49m && product.ProfitMargin < 0.51m);
    }

    #endregion
}
