using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Drives the real RentalRecordsModalsViewModel return/undo/redo flow. Guards the fix where the redo
/// lambda re-read the live return-modal fields (which reset when the modal reopens) instead of the
/// values the user actually confirmed, so redo could flip "paid" back off and change the total.
/// </summary>
public class RentalRecordsModalsViewModelTests : ModalViewModelTestBase
{
    private RentalRecord SeedActiveRental()
    {
        var record = new RentalRecord
        {
            Id = "RNT-1",
            CustomerId = "CUST-1",
            Status = RentalStatus.Active,
            StartDate = DateTime.Today.AddDays(-5),
            SecurityDeposit = 20m,
            Quantity = 1,
            RateType = RateType.Daily,
            RateAmount = 10m,
            // RentalItemId "X" does not resolve to inventory, so the return skips stock adjustments.
            LineItems = new List<RentalLineItem>
            {
                new() { RentalItemId = "X", Quantity = 1, RateType = RateType.Daily, RateAmount = 10m }
            }
        };
        Company.Rentals.Add(record);
        return record;
    }

    [Fact]
    public void ReturnRental_UndoThenRedo_KeepsConfirmedPaidAndTotal()
    {
        var record = SeedActiveRental();
        var vm = new RentalRecordsModalsViewModel();

        vm.OpenReturnModal(new RentalRecordDisplayItem
        {
            Id = "RNT-1", IsActive = true, ItemName = "Widget", CustomerName = "Bob"
        });
        vm.ReturnMarkAsPaid = true;                 // the value the user confirms
        var confirmedCost = vm.ReturnTotalCost;     // computed from the line items
        vm.ConfirmReturn();

        Assert.Equal(RentalStatus.Returned, record.Status);
        Assert.True(record.Paid);
        Assert.Equal(confirmedCost, record.TotalCost);

        Undo();
        Assert.Equal(RentalStatus.Active, record.Status);
        Assert.False(record.Paid);

        // Simulate the modal being reopened for another record, which resets the live fields.
        vm.ReturnMarkAsPaid = false;
        vm.ReturnTotalCost = 999m;

        Redo();
        // Redo must reapply the CONFIRMED values, not the reset/live ones.
        Assert.Equal(RentalStatus.Returned, record.Status);
        Assert.True(record.Paid);
        Assert.Equal(confirmedCost, record.TotalCost);
    }

    [Fact]
    public void ReturnModal_ChangingReturnDate_RecomputesTheChargedCost()
    {
        var record = SeedActiveRental(); // started 5 days ago, 1 unit at 10/day
        var vm = new RentalRecordsModalsViewModel();
        vm.OpenReturnModal(new RentalRecordDisplayItem
        {
            Id = "RNT-1", IsActive = true, ItemName = "Widget", CustomerName = "Bob"
        });
        Assert.Equal(50m, vm.ReturnTotalCost);

        vm.ReturnDate = new DateTimeOffset(record.StartDate.AddDays(10));
        Assert.Equal(100m, vm.ReturnTotalCost);

        vm.ConfirmReturn();
        Assert.Equal(100m, record.TotalCost);
    }

    private InventoryItem SeedRentableStock(int inStock)
    {
        Company.Customers.Add(new Customer { Id = "CUST-1", Name = "Bob" });
        Company.Products.Add(new Product { Id = "P1", Name = "Ladder" });
        var stock = new InventoryItem { Id = "INV-1", ProductId = "P1", InStock = inStock };
        Company.Inventory.Add(stock);
        Company.RentalInventory.Add(new RentalItem { Id = "RI-1", InventoryItemId = "INV-1", DailyRate = 10m });
        return stock;
    }

    private static RentalRecordsModalsViewModel NewRentalWithTwoLinesOfSameItem(string firstQty, string secondQty)
    {
        var vm = new RentalRecordsModalsViewModel();
        vm.OpenAddModal();
        vm.ModalCustomer = vm.AvailableCustomers.Single();
        var first = vm.RentalLineItems.Single();
        first.SelectedItem = vm.AvailableItems.Single();
        first.Quantity = firstQty;
        vm.AddRentalLineItem();
        var second = vm.RentalLineItems.Last();
        second.SelectedItem = vm.AvailableItems.Single();
        second.Quantity = secondQty;
        return vm;
    }

    [Fact]
    public void NewRental_SameItemOnTwoLines_CannotRentMoreThanInStock()
    {
        var stock = SeedRentableStock(5);
        var vm = NewRentalWithTwoLinesOfSameItem("3", "3");

        vm.SaveNewRecord();

        Assert.Empty(Company.Rentals);
        Assert.Equal(5, stock.InStock);
        Assert.Contains(vm.RentalLineItems, li => li.QuantityError != null);
    }

    [Fact]
    public void NewRental_SameItemOnTwoLines_WithinStock_Saves()
    {
        var stock = SeedRentableStock(5);
        var vm = NewRentalWithTwoLinesOfSameItem("2", "3");

        vm.SaveNewRecord();

        Assert.Single(Company.Rentals);
        Assert.Equal(0, stock.InStock);
    }
}
