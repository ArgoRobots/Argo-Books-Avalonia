using System.Text;

namespace ArgoBooks.Core.Services.Sync;

/// <summary>Calls the /api/sync owner endpoints. Desktop authenticates via LicenseAuthHelper.</summary>
public class SyncService
{
    private static readonly string Base = $"{ApiConfig.BaseUrl}/api/sync";
    private readonly HttpClient _http;

    public SyncService(HttpClient? http = null) => _http = http ?? new HttpClient();

    private async Task<JsonElement> PostAsync(string path, object body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, Base + path)
        { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
        LicenseAuthHelper.AddAuthHeaders(req);
        using var resp = await _http.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        resp.EnsureSuccessStatusCode();
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    public async Task<PairingCreation?> CreatePairingAsync(string companyUid, string companyLabel, CancellationToken ct)
    {
        var r = await PostAsync("/pair/create", new { company_uid = companyUid, company_label = companyLabel }, ct);
        if (!r.TryGetProperty("pairing_token", out var t) || t.GetString() is not { } token)
            return null;
        var shortCode = r.TryGetProperty("short_code", out var sc) ? sc.GetString() ?? "" : "";
        return new PairingCreation(token, shortCode);
    }

    public async Task<PairingStatusResult?> GetPairingStatusAsync(string pairingToken, CancellationToken ct)
    {
        var r = await PostAsync("/pair/status", new { pairing_token = pairingToken }, ct);
        if (!r.TryGetProperty("status", out var s) || s.GetString() is not { } status)
            return null;
        var phonePublicKey = r.TryGetProperty("phone_public_key", out var pk) && pk.ValueKind == JsonValueKind.String
            ? pk.GetString()
            : null;
        return new PairingStatusResult(status, phonePublicKey);
    }

    public Task DeliverKeyAsync(string pairingToken, string encryptedSyncKey, CancellationToken ct)
        => PostAsync("/pair/deliver", new { pairing_token = pairingToken, encrypted_sync_key = encryptedSyncKey }, ct);

    public Task UploadSnapshotAsync(string companyUid, string ciphertext, CancellationToken ct)
        => PostAsync("/snapshot/put", new { company_uid = companyUid, ciphertext }, ct);

    public async Task<List<QueueItem>> PullQueueAsync(string companyUid, CancellationToken ct)
    {
        var r = await PostAsync("/queue/pull", new { company_uid = companyUid }, ct);
        var list = new List<QueueItem>();
        if (r.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            foreach (var it in items.EnumerateArray())
                list.Add(new QueueItem { Id = it.GetProperty("id").GetInt32(), Ciphertext = it.GetProperty("ciphertext").GetString() ?? "" });
        return list;
    }

    public Task AckQueueAsync(string companyUid, IReadOnlyList<int> ids, CancellationToken ct)
        => PostAsync("/queue/ack", new { company_uid = companyUid, ids }, ct);

    public async Task<List<ServerDevice>> ListDevicesAsync(string companyUid, CancellationToken ct)
    {
        var r = await PostAsync("/devices/list", new { company_uid = companyUid }, ct);
        var list = new List<ServerDevice>();
        if (r.TryGetProperty("devices", out var d) && d.ValueKind == JsonValueKind.Array)
            foreach (var it in d.EnumerateArray())
                list.Add(new ServerDevice
                {
                    Id = it.GetProperty("id").GetInt32(),
                    DeviceLabel = it.GetProperty("device_label").GetString() ?? "",
                    LastSeenAt = it.TryGetProperty("last_seen_at", out var ls) && ls.ValueKind == JsonValueKind.String ? DateTime.Parse(ls.GetString()!) : null
                });
        return list;
    }

    public Task RevokeDeviceAsync(string companyUid, int serverDeviceId, CancellationToken ct)
        => PostAsync("/devices/revoke", new { company_uid = companyUid, device_id = serverDeviceId }, ct);
}
