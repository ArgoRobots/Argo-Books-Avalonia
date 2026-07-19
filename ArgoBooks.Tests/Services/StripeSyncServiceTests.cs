using System.Net;
using System.Text;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services.Integrations;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class StripeSyncServiceTests
{
    // Routes by URL: the payouts list, then the per-payout balance transactions.
    private sealed class RoutingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var url = request.RequestUri!.ToString();
            string body;
            if (url.Contains("/v1/payouts"))
            {
                // One paid payout po_1, net 48.25.
                body = "{\"has_more\":false,\"data\":[" +
                       "{\"id\":\"po_1\",\"amount\":4825,\"created\":1700000500,\"status\":\"paid\"}" +
                       "]}";
            }
            else // balance_transactions?payout=po_1: a charge (gross 50.00, fee 1.75) and the payout txn.
            {
                body = "{\"has_more\":false,\"data\":[" +
                       "{\"id\":\"txn_c\",\"type\":\"charge\",\"amount\":5000,\"fee\":175,\"net\":4825,\"created\":1700000000,\"currency\":\"usd\",\"description\":\"Sub\"}," +
                       "{\"id\":\"txn_p\",\"type\":\"payout\",\"amount\":-4825,\"fee\":0,\"net\":-4825,\"created\":1700000500,\"currency\":\"usd\",\"description\":null}" +
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
    public async Task Preview_ReturnsNewBatch_WithTotals()
    {
        var data = ConnectedData();
        var preview = await MakeService().PreviewAsync(data);

        var batch = Assert.Single(preview.NewBatches);
        Assert.Equal("po_1", batch.PayoutId);
        Assert.Equal(50.00m, preview.TotalRevenue);
        Assert.Equal(1.75m, preview.TotalFees);
    }

    [Fact]
    public async Task Import_CreatesRecords_AndRemembersPayout_ThenPreviewIsEmpty()
    {
        var svc = MakeService();
        var data = ConnectedData();

        var preview = await svc.PreviewAsync(data);
        var result = svc.ImportPreview(data, preview);

        Assert.Equal(1, result.PayoutsImported);
        Assert.Single(data.Revenues);
        Assert.NotNull(data.Settings.Integrations.Stripe.LastSyncTime);

        // A second preview now sees nothing new (deduped by ImportedPayouts).
        var preview2 = await svc.PreviewAsync(data);
        Assert.Empty(preview2.NewBatches);
    }

    [Fact]
    public async Task Preview_NoKey_ReturnsEmpty()
    {
        var preview = await MakeService().PreviewAsync(new CompanyData());
        Assert.Empty(preview.NewBatches);
    }
}
