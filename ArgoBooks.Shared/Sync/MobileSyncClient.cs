using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ArgoBooks.Shared.Sync;

/// <summary>
/// HTTP client for the mobile app's sync endpoints. Authenticates as a paired device
/// (via X-Sync-Device-Token header) rather than as the desktop owner.
/// Lives in Shared so the Android app can reference it without pulling in ArgoBooks.Core.
/// </summary>
public class MobileSyncClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    /// <summary>
    /// Creates a new mobile sync client.
    /// </summary>
    /// <param name="http">HttpClient to use for requests (or null for a default instance).</param>
    /// <param name="baseUrl">Base URL of the sync server (e.g. http://localhost:5000 or https://api.argorobots.com).</param>
    public MobileSyncClient(HttpClient? http, string baseUrl)
    {
        _http = http ?? new HttpClient();
        _baseUrl = baseUrl;
    }

    private async Task<JsonElement> PostAsync(string path, object body, string? deviceToken = null, CancellationToken ct = default)
    {
        var fullUrl = _baseUrl + "/api/sync" + path;
        using var req = new HttpRequestMessage(HttpMethod.Post, fullUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        if (deviceToken != null)
        {
            req.Headers.Add("X-Sync-Device-Token", deviceToken);
        }

        using var resp = await _http.SendAsync(req, ct);

        // Return null on 404 for snapshot get (no snapshot yet)
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        var text = await resp.Content.ReadAsStringAsync(ct);
        resp.EnsureSuccessStatusCode();
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    /// <summary>
    /// Redeems a pairing token (from the QR code) to get a device token and company info.
    /// This call is unauthenticated (no device token yet).
    /// </summary>
    public async Task<PairResult?> RedeemPairingAsync(string pairingToken, string deviceLabel, CancellationToken ct)
    {
        var json = await PostAsync("/pair/redeem", new { pairing_token = pairingToken, device_label = deviceLabel }, ct: ct);

        if (json.ValueKind == default)
        {
            return null;
        }

        var result = new PairResult
        {
            DeviceToken = json.TryGetProperty("device_token", out var dt) ? (dt.GetString() ?? string.Empty) : string.Empty,
            CompanyUid = json.TryGetProperty("company_uid", out var cu) ? (cu.GetString() ?? string.Empty) : string.Empty,
            CompanyLabel = json.TryGetProperty("company_label", out var cl) ? (cl.GetString() ?? string.Empty) : string.Empty
        };

        return result;
    }

    /// <summary>
    /// Fetches the encrypted snapshot from the server using the device token.
    /// Returns null if the server returns 404 (no snapshot yet).
    /// </summary>
    public async Task<SnapshotResult?> GetSnapshotAsync(string deviceToken, CancellationToken ct)
    {
        var json = await PostAsync("/snapshot/get", new { }, deviceToken, ct);

        if (json.ValueKind == default)
        {
            return null;
        }

        var result = new SnapshotResult
        {
            Ciphertext = json.TryGetProperty("ciphertext", out var c) ? (c.GetString() ?? string.Empty) : string.Empty,
            UpdatedAt = json.TryGetProperty("updated_at", out var u) ? (u.GetString() ?? string.Empty) : string.Empty
        };

        return result;
    }

    /// <summary>
    /// Pushes a captured transaction to the server queue (from a receipt scan or manual entry).
    /// </summary>
    public async Task PushCaptureAsync(string deviceToken, string ciphertext, CancellationToken ct)
    {
        await PostAsync("/queue/push", new { ciphertext }, deviceToken, ct);
    }
}
