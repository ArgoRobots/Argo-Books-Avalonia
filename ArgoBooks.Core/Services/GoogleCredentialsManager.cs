using System.Net.Http.Headers;
using System.Text;
using ArgoBooks.Core.Models.Portal;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Manages Google OAuth 2.0 credentials via the argorobots.com server proxy.
/// The server handles OAuth token storage and refresh.
/// Supports authentication via portal API key or premium license key.
/// </summary>
public static class GoogleCredentialsManager
{
    private const string AuthEndpoint = "https://argorobots.com/api/google/auth.php";

    /// <summary>
    /// Checks if Google API access is configured (portal or license key must be available).
    /// </summary>
    public static bool AreCredentialsConfigured()
    {
        return PortalSettings.IsConfigured || HasLicenseKey();
    }

    /// <summary>
    /// Initiates the Google OAuth flow by requesting an auth URL from the server.
    /// The user should be directed to open this URL in their browser.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The OAuth URL to open in a browser, or null if the request failed.</returns>
    public static async Task<string?> InitiateAuthAsync(CancellationToken cancellationToken = default)
    {
        if (!AreCredentialsConfigured())
            return null;

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var requestBody = new { action = "initiate" };
        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, AuthEndpoint);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        AddAuthHeaders(request);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("success", out var success) && success.GetBoolean()
            && root.TryGetProperty("authUrl", out var authUrl))
        {
            return authUrl.GetString();
        }

        return null;
    }

    /// <summary>
    /// Checks whether the company has completed Google OAuth and has valid tokens stored on the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the company has Google tokens, false otherwise.</returns>
    public static async Task<bool> CheckAuthStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!AreCredentialsConfigured())
            return false;

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var requestBody = new { action = "status" };
        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, AuthEndpoint);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        AddAuthHeaders(request);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return false;

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("success", out var success) && success.GetBoolean()
            && root.TryGetProperty("authenticated", out var authenticated))
        {
            return authenticated.GetBoolean();
        }

        return false;
    }

    /// <summary>
    /// Revokes the Google OAuth connection for this company.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if revocation was successful.</returns>
    public static async Task<bool> RevokeAuthAsync(CancellationToken cancellationToken = default)
    {
        if (!AreCredentialsConfigured())
            return false;

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var requestBody = new { action = "revoke" };
        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, AuthEndpoint);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        AddAuthHeaders(request);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return false;

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        return root.TryGetProperty("success", out var success) && success.GetBoolean();
    }

    /// <summary>
    /// Adds the appropriate auth headers (portal API key or license key).
    /// </summary>
    internal static void AddAuthHeaders(HttpRequestMessage request)
    {
        if (PortalSettings.IsConfigured)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", PortalSettings.ApiKey);
            request.Headers.Add("X-Api-Key", PortalSettings.ApiKey);
        }
        else
        {
            var licenseKey = GetLicenseKey();
            if (!string.IsNullOrEmpty(licenseKey))
            {
                request.Headers.Add("X-License-Key", licenseKey);
            }
        }
    }

    private static bool HasLicenseKey()
    {
        return !string.IsNullOrEmpty(GetLicenseKey());
    }

    private static string? GetLicenseKey()
    {
        return App.LicenseService?.GetLicenseKey();
    }
}
