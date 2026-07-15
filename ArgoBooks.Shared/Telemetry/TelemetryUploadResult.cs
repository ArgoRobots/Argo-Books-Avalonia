namespace ArgoBooks.Core.Services;

/// <summary>
/// Result of a telemetry upload operation.
/// </summary>
public class TelemetryUploadResult
{
    public bool Success { get; set; }
    public int EventsUploaded { get; set; }
    public int TotalPending { get; set; }
    public string? ErrorMessage { get; set; }
    public int? HttpStatusCode { get; set; }
    /// <summary>
    /// Path to the local backup file saved when upload fails. Null if upload succeeded or no backup was needed.
    /// </summary>
    public string? BackupFilePath { get; set; }
}
