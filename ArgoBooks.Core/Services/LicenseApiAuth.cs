namespace ArgoBooks.Core.Services;

/// <summary>
/// Desktop adapter over <see cref="LicenseAuthHelper"/> for the <see cref="IApiAuth"/> seam
/// consumed by the (now-shared) <see cref="GeminiReceiptScannerService"/>. The mobile app will
/// pass a different <see cref="IApiAuth"/> implementation over its own device pairing token.
/// </summary>
public class LicenseApiAuth : IApiAuth
{
    /// <inheritdoc />
    public bool IsConfigured => LicenseAuthHelper.IsConfigured;

    /// <inheritdoc />
    public void AddAuthHeaders(HttpRequestMessage request) => LicenseAuthHelper.AddAuthHeaders(request);
}
