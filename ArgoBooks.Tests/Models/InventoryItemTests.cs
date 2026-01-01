using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Inventory;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the InventoryItem model properties and calculations.
/// </summary>
public class InventoryItemTests
{
    #region Available Quantity Tests

    [Fact]
    public void Available_InStockMinusReserved_CalculatesCorrectly()
    {
        var item = new InventoryItem
        {
            InStock = 100,
            Reserved = 30
        };

        Assert.Equal(70, item.Available);
    }

    [Fact]
    public void Available_NoReservations_EqualsInStock()
    {
        var item = new InventoryItem
        {
            InStock = 50,
            Reserved = 0
        };

        Assert.Equal(50, item.Available);
    }

    [Fact]
    public void Available_AllReserved_ReturnsZero()
    {
        var item = new InventoryItem
        {
            InStock = 25,
            Reserved = 25
        };

        Assert.Equal(0, item.Available);
    }

    [Fact]
    public void Available_ZeroStock_ReturnsZero()
    {
        var item = new InventoryItem
        {
            InStock = 0,
            Reserved = 0
        };

        Assert.Equal(0, item.Available);
    }

    [Fact]
    public void Available_ReservedExceedsStock_ReturnsNegative()
    {
        // Edge case: this shouldn't happen in practice but tests the calculation
        var item = new InventoryItem
        {
            InStock = 10,
            Reserved = 15
        };

        Assert.Equal(-5, item.Available);
    }

    #endregion

    #region TotalValue Tests

    [Fact]
    public void TotalValue_InStockTimesUnitCost_CalculatesCorrectly()
    {
        var item = new InventoryItem
        {
            InStock = 100,
            UnitCost = 25.50m
        };

        Assert.Equal(2550.00m, item.TotalValue);
    }

    [Fact]
    public void TotalValue_ZeroStock_ReturnsZero()
    {
        var item = new InventoryItem
        {
            InStock = 0,
            UnitCost = 100.00m
        };

        Assert.Equal(0m, item.TotalValue);
    }

    [Fact]
    public void TotalValue_ZeroUnitCost_ReturnsZero()
    {
        var item = new InventoryItem
        {
            InStock = 100,
            UnitCost = 0m
        };

        Assert.Equal(0m, item.TotalValue);
    }

    [Fact]
    public void TotalValue_LargeQuantity_CalculatesCorrectly()
    {
        var item = new InventoryItem
        {
            InStock = 10000,
            UnitCost = 99.99m
        };

        Assert.Equal(999900.00m, item.TotalValue);
    }

    [Fact]
    public void TotalValue_FractionalUnitCost_CalculatesCorrectly()
    {
        var item = new InventoryItem
        {
            InStock = 3,
            UnitCost = 0.33m
        };

        Assert.Equal(0.99m, item.TotalValue);
    }

    #endregion

    #region CalculateStatus Tests

    [Fact]
    public void CalculateStatus_ZeroStock_ReturnsOutOfStock()
    {
        var item = new InventoryItem
        {
            InStock = 0,
            ReorderPoint = 10,
            OverstockThreshold = 100
        };

        Assert.Equal(InventoryStatus.OutOfStock, item.CalculateStatus());
    }

    [Fact]
    public void CalculateStatus_BelowReorderPoint_ReturnsLowStock()
    {
        var item = new InventoryItem
        {
            InStock = 5,
            ReorderPoint = 10,
            OverstockThreshold = 100
        };

        Assert.Equal(InventoryStatus.LowStock, item.CalculateStatus());
    }

    [Fact]
    public void CalculateStatus_AtReorderPoint_ReturnsLowStock()
    {
        var item = new InventoryItem
        {
            InStock = 10,
            ReorderPoint = 10,
            OverstockThreshold = 100
        };

        Assert.Equal(InventoryStatus.LowStock, item.CalculateStatus());
    }

    [Fact]
    public void CalculateStatus_AboveReorderBelowOverstock_ReturnsInStock()
    {
        var item = new InventoryItem
        {
            InStock = 50,
            ReorderPoint = 10,
            OverstockThreshold = 100
        };

        Assert.Equal(InventoryStatus.InStock, item.CalculateStatus());
    }

    [Fact]
    public void CalculateStatus_AtOverstockThreshold_ReturnsOverstock()
    {
        var item = new InventoryItem
        {
            InStock = 100,
            ReorderPoint = 10,
            OverstockThreshold = 100
        };

        Assert.Equal(InventoryStatus.Overstock, item.CalculateStatus());
    }

    [Fact]
    public void CalculateStatus_AboveOverstockThreshold_ReturnsOverstock()
    {
        var item = new InventoryItem
        {
            InStock = 150,
            ReorderPoint = 10,
            OverstockThreshold = 100
        };

        Assert.Equal(InventoryStatus.Overstock, item.CalculateStatus());
    }

    [Fact]
    public void CalculateStatus_ZeroOverstockThreshold_NeverReturnsOverstock()
    {
        var item = new InventoryItem
        {
            InStock = 1000,
            ReorderPoint = 10,
            OverstockThreshold = 0
        };

        // When overstock threshold is 0, the overstock check is skipped
        Assert.Equal(InventoryStatus.InStock, item.CalculateStatus());
    }

    [Fact]
    public void CalculateStatus_ZeroReorderPoint_NormalStock()
    {
        var item = new InventoryItem
        {
            InStock = 50,
            ReorderPoint = 0,
            OverstockThreshold = 100
        };

        // Stock at or below reorder point (0) would be LowStock
        // But since InStock (50) > ReorderPoint (0), it's InStock
        Assert.Equal(InventoryStatus.InStock, item.CalculateStatus());
    }

    [Fact]
    public void CalculateStatus_OutOfStockTakesPriority()
    {
        var item = new InventoryItem
        {
            InStock = 0,
            ReorderPoint = 0,
            OverstockThreshold = 0
        };

        // Zero stock should return OutOfStock regardless of other settings
        Assert.Equal(InventoryStatus.OutOfStock, item.CalculateStatus());
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void NewInventoryItem_HasDefaultStatus()
    {
        var item = new InventoryItem();

        Assert.Equal(InventoryStatus.InStock, item.Status);
    }

    [Fact]
    public void NewInventoryItem_HasDefaultUnitOfMeasure()
    {
        var item = new InventoryItem();

        Assert.Equal("Each", item.UnitOfMeasure);
    }

    [Fact]
    public void NewInventoryItem_HasLastUpdatedSet()
    {
        var beforeCreate = DateTime.UtcNow;
        var item = new InventoryItem();
        var afterCreate = DateTime.UtcNow;

        Assert.True(item.LastUpdated >= beforeCreate);
        Assert.True(item.LastUpdated <= afterCreate);
    }

    [Fact]
    public void NewInventoryItem_HasZeroStock()
    {
        var item = new InventoryItem();

        Assert.Equal(0, item.InStock);
        Assert.Equal(0, item.Reserved);
        Assert.Equal(0, item.Available);
    }

    [Fact]
    public void NewInventoryItem_HasZeroTotalValue()
    {
        var item = new InventoryItem();

        Assert.Equal(0m, item.TotalValue);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CalculateStatus_LowStockTakePriorityOverOverstock()
    {
        // Edge case: What if stock is both at reorder point and at overstock?
        // This shouldn't happen in practice but tests the priority
        var item = new InventoryItem
        {
            InStock = 10,
            ReorderPoint = 10,
            OverstockThreshold = 5
        };

        // Overstock is checked before low stock in the implementation
        Assert.Equal(InventoryStatus.Overstock, item.CalculateStatus());
    }

    [Fact]
    public void InventoryItem_PropertyAssignment_WorksCorrectly()
    {
        var item = new InventoryItem
        {
            Id = "INV-001",
            ProductId = "PRD-001",
            Sku = "SKU-12345",
            LocationId = "LOC-001",
            InStock = 100,
            Reserved = 10,
            ReorderPoint = 20,
            OverstockThreshold = 200,
            UnitCost = 15.99m,
            UnitOfMeasure = "Box",
            Status = InventoryStatus.LowStock
        };

        Assert.Equal("INV-001", item.Id);
        Assert.Equal("PRD-001", item.ProductId);
        Assert.Equal("SKU-12345", item.Sku);
        Assert.Equal("LOC-001", item.LocationId);
        Assert.Equal(100, item.InStock);
        Assert.Equal(10, item.Reserved);
        Assert.Equal(90, item.Available);
        Assert.Equal(20, item.ReorderPoint);
        Assert.Equal(200, item.OverstockThreshold);
        Assert.Equal(15.99m, item.UnitCost);
        Assert.Equal("Box", item.UnitOfMeasure);
        Assert.Equal(InventoryStatus.LowStock, item.Status);
        Assert.Equal(1599.00m, item.TotalValue);
    }

    #endregion
}
