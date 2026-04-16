using System.Net.Http.Headers;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Shared helper for adding license key authentication headers to API requests.
/// Premium API endpoints (AI, invoice email, receipt scanning, telemetry) use the license key.
/// Note: Exchange rates are a free feature and do NOT require a license key.
/// </summary>
public static class LicenseAuthHelper
{
    /// <summary>
    /// Returns the current license key, or null if not available.
    /// </summary>
    public static string? GetLicenseKey() => LicenseService.Instance?.GetLicenseKey();

    /// <summary>
    /// Returns the current device ID, or null if not available.
    /// </summary>
    public static string? GetDeviceId() => LicenseService.Instance?.GetDeviceId();

    /// <summary>
    /// Whether authentication is available (license key for premium, or device ID for free users).
    /// </summary>
    public static bool IsConfigured => !string.IsNullOrEmpty(GetLicenseKey()) || !string.IsNullOrEmpty(GetDeviceId());

    /// <summary>
    /// Adds authentication headers to an HTTP request.
    /// Uses license key if available, otherwise device ID.
    /// </summary>
    public static void AddAuthHeaders(HttpRequestMessage request)
    {
        var licenseKey = GetLicenseKey();
        if (!string.IsNullOrEmpty(licenseKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", licenseKey);
            request.Headers.Add("X-License-Key", licenseKey);
        }

        var deviceId = GetDeviceId();
        if (!string.IsNullOrEmpty(deviceId))
        {
            request.Headers.Add("X-Device-Id", deviceId);
        }
    }
}
