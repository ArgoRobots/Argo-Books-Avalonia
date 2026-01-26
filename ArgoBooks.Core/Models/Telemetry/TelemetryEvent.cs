namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// Base class for all telemetry events.
/// </summary>
public abstract class TelemetryEvent
{
    /// <summary>
    /// Unique identifier for this event (used for deduplication).
    /// </summary>
    public string DataId { get; set; } = Guid.NewGuid().ToString("N")[..16];

    /// <summary>
    /// When the event occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Type of telemetry data.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public abstract TelemetryDataType DataType { get; }

    /// <summary>
    /// Application version.
    /// </summary>
    public string? AppVersion { get; set; }

    /// <summary>
    /// Platform identifier (e.g., "Windows", "macOS", "Linux").
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>
    /// User agent string (OS version info).
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Geographic location data (anonymous).
    /// </summary>
    public GeoLocationData? GeoLocation { get; set; }

    /// <summary>
    /// Whether this event has been uploaded to the server.
    /// </summary>
    [JsonIgnore]
    public bool IsUploaded { get; set; }
}
