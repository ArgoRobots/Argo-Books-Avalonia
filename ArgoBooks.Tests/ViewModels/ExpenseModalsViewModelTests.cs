using System;
using System.Linq;
using System.Threading.Tasks;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Drives the real ExpenseModalsViewModel add/edit/undo/redo flows. The edit round-trip specifically
/// guards the regression where redo re-read live (reset) ViewModel fields and wrote a $0 amount with
/// blank notes; capture/restore must round-trip the edited values exactly.
/// </summary>
public class ExpenseModalsViewModelTests : ModalViewModelTestBase
{
    private ExpenseModalsViewModel NewExpenseVmWithProduct()
    {
        Company.Settings.Localization.Currency = "USD";
        Company.Products.Add(new Product { Id = "P1", Name = "Widget", UnitPrice = 100m, CostPrice = 100m });
        var vm = new ExpenseModalsViewModel();
        vm.OpenAddModal(); // loads product options and seeds one empty line item
        return vm;
    }

    private void FillLineItem(ExpenseModalsViewModel vm, decimal unitPrice)
    {
        var line = vm.LineItems.First();
        line.SelectedProduct = vm.ProductOptions.First(p => p.Id == "P1");
        line.Quantity = 1;
        line.UnitPrice = unitPrice;
    }

    [Fact]
    public async Task AddExpense_CreatesExpenseWithAmountAndNotes()
    {
        var vm = NewExpenseVmWithProduct();
        FillLineItem(vm, 100m);
        vm.ModalNotes = "first";
        vm.ModalDate = new DateTimeOffset(new DateTime(2026, 3, 1), TimeSpan.Zero);

        await vm.SaveExpenseCommand.ExecuteAsync(null);

        var expense = Assert.Single(Company.Expenses);
        Assert.Equal(100m, expense.Total);
        Assert.Equal("first", expense.Notes);
    }

    [Fact]
    public async Task AddExpense_UndoThenRedo_RestoresExpenseIntact()
    {
        var vm = NewExpenseVmWithProduct();
        FillLineItem(vm, 100m);
        vm.ModalNotes = "first";
        await vm.SaveExpenseCommand.ExecuteAsync(null);

        Undo();
        Assert.Empty(Company.Expenses);

        Redo();
        var restored = Assert.Single(Company.Expenses);
        Assert.Equal(100m, restored.Total);
        Assert.Equal("first", restored.Notes);
    }

    [Fact]
    public async Task EditExpense_UndoThenRedo_KeepsEditedAmountAndNotes()
    {
        // Add a $100 expense noted "first".
        var vm = NewExpenseVmWithProduct();
        FillLineItem(vm, 100m);
        vm.ModalNotes = "first";
        await vm.SaveExpenseCommand.ExecuteAsync(null);
        var expenseId = Company.Expenses.Single().Id;

        // Edit it to $250 noted "second".
        vm.OpenEditModal(new ExpenseDisplayItem { Id = expenseId });
        vm.LineItems.First().UnitPrice = 250m;
        vm.ModalNotes = "second";
        await vm.SaveExpenseCommand.ExecuteAsync(null);

        var edited = Company.Expenses.Single();
        Assert.Equal(250m, edited.Total);
        Assert.Equal("second", edited.Notes);

        // Undo returns to the original values.
        Undo();
        var reverted = Company.Expenses.Single();
        Assert.Equal(100m, reverted.Total);
        Assert.Equal("first", reverted.Notes);

        // Redo must restore the EDITED values, not zero them out (the regression this guards).
        Redo();
        var redone = Company.Expenses.Single();
        Assert.Equal(250m, redone.Total);
        Assert.Equal("second", redone.Notes);
    }
}
