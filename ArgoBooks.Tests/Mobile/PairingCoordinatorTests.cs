using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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

    /// <summary>
    /// Fakes the sync server's short-code endpoints: POST /pair/claim (once) and POST /pair/key
    /// (polled repeatedly). Captures the phone's RSA public key sent with the claim and, once the
    /// configured poll number is reached, encrypts the test's sync key to that real public key
    /// with <see cref="PairingKeyExchange.EncryptSyncKey"/> so the coordinator's real
    /// <c>DecryptSyncKey</c> call recovers it end-to-end.
    /// </summary>
    private sealed class CodePairingHandler : HttpMessageHandler
    {
        private readonly string _claimJson;
        private readonly HttpStatusCode _claimStatus;
        private readonly byte[]? _syncKeyBytes;
        private readonly int _keyReadyOnPollNumber;
        private readonly int _transientFailOnPollNumber;
        private readonly string? _keyCiphertextOverride;

        public string? LastClaimBody;
        public string? PhonePublicKeyBase64;
        public int KeyPollCount { get; private set; }

        public CodePairingHandler(
            string claimJson,
            HttpStatusCode claimStatus,
            byte[]? syncKeyBytes,
            int keyReadyOnPollNumber,
            int transientFailOnPollNumber = -1,
            string? keyCiphertextOverride = null)
        {
            _claimJson = claimJson;
            _claimStatus = claimStatus;
            _syncKeyBytes = syncKeyBytes;
            _keyReadyOnPollNumber = keyReadyOnPollNumber;
            _transientFailOnPollNumber = transientFailOnPollNumber;
            _keyCiphertextOverride = keyCiphertextOverride;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;

            if (path.EndsWith("/pair/claim"))
            {
                LastClaimBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
                if (LastClaimBody != null)
                {
                    using var doc = JsonDocument.Parse(LastClaimBody);
                    PhonePublicKeyBase64 = doc.RootElement.GetProperty("phone_public_key").GetString();
                }

                return new HttpResponseMessage(_claimStatus)
                { Content = new StringContent(_claimJson, Encoding.UTF8, "application/json") };
            }

            if (path.EndsWith("/pair/key"))
            {
                KeyPollCount++;

                if (KeyPollCount == _transientFailOnPollNumber)
                {
                    throw new HttpRequestException("Simulated transient network failure mid-poll.");
                }

                if (_keyCiphertextOverride != null)
                {
                    var overrideJson = JsonSerializer.Serialize(new { encrypted_sync_key = _keyCiphertextOverride });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(overrideJson, Encoding.UTF8, "application/json") };
                }

                if (_syncKeyBytes != null && KeyPollCount >= _keyReadyOnPollNumber)
                {
                    var ciphertext = PairingKeyExchange.EncryptSyncKey(PhonePublicKeyBase64!, _syncKeyBytes);
                    var json = JsonSerializer.Serialize(new { encrypted_sync_key = ciphertext });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(json, Encoding.UTF8, "application/json") };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("{\"pending\":true}", Encoding.UTF8, "application/json") };
            }

            throw new InvalidOperationException($"Unexpected request path: {path}");
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

    // --- PairFromCodeAsync (short pairing code) ---

    private static readonly byte[] TestSyncKeyBytes = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");

    [Fact]
    public async Task Code_EmptyAfterNormalize_ReturnsFailure_StoresNothing()
    {
        var handler = new CodePairingHandler("{}", HttpStatusCode.OK, syncKeyBytes: null, keyReadyOnPollNumber: 1);
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store);

        // "----" normalizes to empty (dashes aren't in the alphabet).
        var outcome = await coordinator.PairFromCodeAsync("----", "My Phone", CancellationToken.None);

        Assert.False(outcome.Success);
        Assert.Equal("Enter the code shown on your computer.", outcome.Error);
        Assert.Empty(await store.GetAllAsync());
    }

    [Fact]
    public async Task Code_NullClaim_ReturnsGenericInvalidCodeFailure_StoresNothing()
    {
        var handler = new CodePairingHandler("{\"error\":\"invalid\"}", HttpStatusCode.BadRequest, syncKeyBytes: null, keyReadyOnPollNumber: 1);
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store);

        var outcome = await coordinator.PairFromCodeAsync("ABCD1234", "My Phone", CancellationToken.None);

        Assert.False(outcome.Success);
        Assert.Equal("That code is not valid or has expired. Generate a new one on your computer.", outcome.Error);
        Assert.Empty(await store.GetAllAsync());
    }

    [Fact]
    public async Task Code_KeyNeverArrives_ReturnsTimeoutFailure_StoresNothing()
    {
        var claimJson = "{\"device_token\":\"device-tok-xyz\",\"company_uid\":\"company-uid-999\",\"company_label\":\"Beta Co\"}";
        var handler = new CodePairingHandler(claimJson, HttpStatusCode.OK, syncKeyBytes: null, keyReadyOnPollNumber: int.MaxValue);
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store)
        {
            PollInterval = TimeSpan.FromMilliseconds(5),
            PollTimeout = TimeSpan.FromMilliseconds(30)
        };

        var outcome = await coordinator.PairFromCodeAsync("ABCD1234", "My Phone", CancellationToken.None);

        Assert.False(outcome.Success);
        Assert.Equal(
            "Make sure Argo Books is open on your computer and the pairing screen is showing, then try again.",
            outcome.Error);
        Assert.Empty(await store.GetAllAsync());
        Assert.True(handler.KeyPollCount >= 1);
    }

    [Fact]
    public async Task Code_HappyPath_KeyArrivesOnSecondPoll_StoresDecryptedKeyAndSetsActive()
    {
        var claimJson = "{\"device_token\":\"device-tok-xyz\",\"company_uid\":\"company-uid-999\",\"company_label\":\"Beta Co\"}";
        var handler = new CodePairingHandler(claimJson, HttpStatusCode.OK, TestSyncKeyBytes, keyReadyOnPollNumber: 2);
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store)
        {
            PollInterval = TimeSpan.FromMilliseconds(5),
            PollTimeout = TimeSpan.FromSeconds(5)
        };

        var outcome = await coordinator.PairFromCodeAsync("ABCD-1234", "My Phone", CancellationToken.None);

        Assert.True(outcome.Success);
        Assert.Equal("Beta Co", outcome.CompanyLabel);
        Assert.Null(outcome.Error);
        Assert.Equal(2, handler.KeyPollCount);
        Assert.NotNull(handler.PhonePublicKeyBase64);

        var all = await store.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("company-uid-999", all[0].CompanyUid);
        Assert.Equal("Beta Co", all[0].CompanyLabel);
        Assert.Equal("device-tok-xyz", all[0].DeviceToken);
        Assert.Equal(Convert.ToBase64String(TestSyncKeyBytes), all[0].SyncKeyBase64);

        var active = await store.GetActiveAsync();
        Assert.NotNull(active);
        Assert.Equal("company-uid-999", active!.CompanyUid);
    }

    [Fact]
    public async Task Code_ClaimSendsNormalizedCodeAndPhonePublicKey()
    {
        var claimJson = "{\"device_token\":\"device-tok-xyz\",\"company_uid\":\"company-uid-999\",\"company_label\":\"Beta Co\"}";
        var handler = new CodePairingHandler(claimJson, HttpStatusCode.OK, TestSyncKeyBytes, keyReadyOnPollNumber: 1);
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store)
        {
            PollInterval = TimeSpan.FromMilliseconds(5),
            PollTimeout = TimeSpan.FromSeconds(5)
        };

        // Lowercase + dashes + a visually-ambiguous "O" (not in the alphabet) get normalized away/uppercased.
        await coordinator.PairFromCodeAsync("abcd-234O", "My Phone", CancellationToken.None);

        Assert.NotNull(handler.LastClaimBody);
        Assert.Contains("ABCD234", handler.LastClaimBody);
        Assert.Contains("My Phone", handler.LastClaimBody);
        Assert.NotNull(handler.PhonePublicKeyBase64);
    }

    [Fact]
    public async Task Code_TransientNetworkFailureMidPoll_IsTolerated_PairingStillSucceeds()
    {
        var claimJson = "{\"device_token\":\"device-tok-xyz\",\"company_uid\":\"company-uid-999\",\"company_label\":\"Beta Co\"}";
        // Poll 1 throws HttpRequestException (simulated WiFi/cellular handoff); poll 2 succeeds.
        var handler = new CodePairingHandler(
            claimJson, HttpStatusCode.OK, TestSyncKeyBytes, keyReadyOnPollNumber: 2, transientFailOnPollNumber: 1);
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store)
        {
            PollInterval = TimeSpan.FromMilliseconds(5),
            PollTimeout = TimeSpan.FromSeconds(5)
        };

        var outcome = await coordinator.PairFromCodeAsync("ABCD-1234", "My Phone", CancellationToken.None);

        Assert.True(outcome.Success);
        Assert.Equal("Beta Co", outcome.CompanyLabel);
        Assert.Null(outcome.Error);
        Assert.Equal(2, handler.KeyPollCount);

        var all = await store.GetAllAsync();
        Assert.Single(all);
        Assert.Equal(Convert.ToBase64String(TestSyncKeyBytes), all[0].SyncKeyBase64);
    }

    [Fact]
    public async Task Code_CorruptCiphertext_ReturnsGracefulFailure_StoresNothing()
    {
        var claimJson = "{\"device_token\":\"device-tok-xyz\",\"company_uid\":\"company-uid-999\",\"company_label\":\"Beta Co\"}";
        // Not valid base64 at all - exercises the FormatException path inside DecryptSyncKey.
        var handler = new CodePairingHandler(
            claimJson, HttpStatusCode.OK, syncKeyBytes: null, keyReadyOnPollNumber: 1,
            keyCiphertextOverride: "not-base64!!");
        var client = new MobileSyncClient(new HttpClient(handler), "http://localhost:5000");
        var store = new PairedCompanyStore(new InMemorySecureStore());
        var coordinator = new PairingCoordinator(client, store)
        {
            PollInterval = TimeSpan.FromMilliseconds(5),
            PollTimeout = TimeSpan.FromSeconds(5)
        };

        var outcome = await coordinator.PairFromCodeAsync("ABCD-1234", "My Phone", CancellationToken.None);

        Assert.False(outcome.Success);
        Assert.Equal("Something went wrong finishing the connection. Please try pairing again.", outcome.Error);
        Assert.Empty(await store.GetAllAsync());
    }
}
