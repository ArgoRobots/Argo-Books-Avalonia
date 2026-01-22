namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// Types of telemetry events that can be collected.
/// </summary>
public enum TelemetryDataType
{
    /// <summary>
    /// Session start or end events.
    /// </summary>
    Session,

    /// <summary>
    /// Export operations (Excel, Google Sheets, PDF, etc.).
    /// </summary>
    Export,

    /// <summary>
    /// API usage events (OpenAI, Exchange Rates, etc.).
    /// </summary>
    ApiUsage,

    /// <summary>
    /// Application errors.
    /// </summary>
    Error,

    /// <summary>
    /// Feature usage tracking.
    /// </summary>
    FeatureUsage
}
