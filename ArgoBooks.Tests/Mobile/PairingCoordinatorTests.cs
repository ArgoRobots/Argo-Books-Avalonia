using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ArgoBooks.Shared.Mobile;
using ArgoBooks.Shared.Sync;
using Xunit;

namespace ArgoBooks.Tests.Mobile;

/// <summary>
/// Unit tests for PairingCoordinator, using a canned HttpMessageHandler (fakes the sync server's
/// /pair/redeem response) and an in-memory ISecureStore (fakes device secure storage).
/// </summary>
public class PairingCoordinatorTests
{
    private sealed class CannedHandler : HttpMessageHandler
    {
        private readonly string _json;
        private readonly HttpStatusCode _statusCode;
        public HttpRequestMessage? Last;
        public string? LastBody;

        public CannedHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _json = json;
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(_statusCode)
            { Content = new StringContent(_json, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class InMemorySecureStore : ISecureStore
    {
        private readonly Dictionary<string, string> _storage = new();

        public Task SetAsync(string key, string value)
        {
            _storage[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key)
        {
            _storage.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }

        public Task RemoveAsync(string key)
        {
            _storage.Remove(key);
            return Task.CompletedTask;
        }
    }

    private const string ValidPayload =
        "{\"t\":\"pairing-tok-123\",\"u\":\"company-uid-456\",\"l\":\"Acme Corp\",\"k\":\"sync-key-base64==\"}";

    [Fact]
    public async Task ValidPayload_PairsAndStoresAndSetsActive()
    {
        var handler = new CannedHandler(
            "{\"success\":true,\"device_token\":\"device-tok-abc\",\"company_uid\":\"company-uid-456\",\"company_label\":\"Acme Corp\"}");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store);

        var outcome = await coordinator.PairFromPayloadAsync(ValidPayload, "My Phone", CancellationToken.None);

        Assert.True(outcome.Success);
        Assert.Equal("Acme Corp", outcome.CompanyLabel);
        Assert.Null(outcome.Error);

        var all = await store.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("company-uid-456", all[0].CompanyUid);
        Assert.Equal("Acme Corp", all[0].CompanyLabel);
        Assert.Equal("device-tok-abc", all[0].DeviceToken);
        Assert.Equal("sync-key-base64==", all[0].SyncKeyBase64);

        var active = await store.GetActiveAsync();
        Assert.NotNull(active);
        Assert.Equal("company-uid-456", active!.CompanyUid);
    }

    [Fact]
    public async Task MalformedJson_ReturnsFailure_StoresNothing()
    {
        var handler = new CannedHandler("{\"success\":true,\"device_token\":\"device-tok-abc\"}");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store);

        var outcome = await coordinator.PairFromPayloadAsync("not-json-at-all {{{", "My Phone", CancellationToken.None);

        Assert.False(outcome.Success);
        Assert.NotNull(outcome.Error);
        Assert.Null(outcome.CompanyLabel);

        var all = await store.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task PayloadMissingRequiredField_ReturnsFailure_StoresNothing()
    {
        var handler = new CannedHandler("{\"success\":true,\"device_token\":\"device-tok-abc\"}");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store);

        // Missing "k" (sync key)
        var outcome = await coordinator.PairFromPayloadAsync(
            "{\"t\":\"pairing-tok-123\",\"u\":\"company-uid-456\",\"l\":\"Acme Corp\"}", "My Phone", CancellationToken.None);

        Assert.False(outcome.Success);
        Assert.Empty(await store.GetAllAsync());
    }

    [Fact]
    public async Task RedeemFailure_ExpiredToken_ReturnsFailure_StoresNothing()
    {
        // Server returns 410 Gone for an expired/already-used pairing token.
        var handler = new CannedHandler("{\"success\":false,\"error\":\"expired\"}", HttpStatusCode.Gone);
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store);

        var outcome = await coordinator.PairFromPayloadAsync(ValidPayload, "My Phone", CancellationToken.None);

        Assert.False(outcome.Success);
        Assert.NotNull(outcome.Error);

        var all = await store.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task RedeemFailure_NullDeviceToken_ReturnsFailure_StoresNothing()
    {
        // Server responds 200 but with no device_token (defensive case, shouldn't normally happen).
        var handler = new CannedHandler("{\"success\":false}");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store);

        var outcome = await coordinator.PairFromPayloadAsync(ValidPayload, "My Phone", CancellationToken.None);

        Assert.False(outcome.Success);
        Assert.Empty(await store.GetAllAsync());
    }

    [Fact]
    public async Task RedeemPassesPairingTokenAndDeviceLabel()
    {
        var handler = new CannedHandler(
            "{\"success\":true,\"device_token\":\"device-tok-abc\",\"company_uid\":\"company-uid-456\",\"company_label\":\"Acme Corp\"}");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store);

        await coordinator.PairFromPayloadAsync(ValidPayload, "My Phone", CancellationToken.None);

        Assert.NotNull(handler.LastBody);
        Assert.Contains("pairing-tok-123", handler.LastBody);
        Assert.Contains("My Phone", handler.LastBody);
    }
}
