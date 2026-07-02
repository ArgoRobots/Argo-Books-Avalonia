using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Drives the real PurchaseOrdersModalsViewModel edit/undo/redo flow. Guards the fix where editing a
/// PO recomputed its USD total but undo/redo never restored the currency fields, so undo left a stale
/// TotalUSD (priced for the edited state) on a reverted order.
/// </summary>
public class PurchaseOrdersModalsViewModelTests : ModalViewModelTestBase
{
    private PurchaseOrder SeedOrder()
    {
        Company.Settings.Localization.Currency = "USD";
        Company.Suppliers.Add(new Supplier { Id = "S1", Name = "Acme" });
        Company.Products.Add(new Product { Id = "P1", Name = "Widget", UnitPrice = 100m, CostPrice = 100m });
        var order = new PurchaseOrder
        {
            Id = "PO-1",
            SupplierId = "S1",
            OrderDate = new DateTime(2026, 3, 1),
            ExpectedDeliveryDate = new DateTime(2026, 3, 8),
            Subtotal = 100m,
            ShippingCost = 0m,
            Total = 100m,
            OriginalCurrency = "USD",
            TotalUSD = 100m,
            LineItems = new List<PurchaseOrderLineItem>
            {
                new() { ProductId = "P1", Quantity = 1, UnitCost = 100m }
            }
        };
        Company.PurchaseOrders.Add(order);
        return order;
    }

    [Fact]
    public async Task EditOrder_UndoThenRedo_RestoresTotalAndUsdTotal()
    {
        var order = SeedOrder();
        var vm = new PurchaseOrdersModalsViewModel();

        vm.OpenEditModal(new PurchaseOrderDisplayItem { Id = "PO-1" });
        vm.LineItems.First().UnitCost = "250.00"; // raise the total to 250
        await vm.SaveOrderCommand.ExecuteAsync(null);

        Assert.Equal(250m, order.Total);
        Assert.Equal(250m, order.TotalUSD);

        // Undo must revert BOTH the native total and the USD total (the fix); a stale TotalUSD of 250
        // on a reverted 100 order was the bug.
        Undo();
        Assert.Equal(100m, order.Total);
        Assert.Equal(100m, order.TotalUSD);
        Assert.False(order.IsPendingConversion);

        Redo();
        Assert.Equal(250m, order.Total);
        Assert.Equal(250m, order.TotalUSD);
    }
}
