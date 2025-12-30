using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Manages Google OAuth 2.0 credentials for Google Sheets and Drive API access.
/// </summary>
public static class GoogleCredentialsManager
{
    private static readonly string[] Scopes =
    [
        "https://www.googleapis.com/auth/spreadsheets",
        "https://www.googleapis.com/auth/drive.file"
    ];

    private static UserCredential? _cachedCredential;

    /// <summary>
    /// Gets user credentials using OAuth from environment variables.
    /// First time will prompt user to authenticate in browser.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user credential for Google API access.</returns>
    /// <exception cref="InvalidOperationException">Thrown when credentials are not configured.</exception>
    public static async Task<UserCredential> GetUserCredentialAsync(CancellationToken cancellationToken = default)
    {
        // Return cached credential if available and not expired
        if (_cachedCredential != null)
        {
            return _cachedCredential;
        }

        var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException(
                "Google OAuth credentials not configured. Please set GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET environment variables.");
        }

        var secrets = new ClientSecrets
        {
            ClientId = clientId,
            ClientSecret = clientSecret
        };

        // Store token in AppData
        var credPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ArgoBooks",
            "google_token"
        );

        _cachedCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            Scopes,
            "user",
            cancellationToken,
            new FileDataStore(credPath, true)
        );

        return _cachedCredential;
    }

    /// <summary>
    /// Clears the cached credential, forcing re-authentication on next use.
    /// </summary>
    public static void ClearCache()
    {
        _cachedCredential = null;
    }

    /// <summary>
    /// Checks if Google OAuth credentials are configured in environment variables.
    /// </summary>
    /// <returns>True if credentials are configured, false otherwise.</returns>
    public static bool AreCredentialsConfigured()
    {
        var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
        return !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret);
    }
}
