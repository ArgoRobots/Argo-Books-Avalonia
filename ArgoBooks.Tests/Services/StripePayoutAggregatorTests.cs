using ArgoBooks.Core.Services.Integrations;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class StripePayoutAggregatorTests
{
    private static StripeImportItem Item(StripeItemKind kind, decimal amt, string? payout)
        => new(kind, amt, new DateTime(2026, 1, 15), "x", "src", payout);

    [Fact]
    public void Aggregates_OnePayout_SumsGrossFeesRefunds()
    {
        var items = new[]
        {
            Item(StripeItemKind.Revenue, 50m, "po_1"),
            Item(StripeItemKind.Revenue, 30m, "po_1"),
            Item(StripeItemKind.Fee, 2.30m, "po_1"),
            Item(StripeItemKind.Refund, 5m, "po_1"),
            Item(StripeItemKind.Payout, 72.70m, "po_1"),
        };

        var batch = Assert.Single(new StripePayoutAggregator().Aggregate(items));
        Assert.Equal("po_1", batch.PayoutId);
        Assert.Equal(80m, batch.GrossRevenue);
        Assert.Equal(2.30m, batch.Fees);
        Assert.Equal(5m, batch.Refunds);
        Assert.Equal(72.70m, batch.NetAmount);
    }

    [Fact]
    public void SeparatesByPayoutId()
    {
        var items = new[]
        {
            Item(StripeItemKind.Revenue, 10m, "po_1"),
            Item(StripeItemKind.Revenue, 20m, "po_2"),
        };
        var batches = new StripePayoutAggregator().Aggregate(items);
        Assert.Equal(2, batches.Count);
    }

    [Fact]
    public void ExcludesNotYetPaidOutItems()
    {
        var items = new[]
        {
            Item(StripeItemKind.Revenue, 10m, null),
            Item(StripeItemKind.Revenue, 20m, "po_1"),
        };
        var batch = Assert.Single(new StripePayoutAggregator().Aggregate(items));
        Assert.Equal(20m, batch.GrossRevenue);
    }

    [Fact]
    public void NoPayoutItem_NetIsGrossMinusFeesRefunds()
    {
        var items = new[]
        {
            Item(StripeItemKind.Revenue, 100m, "po_9"),
            Item(StripeItemKind.Fee, 3m, "po_9"),
            Item(StripeItemKind.Refund, 7m, "po_9"),
        };
        var batch = Assert.Single(new StripePayoutAggregator().Aggregate(items));
        Assert.Equal(90m, batch.NetAmount);
    }
}
