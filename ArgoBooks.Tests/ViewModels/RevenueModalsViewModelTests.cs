using ArgoBooks.Core.Models.Entities;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Drives the real RevenueModalsViewModel add/edit/undo/redo flows. The edit round-trip guards the
/// same capture/restore asymmetry class of bug as the expense modal.
/// </summary>
public class RevenueModalsViewModelTests : ModalViewModelTestBase
{
    private RevenueModalsViewModel NewRevenueVmWithProduct()
    {
        Company.Settings.Localization.Currency = "USD";
        Company.Products.Add(new Product { Id = "P1", Name = "Widget", UnitPrice = 100m, CostPrice = 100m });
        var vm = new RevenueModalsViewModel();
        vm.OpenAddModal();
        return vm;
    }

    private void FillLineItem(RevenueModalsViewModel vm, decimal unitPrice)
    {
        var line = vm.LineItems.First();
        line.SelectedProduct = vm.ProductOptions.First(p => p.Id == "P1");
        line.Quantity = 1;
        line.UnitPrice = unitPrice;
    }

    [Fact]
    public async Task AddRevenue_CreatesRevenueWithAmountAndNotes()
    {
        var vm = NewRevenueVmWithProduct();
        FillLineItem(vm, 100m);
        vm.ModalNotes = "first";
        vm.ModalDate = new DateTimeOffset(new DateTime(2026, 3, 1), TimeSpan.Zero);

        await vm.SaveRevenueCommand.ExecuteAsync(null);

        var revenue = Assert.Single(Company.Revenues);
        Assert.Equal(100m, revenue.Total);
        Assert.Equal("first", revenue.Notes);
    }

    [Fact]
    public async Task AddRevenue_UndoThenRedo_RestoresRevenueIntact()
    {
        var vm = NewRevenueVmWithProduct();
        FillLineItem(vm, 100m);
        vm.ModalNotes = "first";
        await vm.SaveRevenueCommand.ExecuteAsync(null);

        Undo();
        Assert.Empty(Company.Revenues);

        Redo();
        var restored = Assert.Single(Company.Revenues);
        Assert.Equal(100m, restored.Total);
        Assert.Equal("first", restored.Notes);
    }

    [Fact]
    public async Task EditRevenue_UndoThenRedo_KeepsEditedAmountAndNotes()
    {
        var vm = NewRevenueVmWithProduct();
        FillLineItem(vm, 100m);
        vm.ModalNotes = "first";
        await vm.SaveRevenueCommand.ExecuteAsync(null);
        var revenueId = Company.Revenues.Single().Id;

        vm.OpenEditModal(new RevenueDisplayItem { Id = revenueId });
        vm.LineItems.First().UnitPrice = 250m;
        vm.ModalNotes = "second";
        await vm.SaveRevenueCommand.ExecuteAsync(null);

        Assert.Equal(250m, Company.Revenues.Single().Total);
        Assert.Equal("second", Company.Revenues.Single().Notes);

        Undo();
        Assert.Equal(100m, Company.Revenues.Single().Total);
        Assert.Equal("first", Company.Revenues.Single().Notes);

        Redo();
        Assert.Equal(250m, Company.Revenues.Single().Total);
        Assert.Equal("second", Company.Revenues.Single().Notes);
    }
}
