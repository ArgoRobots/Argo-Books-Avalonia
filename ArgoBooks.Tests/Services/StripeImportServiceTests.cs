using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services.Integrations;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class StripeImportServiceTests
{
    private static StripeDailyBatch Day(decimal gross, decimal fees, decimal refunds)
        => new(new DateTime(2026, 1, 15), gross, fees, refunds);

    [Fact]
    public void Import_CreatesRevenueAndFeeExpense()
    {
        var data = new CompanyData();
        var result = new StripeImportService().Import(data, new[] { Day(80m, 2.30m, 0m) });

        Assert.Equal(1, result.RevenuesCreated);
        Assert.Single(data.Revenues);
        Assert.Equal(80m, data.Revenues[0].Total);
        Assert.Single(data.Expenses);
        Assert.Equal(2.30m, data.Expenses[0].Total);
    }

    [Fact]
    public void Import_NoRevenue_StillPostsFee()
    {
        var data = new CompanyData();
        new StripeImportService().Import(data, new[] { Day(0m, 1m, 0m) });
        Assert.Empty(data.Revenues);
        Assert.Single(data.Expenses);
    }

    [Fact]
    public void Import_Refunds_PostAsExpense()
    {
        var data = new CompanyData();
        new StripeImportService().Import(data, new[] { Day(50m, 0m, 10m) });
        Assert.Single(data.Revenues);
        var refund = Assert.Single(data.Expenses);
        Assert.Equal(10m, refund.Total);
    }

    [Fact]
    public void Import_MultipleDays_CreatesRecordPerDay()
    {
        var data = new CompanyData();
        var batches = new[]
        {
            new StripeDailyBatch(new DateTime(2026, 1, 15), 10m, 0.3m, 0m),
            new StripeDailyBatch(new DateTime(2026, 1, 16), 20m, 0.6m, 0m),
        };
        var result = new StripeImportService().Import(data, batches);
        Assert.Equal(2, result.RevenuesCreated);
        Assert.Equal(2, data.Revenues.Count);
    }
}
