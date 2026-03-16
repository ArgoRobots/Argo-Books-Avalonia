using System.Net.Http.Headers;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Shared helper for adding license key authentication headers to API requests.
/// All premium API endpoints (exchange rates, AI, invoice email, telemetry) use the license key.
/// </summary>
public static class LicenseAuthHelper
{
    /// <summary>
    /// Returns the current license key, or null if not available.
    /// </summary>
    public static string? GetLicenseKey() => LicenseService.Instance?.GetLicenseKey();

    /// <summary>
    /// Whether a license key is configured and available.
    /// </summary>
    public static bool IsConfigured => !string.IsNullOrEmpty(GetLicenseKey());

    /// <summary>
    /// Adds license key authentication headers to an HTTP request.
    /// </summary>
    public static void AddAuthHeaders(HttpRequestMessage request)
    {
        var licenseKey = GetLicenseKey();
        if (!string.IsNullOrEmpty(licenseKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", licenseKey);
            request.Headers.Add("X-License-Key", licenseKey);
        }
    }
}
