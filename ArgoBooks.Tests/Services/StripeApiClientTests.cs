using System.Net;
using System.Text;
using ArgoBooks.Core.Services.Integrations;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class StripeApiClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        public HttpRequestMessage? LastRequest { get; private set; }

        public StubHandler(HttpStatusCode status, string body) { _status = status; _body = body; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
        }
    }

    [Fact]
    public async Task ValidateKeyAsync_ValidKey_ReturnsOkWithBusinessName()
    {
        var handler = new StubHandler(HttpStatusCode.OK,
            "{\"id\":\"acct_1\",\"email\":\"e@x.com\",\"business_profile\":{\"name\":\"Acme Inc\"}}");
        var client = new StripeApiClient(new HttpClient(handler));

        var result = await client.ValidateKeyAsync("rk_test_abc");

        Assert.True(result.Ok);
        Assert.Equal("Acme Inc", result.AccountLabel);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("rk_test_abc", handler.LastRequest!.Headers.Authorization!.Parameter);
        Assert.Equal("https://api.stripe.com/v1/account", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ValidateKeyAsync_NoBusinessName_FallsBackToEmail()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{\"id\":\"acct_1\",\"email\":\"e@x.com\"}");
        var client = new StripeApiClient(new HttpClient(handler));

        var result = await client.ValidateKeyAsync("rk_test_abc");

        Assert.True(result.Ok);
        Assert.Equal("e@x.com", result.AccountLabel);
    }

    [Fact]
    public async Task ValidateKeyAsync_Unauthorized_ReturnsRejectedMessage()
    {
        var handler = new StubHandler(HttpStatusCode.Unauthorized, "{\"error\":{\"message\":\"Invalid API Key\"}}");
        var client = new StripeApiClient(new HttpClient(handler));

        var result = await client.ValidateKeyAsync("bad");

        Assert.False(result.Ok);
        Assert.Contains("rejected by Stripe", result.ErrorMessage);
    }
}
