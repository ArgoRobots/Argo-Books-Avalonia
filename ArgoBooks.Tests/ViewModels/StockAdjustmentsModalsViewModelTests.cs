using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Drives the real StockAdjustmentsModalsViewModel add/undo/redo flow: an adjustment moves the item's
/// InStock and writes a StockAdjustment ledger row, and undo/redo restore both exactly.
/// </summary>
public class StockAdjustmentsModalsViewModelTests : ModalViewModelTestBase
{
    private StockAdjustmentsModalsViewModel NewVmWithItem(int startStock)
    {
        Company.Products.Add(new Product { Id = "P1", Name = "Widget" });
        Company.Inventory.Add(new InventoryItem { Id = "I1", ProductId = "P1", InStock = startStock });
        var vm = new StockAdjustmentsModalsViewModel();
        vm.OpenAddModal();
        vm.SelectedInventoryOption = vm.AvailableInventoryItems.First();
        return vm;
    }

    [Fact]
    public void AddAdjustment_ChangesStockAndWritesLedgerRow()
    {
        var vm = NewVmWithItem(100);
        vm.AdjustmentType = "Add";
        vm.AdjustmentQuantity = "50";

        vm.SaveAdjustmentCommand.Execute(null);

        Assert.Equal(150, Company.Inventory.Single().InStock);
        var adj = Assert.Single(Company.StockAdjustments);
        Assert.Equal(100, adj.PreviousStock);
        Assert.Equal(150, adj.NewStock);
    }

    [Fact]
    public void AddAdjustment_UndoThenRedo_RestoresStockAndLedger()
    {
        var vm = NewVmWithItem(100);
        vm.AdjustmentType = "Add";
        vm.AdjustmentQuantity = "50";
        vm.SaveAdjustmentCommand.Execute(null);

        Undo();
        Assert.Equal(100, Company.Inventory.Single().InStock);
        Assert.Empty(Company.StockAdjustments);

        Redo();
        Assert.Equal(150, Company.Inventory.Single().InStock);
        Assert.Single(Company.StockAdjustments);
    }

    [Fact]
    public void RemoveMoreThanInStock_IsRejected()
    {
        var vm = NewVmWithItem(10);
        vm.AdjustmentType = "Remove";
        vm.AdjustmentQuantity = "25"; // more than the 10 in stock

        vm.SaveAdjustmentCommand.Execute(null);

        // No change and no ledger row; the guard blocks negative stock.
        Assert.Equal(10, Company.Inventory.Single().InStock);
        Assert.Empty(Company.StockAdjustments);
    }
}
