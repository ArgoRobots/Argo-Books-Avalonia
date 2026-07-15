using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services.Sync;
using ArgoBooks.Shared.Mobile;
using ArgoBooks.Shared.Sync;
using Xunit;

namespace ArgoBooks.Tests.Mobile;

/// <summary>
/// Unit tests for <see cref="SnapshotStore"/>: builds a real <see cref="MobileSnapshot"/> via the
/// desktop's <see cref="SnapshotBuilder"/>, encrypts it the same way the server would store it,
/// and feeds it through a fake <see cref="MobileSyncClient"/> HTTP handler to confirm the phone
/// decrypts/deserializes back to the same data.
/// </summary>
public class SnapshotStoreTests
{
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

    private sealed class InMemorySnapshotCache : ISnapshotCache
    {
        private readonly Dictionary<string, string> _cache = new();

        public Task SaveAsync(string companyUid, string json)
        {
            _cache[companyUid] = json;
            return Task.CompletedTask;
        }

        public Task<string?> LoadAsync(string companyUid)
        {
            _cache.TryGetValue(companyUid, out var json);
            return Task.FromResult(json);
        }
    }

    private sealed class CannedHandler : HttpMessageHandler
    {
        private readonly string _json;
        private readonly HttpStatusCode _statusCode;

        public CannedHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _json = json;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_statusCode)
            { Content = new StringContent(_json, Encoding.UTF8, "application/json") });
    }

    private static async Task<PairedCompanyStore> SeedPairedStoreAsync(string companyUid, string deviceToken, string syncKeyBase64)
    {
        var store = new PairedCompanyStore(new InMemorySecureStore());
        await store.SaveAsync(new PairedCompanyRecord
        {
            CompanyUid = companyUid,
            CompanyLabel = "Acme Co",
            DeviceToken = deviceToken,
            SyncKeyBase64 = syncKeyBase64,
        });
        await store.SetActiveAsync(companyUid);
        return store;
    }

    [Fact]
    public async Task RefreshAsync_decrypts_and_deserializes_the_desktop_snapshot()
    {
        var data = new CompanyData();
        data.Revenues.Add(new Revenue { Id = "REV-1", Date = new DateTime(2026, 1, 1), Description = "Consulting", Total = 60m });
        data.Expenses.Add(new Expense { Id = "EXP-1", Date = new DateTime(2026, 1, 3), Description = "Office supplies", Total = 40m });
        var snapshot = SnapshotBuilder.Build(data);

        var syncKey = SyncCrypto.GenerateSyncKey();
        var plaintext = SnapshotBuilder.Serialize(snapshot);
        var ciphertext = SyncCrypto.Encrypt(plaintext, syncKey);

        var handler = new CannedHandler(
            JsonSerializer.Serialize(new { ciphertext, updated_at = "2026-07-13T10:00:00Z" }));
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var pairedStore = await SeedPairedStoreAsync("company-1", "device-token-1", syncKey);
        var cache = new InMemorySnapshotCache();

        var store = new SnapshotStore(client, pairedStore, cache);
        var state = await store.RefreshAsync(CancellationToken.None);

        Assert.Equal(SnapshotStatus.Loaded, state.Status);
        Assert.False(state.IsStale);
        Assert.NotNull(state.Snapshot);
        Assert.Equal(60m, state.Snapshot!.Dashboard.MoneyIn);
        Assert.Equal(40m, state.Snapshot.Dashboard.MoneyOut);
        Assert.Equal(20m, state.Snapshot.Dashboard.Profit);
        Assert.Single(state.Snapshot.Revenue);
        Assert.Single(state.Snapshot.Expenses);
        Assert.NotNull(state.LastSyncedAt);
        Assert.Same(state, store.Current);

        // Also cached for offline viewing.
        Assert.NotNull(await cache.LoadAsync("company-1"));
    }

    [Fact]
    public async Task RefreshAsync_returns_waiting_state_on_404_with_no_cache()
    {
        var handler = new CannedHandler("{}", HttpStatusCode.NotFound);
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var pairedStore = await SeedPairedStoreAsync("company-2", "device-token-2", SyncCrypto.GenerateSyncKey());
        var store = new SnapshotStore(client, pairedStore, new InMemorySnapshotCache());

        var state = await store.RefreshAsync(CancellationToken.None);

        Assert.Equal(SnapshotStatus.WaitingForFirstSync, state.Status);
        Assert.Null(state.Snapshot);
    }

    [Fact]
    public async Task RefreshAsync_returns_not_paired_when_no_active_company()
    {
        var handler = new CannedHandler("{}", HttpStatusCode.NotFound);
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var pairedStore = new PairedCompanyStore(new InMemorySecureStore());
        var store = new SnapshotStore(client, pairedStore, new InMemorySnapshotCache());

        var state = await store.RefreshAsync(CancellationToken.None);

        Assert.Equal(SnapshotStatus.NotPaired, state.Status);
    }

    [Fact]
    public async Task RefreshAsync_falls_back_to_cache_when_payload_is_corrupt()
    {
        var handler = new CannedHandler(
            JsonSerializer.Serialize(new { ciphertext = "not-valid-base64-ciphertext!!", updated_at = "2026-07-13T10:00:00Z" }));
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var pairedStore = await SeedPairedStoreAsync("company-3", "device-token-3", SyncCrypto.GenerateSyncKey());

        var cachedSnapshot = SnapshotBuilder.Build(new CompanyData());
        var cache = new InMemorySnapshotCache();
        await cache.SaveAsync("company-3", Encoding.UTF8.GetString(SnapshotBuilder.Serialize(cachedSnapshot)));

        var store = new SnapshotStore(client, pairedStore, cache);
        var state = await store.RefreshAsync(CancellationToken.None);

        Assert.Equal(SnapshotStatus.Loaded, state.Status);
        Assert.True(state.IsStale);
        Assert.NotNull(state.Error);
    }

    [Fact]
    public async Task LoadCachedAsync_returns_cached_snapshot_marked_stale()
    {
        var snapshot = SnapshotBuilder.Build(new CompanyData());
        var pairedStore = await SeedPairedStoreAsync("company-4", "device-token-4", SyncCrypto.GenerateSyncKey());
        var cache = new InMemorySnapshotCache();
        await cache.SaveAsync("company-4", Encoding.UTF8.GetString(SnapshotBuilder.Serialize(snapshot)));

        var handler = new CannedHandler("{}", HttpStatusCode.NotFound); // never called by LoadCachedAsync
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new SnapshotStore(client, pairedStore, cache);

        var state = await store.LoadCachedAsync();

        Assert.Equal(SnapshotStatus.Loaded, state.Status);
        Assert.True(state.IsStale);
        Assert.NotNull(state.Snapshot);
    }

    [Fact]
    public async Task LoadCachedAsync_returns_waiting_state_when_nothing_cached()
    {
        var pairedStore = await SeedPairedStoreAsync("company-5", "device-token-5", SyncCrypto.GenerateSyncKey());
        var handler = new CannedHandler("{}", HttpStatusCode.NotFound);
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new SnapshotStore(client, pairedStore, new InMemorySnapshotCache());

        var state = await store.LoadCachedAsync();

        Assert.Equal(SnapshotStatus.WaitingForFirstSync, state.Status);
    }
}
