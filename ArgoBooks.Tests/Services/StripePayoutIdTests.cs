using ArgoBooks.Core.Services.Integrations;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class StripePayoutIdTests
{
    [Fact]
    public void Mapper_CarriesPayoutId_OntoItems()
    {
        var tx = new StripeBalanceTransaction("txn_1", "charge", 5000, 175, 4825, 1700000000, "usd", null, "po_123");
        var items = new StripeActivityMapper().Map(new[] { tx });
        Assert.All(items, i => Assert.Equal("po_123", i.PayoutId));
    }

    [Fact]
    public void Mapper_NullPayoutId_WhenNotYetPaidOut()
    {
        var tx = new StripeBalanceTransaction("txn_1", "charge", 5000, 175, 4825, 1700000000, "usd", null, null);
        var items = new StripeActivityMapper().Map(new[] { tx });
        Assert.All(items, i => Assert.Null(i.PayoutId));
    }
}
