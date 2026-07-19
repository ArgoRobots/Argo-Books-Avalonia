using System.Net;
using System.Text;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services.Integrations;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class StripeSyncServiceTests
{
    // Routes by URL: the payouts list, and the charges list (fetched until a watermark).
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
            else // /v1/charges (newest first): one succeeded charge, expanded customer/invoice/balance_transaction.
            {
                body = "{\"has_more\":false,\"data\":[{" +
                       "\"id\":\"ch_1\",\"status\":\"succeeded\",\"paid\":true,\"amount\":5000,\"amount_refunded\":0,\"currency\":\"usd\",\"created\":1700000000," +
                       "\"customer\":{\"id\":\"cus_1\",\"name\":\"Jane Doe\",\"email\":\"jane@x.com\"}," +
                       "\"balance_transaction\":{\"id\":\"txn_1\",\"fee\":175,\"net\":4825}," +
                       "\"invoice\":{\"id\":\"in_1\",\"tax\":0,\"lines\":{\"data\":[{\"description\":\"Premium Plan\"}]}}" +
                       "}]}";
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
    public async Task Preview_ReturnsChargeRevenue_AndNewPayout()
    {
        var preview = await MakeService().PreviewAsync(ConnectedData());

        Assert.Single(preview.Charges);
        Assert.Equal(50.00m, preview.TotalRevenue);
        Assert.Equal(1.75m, preview.TotalFees);
        Assert.Single(preview.NewPayouts);
        Assert.True(preview.HasActivity);
        Assert.Equal("ch_1", preview.NewCursor);
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
        Assert.Equal("Premium Plan", data.Revenues[0].Description);
        Assert.NotEmpty(data.Revenues[0].CustomerId);
        Assert.Single(data.Customers);
        Assert.Equal("Jane Doe", data.Customers[0].Name);

        var stripe = data.Settings.Integrations.Stripe;
        Assert.Single(stripe.ImportedPayouts);
        Assert.Equal("po_1", stripe.ImportedPayouts[0].StripePayoutId);
        Assert.Equal("ch_1", stripe.LastSyncCursor);
        Assert.NotNull(stripe.LastSyncTime);

        // Second sync: the watermark stops the fetch immediately and the payout is known,
        // so there is nothing new.
        var preview2 = await svc.PreviewAsync(data);
        Assert.False(preview2.HasActivity);
        Assert.Empty(preview2.Charges);
        Assert.Empty(preview2.NewPayouts);
    }

    [Fact]
    public async Task Preview_NoKey_ReturnsNoActivity()
    {
        var preview = await MakeService().PreviewAsync(new CompanyData());
        Assert.False(preview.HasActivity);
    }
}
