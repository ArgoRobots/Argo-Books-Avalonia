using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Invoice totals must follow docs/Calculations.md §4: the invoice-level discount and custom fee
/// adjust the subtotal BEFORE tax, then tax is applied to that adjusted subtotal. These tests assert
/// the doc-correct numbers; they fail while the ViewModel taxes the raw line-item subtotal and nets
/// the discount/fee in only after tax.
/// </summary>
public class InvoiceTotalsCalculationTests
{
    [Fact]
    public void FlatDiscountWithTax_TaxesThePostDiscountSubtotal()
    {
        var vm = new InvoiceModalsViewModel();
        vm.LineItems.Add(new LineItemDisplayModel { Quantity = 1, UnitPrice = 100m });
        vm.TaxRate = 10m;            // 10%
        vm.DiscountIsPercent = false;
        vm.DiscountAmount = 20m;     // flat $20 invoice-level discount
        vm.CustomFeeAmount = 0m;
        vm.SecurityDeposit = 0m;

        // §4: subtotal 100 - 20 = 80; tax = 80 * 10% = 8; total = 80 + 8 = 88.
        Assert.Equal(8m, vm.TaxAmount);
        Assert.Equal(88m, vm.Total);
    }

    [Fact]
    public void CustomFeeWithTax_TaxesThePostFeeSubtotal()
    {
        var vm = new InvoiceModalsViewModel();
        vm.LineItems.Add(new LineItemDisplayModel { Quantity = 1, UnitPrice = 100m });
        vm.TaxRate = 10m;            // 10%
        vm.CustomFeeIsPercent = false;
        vm.CustomFeeAmount = 30m;    // flat $30 fee (taxable per §4)
        vm.DiscountAmount = 0m;
        vm.SecurityDeposit = 0m;

        // §4: subtotal 100 + 30 = 130; tax = 130 * 10% = 13; total = 130 + 13 = 143.
        Assert.Equal(13m, vm.TaxAmount);
        Assert.Equal(143m, vm.Total);
    }
}
