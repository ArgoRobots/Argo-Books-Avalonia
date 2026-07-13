using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArgoBooks.Shared.Sync;
using Xunit;

namespace ArgoBooks.Tests.Services.Sync;

public class MobileSyncClientTests
{
    private sealed class CannedHandler : HttpMessageHandler
    {
        private readonly string _json;
        private readonly HttpStatusCode _statusCode;
        public HttpRequestMessage? Last;

        public CannedHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _json = json;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            { Content = new StringContent(_json, Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public async Task RedeemPairing_parses_device_token_and_company_uid()
    {
        var handler = new CannedHandler(
            "{\"success\":true,\"device_token\":\"token-abc123\",\"company_uid\":\"company-456\",\"company_label\":\"Acme Corp\"}");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");

        var result = await client.RedeemPairingAsync("pairing-xyz", "My Phone", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("token-abc123", result.DeviceToken);
        Assert.Equal("company-456", result.CompanyUid);
        Assert.Equal("Acme Corp", result.CompanyLabel);
    }

    [Fact]
    public async Task RedeemPairing_posts_to_correct_endpoint()
    {
        var handler = new CannedHandler(
            "{\"success\":true,\"device_token\":\"token-abc123\",\"company_uid\":\"company-456\",\"company_label\":\"Acme Corp\"}");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");

        await client.RedeemPairingAsync("pairing-xyz", "My Phone", CancellationToken.None);

        Assert.EndsWith("/pair/redeem", handler.Last!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetSnapshot_sends_device_token_header()
    {
        var handler = new CannedHandler("{\"ciphertext\":\"encrypted-data\",\"updated_at\":\"2026-07-13T10:00:00Z\"}");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");

        await client.GetSnapshotAsync("device-token-123", CancellationToken.None);

        Assert.True(handler.Last!.Headers.Contains("X-Sync-Device-Token"));
        var headerValues = handler.Last.Headers.GetValues("X-Sync-Device-Token");
        Assert.Contains("device-token-123", headerValues);
    }

    [Fact]
    public async Task GetSnapshot_parses_ciphertext_and_updated_at()
    {
        var handler = new CannedHandler("{\"ciphertext\":\"blob-data\",\"updated_at\":\"2026-07-13T10:00:00Z\"}");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");

        var result = await client.GetSnapshotAsync("device-token-123", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("blob-data", result.Ciphertext);
        Assert.Equal("2026-07-13T10:00:00Z", result.UpdatedAt);
    }

    [Fact]
    public async Task GetSnapshot_returns_null_on_404()
    {
        var handler = new CannedHandler("{}", HttpStatusCode.NotFound);
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");

        var result = await client.GetSnapshotAsync("device-token-123", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSnapshot_posts_to_correct_endpoint()
    {
        var handler = new CannedHandler("{\"ciphertext\":\"blob-data\",\"updated_at\":\"2026-07-13T10:00:00Z\"}");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");

        await client.GetSnapshotAsync("device-token-123", CancellationToken.None);

        Assert.EndsWith("/snapshot/get", handler.Last!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task PushCapture_posts_ciphertext_with_device_token_header()
    {
        var handler = new CannedHandler("{\"success\":true}");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");

        await client.PushCaptureAsync("device-token-123", "capture-blob-456", CancellationToken.None);

        Assert.True(handler.Last!.Headers.Contains("X-Sync-Device-Token"));
        var headerValues = handler.Last.Headers.GetValues("X-Sync-Device-Token");
        Assert.Contains("device-token-123", headerValues);
        Assert.EndsWith("/queue/push", handler.Last.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task PushCapture_posts_to_correct_endpoint()
    {
        var handler = new CannedHandler("{\"success\":true}");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");

        await client.PushCaptureAsync("device-token-123", "capture-blob-456", CancellationToken.None);

        Assert.EndsWith("/queue/push", handler.Last!.RequestUri!.AbsolutePath);
    }
}
