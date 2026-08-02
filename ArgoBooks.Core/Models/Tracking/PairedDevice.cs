namespace ArgoBooks.Core.Models.Tracking;

/// <summary>A phone paired to this company for mobile sync.</summary>
public class PairedDevice
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;      // local id, e.g. PDV-001
    [JsonPropertyName("serverDeviceId")] public int ServerDeviceId { get; set; } // id from mobile_sync_devices
    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;
    [JsonPropertyName("lastSeenAt")] public DateTime? LastSeenAt { get; set; }
}
