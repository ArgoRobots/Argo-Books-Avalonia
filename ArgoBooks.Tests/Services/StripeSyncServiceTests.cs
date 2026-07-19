using System.Net;
using System.Text;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services.Integrations;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class StripeSyncServiceTests
{
    // Routes by URL: the payouts list, and the balance-transactions list (fetched until a watermark).
    private sealed class RoutingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var url = request.RequestUri!.ToString();
            string body;
            if (url.Contains("/v1/payouts"))
            {
                // One paid payout po_1, amount 48.25, arrival date.
                body = "{\"has_more\":false,\"data\":[" +
                       "{\"id\":\"po_1\",\"amount\":4825,\"arrival_date\":1700000900,\"created\":1700000800,\"status\":\"paid\"}" +
                       "]}";
            }
            else // balance_transactions (newest first): one charge, gross 50.00, fee 1.75.
            {
                body = "{\"has_more\":false,\"data\":[" +
                       "{\"id\":\"txn_c\",\"type\":\"charge\",\"amount\":5000,\"fee\":175,\"net\":4825,\"created\":1700000000,\"currency\":\"usd\",\"description\":\"Sub\"}" +
                       "]}";
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private static StripeSyncService MakeService()
        => new StripeSyncService(new StripeApiClient(new HttpClient(new RoutingHandler())));

    private static CompanyData ConnectedData()
    {
        var data = new CompanyData();
        data.Settings.Integrations.Stripe.ApiKey = "rk_test";
        data.Settings.Integrations.Stripe.Connected = true;
        return data;
    }

    [Fact]
    public async Task Preview_ReturnsDailyRevenue_AndNewPayout()
    {
        var preview = await MakeService().PreviewAsync(ConnectedData());

        Assert.Single(preview.Days);
        Assert.Equal(50.00m, preview.TotalRevenue);
        Assert.Equal(1.75m, preview.TotalFees);
        Assert.Single(preview.NewPayouts);
        Assert.True(preview.HasActivity);
        Assert.Equal("txn_c", preview.NewCursor);
    }

    [Fact]
    public async Task Import_CreatesRecords_RemembersPayout_AdvancesCursor_ThenSecondPreviewEmpty()
    {
        var svc = MakeService();
        var data = ConnectedData();

        var preview = await svc.PreviewAsync(data);
        var result = svc.ImportPreview(data, preview);

        Assert.Equal(1, result.RevenuesCreated);
        Assert.Single(data.Revenues);
        var stripe = data.Settings.Integrations.Stripe;
        Assert.Single(stripe.ImportedPayouts);
        Assert.Equal("po_1", stripe.ImportedPayouts[0].StripePayoutId);
        Assert.Equal("txn_c", stripe.LastSyncCursor);
        Assert.NotNull(stripe.LastSyncTime);

        // Second sync: the watermark stops the fetch immediately and the payout is known,
        // so there is nothing new.
        var preview2 = await svc.PreviewAsync(data);
        Assert.False(preview2.HasActivity);
        Assert.Empty(preview2.Days);
        Assert.Empty(preview2.NewPayouts);
    }

    [Fact]
    public async Task Preview_NoKey_ReturnsNoActivity()
    {
        var preview = await MakeService().PreviewAsync(new CompanyData());
        Assert.False(preview.HasActivity);
    }
}
