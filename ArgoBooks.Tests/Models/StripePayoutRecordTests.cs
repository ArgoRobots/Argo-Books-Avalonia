using System.Text.Json;
using ArgoBooks.Core.Models.Integrations;
using Xunit;

namespace ArgoBooks.Tests.Models;

public class StripePayoutRecordTests
{
    [Fact]
    public void ImportedPayouts_RoundTrip()
    {
        var s = new StripeIntegrationSettings();
        s.ImportedPayouts.Add(new StripePayoutRecord { StripePayoutId = "po_1", AmountCents = 7270, Date = new DateTime(2026, 1, 15) });

        var json = JsonSerializer.Serialize(s);
        var back = JsonSerializer.Deserialize<StripeIntegrationSettings>(json)!;

        var p = Assert.Single(back.ImportedPayouts);
        Assert.Equal("po_1", p.StripePayoutId);
        Assert.Equal(7270, p.AmountCents);
    }

    [Fact]
    public void ImportedPayouts_DefaultsEmpty()
    {
        Assert.Empty(new StripeIntegrationSettings().ImportedPayouts);
    }
}
