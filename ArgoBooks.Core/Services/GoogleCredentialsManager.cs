using System.Net.Http.Headers;
using System.Text;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Manages Google OAuth 2.0 credentials via the argorobots.com server proxy.
/// The server handles OAuth token storage and refresh.
/// Google Sheets is a free feature — authentication uses device ID.
/// </summary>
public static class GoogleCredentialsManager
{
    private const string AuthEndpoint = "https://argorobots.com/api/google/auth.php";

    /// <summary>
    /// Checks if Google API access is configured (always true — free feature).
    /// </summary>
    public static bool AreCredentialsConfigured() => true;

    /// <summary>
    /// Initiates the Google OAuth flow by requesting an auth URL from the server.
    /// The user should be directed to open this URL in their browser.
    /// </summary>
    public static async Task<string?> InitiateAuthAsync(CancellationToken cancellationToken = default)
    {
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
    /// Checks whether the user has completed Google OAuth and has valid tokens stored on the server.
    /// </summary>
    public static async Task<bool> CheckAuthStatusAsync(CancellationToken cancellationToken = default)
    {
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
    /// Revokes the Google OAuth connection for this user.
    /// </summary>
    public static async Task<bool> RevokeAuthAsync(CancellationToken cancellationToken = default)
    {
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
    /// Adds device ID auth header for Google API requests (free feature).
    /// </summary>
    internal static void AddAuthHeaders(HttpRequestMessage request)
    {
        var deviceId = LicenseService.Instance?.GetDeviceId();
        if (!string.IsNullOrEmpty(deviceId))
        {
            request.Headers.Add("X-Device-Id", deviceId);
        }
    }
}
