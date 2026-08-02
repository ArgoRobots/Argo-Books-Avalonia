namespace ArgoBooks.Core.Models;

/// <summary>Per-company mobile sync settings. Persisted in appSettings.json inside the .argo file.</summary>
public class MobileSyncSettings
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("companyUid")] public string? CompanyUid { get; set; }
    [JsonPropertyName("syncKey")] public string? SyncKeyBase64 { get; set; }
    [JsonPropertyName("notifyOnCapture")] public bool NotifyOnCapture { get; set; } = true;
    [JsonPropertyName("lastSyncTime")] public DateTime? LastSyncTime { get; set; }

    [JsonIgnore] public bool IsConfigured => Enabled && !string.IsNullOrEmpty(CompanyUid) && !string.IsNullOrEmpty(SyncKeyBase64);
}
