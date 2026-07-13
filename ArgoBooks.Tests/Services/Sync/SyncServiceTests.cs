using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArgoBooks.Core.Services.Sync;
using Xunit;
namespace ArgoBooks.Tests.Services.Sync;

public class SyncServiceTests
{
    private sealed class StubHandler : HttpRequestMessage { }
    private sealed class CannedHandler : HttpMessageHandler
    {
        private readonly string _json; public HttpRequestMessage? Last;
        public CannedHandler(string json) => _json = json;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(_json, Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public async Task CreatePairingToken_returns_token_from_response()
    {
        var handler = new CannedHandler("{\"success\":true,\"pairing_token\":\"abc123\",\"expires_in_seconds\":600}");
        var svc = new SyncService(new HttpClient(handler));
        var token = await svc.CreatePairingTokenAsync("uid-1", "Acme", CancellationToken.None);
        Assert.Equal("abc123", token);
        Assert.EndsWith("/pair/create", handler.Last!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task PullQueue_maps_items()
    {
        var handler = new CannedHandler("{\"success\":true,\"items\":[{\"id\":7,\"ciphertext\":\"BLOB\"}]}");
        var svc = new SyncService(new HttpClient(handler));
        var items = await svc.PullQueueAsync("uid-1", CancellationToken.None);
        Assert.Single(items);
        Assert.Equal(7, items[0].Id);
        Assert.Equal("BLOB", items[0].Ciphertext);
    }
}
