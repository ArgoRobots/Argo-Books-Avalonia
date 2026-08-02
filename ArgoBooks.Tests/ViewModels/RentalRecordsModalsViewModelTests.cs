using ArgoBooks.Core.Enums;
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
}
