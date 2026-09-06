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
    /// Server proxy for receipt scanning.
    /// </summary>
    ReceiptScanProxy,

    /// <summary>
    /// The bulk exchange-rate endpoint, which prices many dates in one request. Kept separate from
    /// <see cref="OpenExchangeRates"/> so the dashboard shows how often the bulk call is doing its
    /// job versus how often the per-date repair path is picking up after it. Logged under one name,
    /// a failing bulk call is indistinguishable from normal traffic.
    /// </summary>
    OpenExchangeRatesBatch
}
