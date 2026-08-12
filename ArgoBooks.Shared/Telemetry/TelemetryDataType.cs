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
    /// API usage events (Gemini, Exchange Rates, etc.).
    /// </summary>
    ApiUsage,

    /// <summary>
    /// Application errors.
    /// </summary>
    Error,

    /// <summary>
    /// Feature usage tracking.
    /// </summary>
    FeatureUsage,

    /// <summary>
    /// Who the user is: company name, business type, industry, country and currency.
    /// The one event type that is not anonymous, disclosed as such in the privacy policy.
    /// </summary>
    CompanyProfile,

    /// <summary>
    /// How long a launch took, split at the first moment the app could draw anything.
    /// </summary>
    Startup
}
