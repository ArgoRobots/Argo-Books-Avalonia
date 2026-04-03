namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// API usage event for tracking external API calls.
/// </summary>
public class ApiUsageEvent : TelemetryEvent
{
    /// <inheritdoc />
    public override TelemetryDataType DataType => TelemetryDataType.ApiUsage;

    /// <summary>
    /// Name of the API being used.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ApiName ApiName { get; set; }

    /// <summary>
    /// Specific model used (for AI APIs like Gemini).
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Duration of the API call in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Number of tokens used (for AI APIs).
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Whether the API call was successful.
    /// </summary>
    public bool Success { get; set; } = true;
}

/// <summary>
/// External APIs used by the application.
/// </summary>
public enum ApiName
{
    /// <summary>
    /// Gemini API for AI features.
    /// </summary>
    Gemini,

    /// <summary>
    /// Open Exchange Rates API for currency conversion.
    /// </summary>
    OpenExchangeRates,

    /// <summary>
    /// Google Sheets API for spreadsheet export.
    /// </summary>
    GoogleSheets,

    /// <summary>
    /// Server proxy for receipt scanning.
    /// </summary>
    ReceiptScanProxy,

    /// <summary>
    /// Microsoft Translator API.
    /// </summary>
    MicrosoftTranslator
}
