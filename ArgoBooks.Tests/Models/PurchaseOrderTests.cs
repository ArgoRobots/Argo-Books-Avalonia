using ArgoBooks.Core.Models.Inventory;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for PurchaseOrder and PurchaseOrderLineItem calculations.
/// </summary>
public class PurchaseOrderTests
{
    #region PurchaseOrderLineItem.Total Tests

    [Fact]
    public void LineItem_Total_QuantityTimesUnitCost()
    {
        var lineItem = new PurchaseOrderLineItem
        {
            Quantity = 10,
            UnitCost = 25.00m
        };

        Assert.Equal(250.00m, lineItem.Total);
    }

    [Fact]
    public void LineItem_Total_ZeroQuantity_ReturnsZero()
    {
        var lineItem = new PurchaseOrderLineItem
        {
            Quantity = 0,
            UnitCost = 100.00m
        };

        Assert.Equal(0m, lineItem.Total);
    }

    [Fact]
    public void LineItem_Total_ZeroUnitCost_ReturnsZero()
    {
        var lineItem = new PurchaseOrderLineItem
        {
            Quantity = 50,
            UnitCost = 0m
        };

        Assert.Equal(0m, lineItem.Total);
    }

    [Theory]
    [InlineData(1, 99.99, 99.99)]
    [InlineData(100, 0.50, 50.00)]
    [InlineData(1000, 1.25, 1250.00)]
    [InlineData(5, 199.99, 999.95)]
    public void LineItem_Total_VariousScenarios(int quantity, decimal unitCost, decimal expectedTotal)
    {
        var lineItem = new PurchaseOrderLineItem
        {
            Quantity = quantity,
            UnitCost = unitCost
        };

        Assert.Equal(expectedTotal, lineItem.Total);
    }

    #endregion

    #region PurchaseOrderLineItem.IsFullyReceived Tests

    [Fact]
    public void LineItem_IsFullyReceived_ReceivedEqualsOrdered_ReturnsTrue()
    {
        var lineItem = new PurchaseOrderLineItem
        {
            Quantity = 50,
            QuantityReceived = 50
        };

        Assert.True(lineItem.IsFullyReceived);
    }

    [Fact]
    public void LineItem_IsFullyReceived_ReceivedExceedsOrdered_ReturnsTrue()
    {
        var lineItem = new PurchaseOrderLineItem
        {
            Quantity = 50,
            QuantityReceived = 55
        };

        Assert.True(lineItem.IsFullyReceived);
    }

    [Fact]
    public void LineItem_IsFullyReceived_PartiallyReceived_ReturnsFalse()
    {
        var lineItem = new PurchaseOrderLineItem
        {
            Quantity = 100,
            QuantityReceived = 75
        };

        Assert.False(lineItem.IsFullyReceived);
    }

    [Fact]
    public void LineItem_IsFullyReceived_NoneReceived_ReturnsFalse()
    {
        var lineItem = new PurchaseOrderLineItem
        {
            Quantity = 25,
            QuantityReceived = 0
        };

        Assert.False(lineItem.IsFullyReceived);
    }

    [Fact]
    public void LineItem_IsFullyReceived_ZeroOrdered_ReturnsTrue()
    {
        var lineItem = new PurchaseOrderLineItem
        {
            Quantity = 0,
            QuantityReceived = 0
        };

        Assert.True(lineItem.IsFullyReceived);
    }

    #endregion

    #region PurchaseOrder.IsFullyReceived Tests

    [Fact]
    public void PurchaseOrder_IsFullyReceived_AllLineItemsReceived_ReturnsTrue()
    {
        var po = new PurchaseOrder
        {
            LineItems =
            [
                new PurchaseOrderLineItem { Quantity = 10, QuantityReceived = 10 },
                new PurchaseOrderLineItem { Quantity = 20, QuantityReceived = 20 },
                new PurchaseOrderLineItem { Quantity = 30, QuantityReceived = 30 }
            ]
        };

        Assert.True(po.IsFullyReceived);
    }

    [Fact]
    public void PurchaseOrder_IsFullyReceived_OnePartiallyReceived_ReturnsFalse()
    {
        var po = new PurchaseOrder
        {
            LineItems =
            [
                new PurchaseOrderLineItem { Quantity = 10, QuantityReceived = 10 },
                new PurchaseOrderLineItem { Quantity = 20, QuantityReceived = 15 },
                new PurchaseOrderLineItem { Quantity = 30, QuantityReceived = 30 }
            ]
        };

        Assert.False(po.IsFullyReceived);
    }

    [Fact]
    public void PurchaseOrder_IsFullyReceived_NoneReceived_ReturnsFalse()
    {
        var po = new PurchaseOrder
        {
            LineItems =
            [
                new PurchaseOrderLineItem { Quantity = 10, QuantityReceived = 0 },
                new PurchaseOrderLineItem { Quantity = 20, QuantityReceived = 0 }
            ]
        };

        Assert.False(po.IsFullyReceived);
    }

    [Fact]
    public void PurchaseOrder_IsFullyReceived_EmptyLineItems_ReturnsFalse()
    {
        var po = new PurchaseOrder
        {
            LineItems = []
        };

        Assert.False(po.IsFullyReceived);
    }

    [Fact]
    public void PurchaseOrder_IsFullyReceived_OverReceived_ReturnsTrue()
    {
        var po = new PurchaseOrder
        {
            LineItems =
            [
                new PurchaseOrderLineItem { Quantity = 10, QuantityReceived = 12 },
                new PurchaseOrderLineItem { Quantity = 20, QuantityReceived = 25 }
            ]
        };

        Assert.True(po.IsFullyReceived);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void PurchaseOrderLineItem_HasExpectedDefaults()
    {
        var lineItem = new PurchaseOrderLineItem();

        Assert.Equal(0, lineItem.Quantity);
        Assert.Equal(0, lineItem.QuantityReceived);
        Assert.Equal(0m, lineItem.UnitCost);
        Assert.Equal(0m, lineItem.Total);
        Assert.True(lineItem.IsFullyReceived); // 0 >= 0
    }

    [Fact]
    public void PurchaseOrder_HasExpectedDefaults()
    {
        var po = new PurchaseOrder();

        Assert.Empty(po.LineItems);
        Assert.False(po.IsFullyReceived); // Empty line items = false
    }

    #endregion
}
