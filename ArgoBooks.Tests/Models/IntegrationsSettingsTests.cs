using System.Text.Json;
using ArgoBooks.Core.Models;
using Xunit;

namespace ArgoBooks.Tests.Models;

public class IntegrationsSettingsTests
{
    [Fact]
    public void CompanySettings_RoundTrips_StripeIntegration()
    {
        var settings = new CompanySettings();
        settings.Integrations.Stripe.ApiKey = "rk_test_abc";
        settings.Integrations.Stripe.Connected = true;
        settings.Integrations.Stripe.AccountLabel = "Acme Inc";
        settings.Integrations.Stripe.LastSyncCursor = "txn_123";

        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<CompanySettings>(json)!;

        Assert.Equal("rk_test_abc", restored.Integrations.Stripe.ApiKey);
        Assert.True(restored.Integrations.Stripe.Connected);
        Assert.Equal("Acme Inc", restored.Integrations.Stripe.AccountLabel);
        Assert.Equal("txn_123", restored.Integrations.Stripe.LastSyncCursor);
    }

    [Fact]
    public void Integrations_DefaultsToDisconnectedStripe()
    {
        var settings = new CompanySettings();
        Assert.NotNull(settings.Integrations);
        Assert.NotNull(settings.Integrations.Stripe);
        Assert.False(settings.Integrations.Stripe.Connected);
        Assert.Null(settings.Integrations.Stripe.ApiKey);
    }
}
