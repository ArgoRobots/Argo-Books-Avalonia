using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services.Integrations;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class StripeImportServiceTests
{
    private static StripePayoutBatch Batch(string id, decimal gross, decimal fees, decimal refunds, decimal net)
        => new(id, new DateTime(2026, 1, 15), gross, fees, refunds, net);

    [Fact]
    public void Import_CreatesRevenueAndFeeExpense_AndRemembersPayout()
    {
        var data = new CompanyData();
        var result = new StripeImportService().Import(data, new[] { Batch("po_1", 80m, 2.30m, 0m, 77.70m) });

        Assert.Equal(1, result.PayoutsImported);
        Assert.Single(data.Revenues);
        Assert.Equal(80m, data.Revenues[0].Total);
        Assert.Single(data.Expenses);
        Assert.Equal(2.30m, data.Expenses[0].Total);
        Assert.Single(data.Settings.Integrations.Stripe.ImportedPayouts);
        Assert.Equal("po_1", data.Settings.Integrations.Stripe.ImportedPayouts[0].StripePayoutId);
    }

    [Fact]
    public void Import_SkipsAlreadyImportedPayout()
    {
        var data = new CompanyData();
        data.Settings.Integrations.Stripe.ImportedPayouts.Add(
            new ArgoBooks.Core.Models.Integrations.StripePayoutRecord { StripePayoutId = "po_1", AmountCents = 7770, Date = new DateTime(2026, 1, 15) });

        var result = new StripeImportService().Import(data, new[] { Batch("po_1", 80m, 2.30m, 0m, 77.70m) });

        Assert.Equal(0, result.PayoutsImported);
        Assert.Equal(1, result.SkippedAlreadyImported);
        Assert.Empty(data.Revenues);
        Assert.Empty(data.Expenses);
    }

    [Fact]
    public void Import_NoRevenue_CreatesNoRevenueRecord()
    {
        var data = new CompanyData();
        var result = new StripeImportService().Import(data, new[] { Batch("po_2", 0m, 1m, 0m, -1m) });
        Assert.Empty(data.Revenues);
        Assert.Single(data.Expenses); // the fee still posts
    }
}
