using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Statistics about stored telemetry data.
/// </summary>
public class TelemetryStatistics
{
    public int TotalEvents { get; set; }
    public int PendingEvents { get; set; }
    public int UploadedEvents { get; set; }
    public Dictionary<TelemetryDataType, int> EventsByType { get; set; } = new();
    public DateTime? OldestEventTime { get; set; }
    public DateTime? NewestEventTime { get; set; }
    public DateTime? LastUploadTime { get; set; }
    public int TotalEventsEverUploaded { get; set; }
}
