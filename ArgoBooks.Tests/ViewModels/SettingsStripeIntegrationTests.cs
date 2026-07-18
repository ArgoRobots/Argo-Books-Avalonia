using System.Net;
using System.Text;
using ArgoBooks.Core.Models.Integrations;
using ArgoBooks.Core.Services.Integrations;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

public class SettingsStripeIntegrationTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        public StubHandler(HttpStatusCode status, string body) { _status = status; _body = body; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_status)
            { Content = new StringContent(_body, Encoding.UTF8, "application/json") });
    }

    [Fact]
    public async Task TryConnectStripe_ValidKey_StoresKeyAndMarksConnected()
    {
        var vm = new SettingsModalViewModel();
        vm.StripeKeyInput = "rk_test_abc";
        var target = new StripeIntegrationSettings();
        var client = new StripeApiClient(new HttpClient(
            new StubHandler(HttpStatusCode.OK, "{\"id\":\"acct_1\",\"business_profile\":{\"name\":\"Acme Inc\"}}")));

        var connected = await vm.TryConnectStripeAsync(client, target);

        Assert.True(connected);
        Assert.True(target.Connected);
        Assert.Equal("rk_test_abc", target.ApiKey);
        Assert.Equal("Acme Inc", target.AccountLabel);
        Assert.True(vm.StripeIntegrationConnected);
        Assert.Equal("Acme Inc", vm.StripeIntegrationAccountLabel);
        Assert.Null(vm.StripeIntegrationError);
    }

    [Fact]
    public async Task TryConnectStripe_RejectedKey_SetsErrorAndStaysDisconnected()
    {
        var vm = new SettingsModalViewModel();
        vm.StripeKeyInput = "bad";
        var target = new StripeIntegrationSettings();
        var client = new StripeApiClient(new HttpClient(
            new StubHandler(HttpStatusCode.Unauthorized, "{\"error\":{\"message\":\"no\"}}")));

        var connected = await vm.TryConnectStripeAsync(client, target);

        Assert.False(connected);
        Assert.False(target.Connected);
        Assert.Null(target.ApiKey);
        Assert.False(vm.StripeIntegrationConnected);
        Assert.NotNull(vm.StripeIntegrationError);
    }
}
