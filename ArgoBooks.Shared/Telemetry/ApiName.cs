namespace ArgoBooks.Core.Models.Telemetry;

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
    ReceiptScanProxy
}
