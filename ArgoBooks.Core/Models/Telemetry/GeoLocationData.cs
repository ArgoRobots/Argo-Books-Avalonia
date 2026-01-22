namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// Anonymous geographic location data derived from IP address.
/// </summary>
public class GeoLocationData
{
    /// <summary>
    /// Country name (e.g., "United States").
    /// </summary>
    public string Country { get; set; } = "Unknown";

    /// <summary>
    /// ISO country code (e.g., "US").
    /// </summary>
    public string CountryCode { get; set; } = "Unknown";

    /// <summary>
    /// Region or state name.
    /// </summary>
    public string Region { get; set; } = "Unknown";

    /// <summary>
    /// City name.
    /// </summary>
    public string City { get; set; } = "Unknown";

    /// <summary>
    /// Timezone identifier (e.g., "America/New_York").
    /// </summary>
    public string Timezone { get; set; } = "Unknown";

    /// <summary>
    /// One-way hashed IP address (SHA256, truncated to 16 chars).
    /// Cannot be reversed to identify the user.
    /// </summary>
    public string? HashedIp { get; set; }
}
