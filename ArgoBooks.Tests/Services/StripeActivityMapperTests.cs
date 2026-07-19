using ArgoBooks.Core.Services.Integrations;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class StripeActivityMapperTests
{
    private static StripeBalanceTransaction Tx(string id, string type, long amount, long fee)
        => new(id, type, amount, fee, amount - fee, 1700000000, "usd", null, null);

    [Fact]
    public void Charge_EmitsGrossRevenue_AndFeeExpense()
    {
        var items = new StripeActivityMapper().Map(new[] { Tx("txn_1", "charge", 5000, 175) });

        Assert.Equal(2, items.Count);
        var rev = Assert.Single(items, i => i.Kind == StripeItemKind.Revenue);
        Assert.Equal(50.00m, rev.Amount);
        var fee = Assert.Single(items, i => i.Kind == StripeItemKind.Fee);
        Assert.Equal(1.75m, fee.Amount);
    }

    [Fact]
    public void Charge_NoFee_EmitsOnlyRevenue()
    {
        var items = new StripeActivityMapper().Map(new[] { Tx("txn_1", "charge", 5000, 0) });
        Assert.Single(items);
        Assert.Equal(StripeItemKind.Revenue, items[0].Kind);
    }

    [Fact]
    public void Refund_EmitsRefundItem_PositiveAmount()
    {
        var items = new StripeActivityMapper().Map(new[] { Tx("txn_2", "refund", -2000, 0) });
        var r = Assert.Single(items);
        Assert.Equal(StripeItemKind.Refund, r.Kind);
        Assert.Equal(20.00m, r.Amount);
    }

    [Fact]
    public void Payout_EmitsPayoutItem()
    {
        var items = new StripeActivityMapper().Map(new[] { Tx("po_1", "payout", -4825, 0) });
        var p = Assert.Single(items);
        Assert.Equal(StripeItemKind.Payout, p.Kind);
        Assert.Equal(48.25m, p.Amount);
    }

    [Fact]
    public void UnknownType_EmitsOther_NotDropped()
    {
        var items = new StripeActivityMapper().Map(new[] { Tx("txn_x", "adjustment", 500, 0) });
        var o = Assert.Single(items);
        Assert.Equal(StripeItemKind.Other, o.Kind);
        Assert.Contains("adjustment", o.Description);
    }
}
