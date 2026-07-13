namespace ArgoBooks.Core.Services;

/// <summary>
/// Adds authentication headers to a request bound for the argorobots.com API. Lets
/// <see cref="GeminiReceiptScannerService"/> stay platform-agnostic: the desktop passes an
/// adapter over its license key / device ID (<c>LicenseApiAuth</c>), while the mobile app passes
/// an adapter over its own device pairing token.
/// </summary>
public interface IApiAuth
{
    /// <summary>
    /// Whether authentication is currently available (so a call is worth attempting).
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Adds authentication headers to an outgoing HTTP request.
    /// </summary>
    void AddAuthHeaders(HttpRequestMessage request);
}
