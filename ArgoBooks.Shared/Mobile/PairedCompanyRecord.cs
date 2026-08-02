using System.Text.Json.Serialization;

namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Represents a paired company record stored in secure storage.
/// All fields are serialized/deserialized to JSON for storage.
/// </summary>
public class PairedCompanyRecord
{
    /// <summary>
    /// The unique company identifier from the sync system.
    /// </summary>
    [JsonPropertyName("companyUid")]
    public string CompanyUid { get; set; } = string.Empty;

    /// <summary>
    /// The human-readable company label (e.g., "Acme Corp").
    /// </summary>
    [JsonPropertyName("companyLabel")]
    public string CompanyLabel { get; set; } = string.Empty;

    /// <summary>
    /// The device token used to authenticate snapshot and queue requests.
    /// </summary>
    [JsonPropertyName("deviceToken")]
    public string DeviceToken { get; set; } = string.Empty;

    /// <summary>
    /// The sync key in Base64 format, used to decrypt snapshots.
    /// </summary>
    [JsonPropertyName("syncKeyBase64")]
    public string SyncKeyBase64 { get; set; } = string.Empty;
}
