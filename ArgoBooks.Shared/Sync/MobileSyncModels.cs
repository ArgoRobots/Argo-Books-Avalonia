using System.Text.Json.Serialization;

namespace ArgoBooks.Shared.Sync;

/// <summary>
/// Response from POST /api/sync/pair/redeem when a pairing token is successfully redeemed.
/// </summary>
public sealed class PairResult
{
    /// <summary>The device token the phone uses for future authenticated requests.</summary>
    [JsonPropertyName("device_token")]
    public string DeviceToken { get; init; } = string.Empty;

    /// <summary>The company UID associated with this pairing.</summary>
    [JsonPropertyName("company_uid")]
    public string CompanyUid { get; init; } = string.Empty;

    /// <summary>The company label (friendly name) for display on the phone.</summary>
    [JsonPropertyName("company_label")]
    public string CompanyLabel { get; init; } = string.Empty;
}

/// <summary>
/// Response from POST /api/sync/snapshot/get when the phone fetches the encrypted snapshot.
/// </summary>
public sealed class SnapshotResult
{
    /// <summary>The AES-256-GCM encrypted snapshot blob (base64-encoded).</summary>
    [JsonPropertyName("ciphertext")]
    public string Ciphertext { get; init; } = string.Empty;

    /// <summary>ISO 8601 timestamp (UTC) when the snapshot was generated on the desktop.</summary>
    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; init; } = string.Empty;
}
