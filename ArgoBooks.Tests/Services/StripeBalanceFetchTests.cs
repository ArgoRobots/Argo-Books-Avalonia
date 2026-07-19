using System.Net;
using System.Text;
using ArgoBooks.Core.Services.Integrations;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class StripeBalanceFetchTests
{
    private sealed class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<string> _bodies;
        public List<string> RequestedUris { get; } = new();
        public QueueHandler(params string[] bodies) => _bodies = new Queue<string>(bodies);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestedUris.Add(request.RequestUri!.ToString());
            var body = _bodies.Dequeue();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public async Task Fetch_SinglePage_ParsesFields()
    {
        var body = "{\"has_more\":false,\"data\":[" +
            "{\"id\":\"txn_1\",\"type\":\"charge\",\"amount\":5000,\"fee\":175,\"net\":4825,\"created\":1700000000,\"currency\":\"usd\",\"description\":\"Sub\"}" +
            "]}";
        var handler = new QueueHandler(body);
        var client = new StripeApiClient(new HttpClient(handler));

        var result = await client.FetchBalanceTransactionsSinceAsync("rk_test", null);

        Assert.Single(result);
        var t = result[0];
        Assert.Equal("txn_1", t.Id);
        Assert.Equal("charge", t.Type);
        Assert.Equal(5000, t.AmountCents);
        Assert.Equal(175, t.FeeCents);
        Assert.Equal(4825, t.NetCents);
        Assert.Equal("usd", t.Currency);
        Assert.Contains("limit=100", handler.RequestedUris[0]);
        Assert.DoesNotContain("starting_after", handler.RequestedUris[0]);
    }

    [Fact]
    public async Task Fetch_WithCursor_AddsStartingAfter()
    {
        var body = "{\"has_more\":false,\"data\":[]}";
        var handler = new QueueHandler(body);
        var client = new StripeApiClient(new HttpClient(handler));

        await client.FetchBalanceTransactionsSinceAsync("rk_test", "txn_prev");

        Assert.Contains("starting_after=txn_prev", handler.RequestedUris[0]);
    }

    [Fact]
    public async Task Fetch_MultiPage_FollowsHasMore()
    {
        var page1 = "{\"has_more\":true,\"data\":[" +
            "{\"id\":\"txn_1\",\"type\":\"charge\",\"amount\":100,\"fee\":3,\"net\":97,\"created\":1,\"currency\":\"usd\",\"description\":null}]}";
        var page2 = "{\"has_more\":false,\"data\":[" +
            "{\"id\":\"txn_2\",\"type\":\"charge\",\"amount\":200,\"fee\":6,\"net\":194,\"created\":2,\"currency\":\"usd\",\"description\":null}]}";
        var handler = new QueueHandler(page1, page2);
        var client = new StripeApiClient(new HttpClient(handler));

        var result = await client.FetchBalanceTransactionsSinceAsync("rk_test", null);

        Assert.Equal(2, result.Count);
        Assert.Contains("starting_after=txn_1", handler.RequestedUris[1]);
    }
}
