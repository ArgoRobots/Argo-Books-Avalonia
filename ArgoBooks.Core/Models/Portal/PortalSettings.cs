using ArgoBooks.Core.Services;

namespace ArgoBooks.Core.Models.Portal;

/// <summary>
/// Settings for the payment portal integration.
/// Stored in company data (persisted in .argo file).
/// API key is loaded from .env file for security.
/// </summary>
public class PortalSettings
{
    /// <summary>
    /// The payment portal API base URL.
    /// </summary>
    public static readonly string ApiBaseUrl = $"{ApiConfig.BaseUrl}/api/portal";

    /// <summary>
    /// Environment variable name for the portal API key (per-company, obtained during registration).
    /// </summary>
    public const string ApiKeyEnvVar = "PAYMENT_PORTAL_API_KEY";

    /// <summary>
    /// Gets the active portal API key (from DotEnv, which is loaded per-company).
    /// </summary>
    [JsonIgnore]
    public static string ApiKey => DotEnv.Get(ApiKeyEnvVar);

    /// <summary>
    /// Whether the portal API is configured (API key is present).
    /// </summary>
    [JsonIgnore]
    public static bool IsConfigured => DotEnv.HasValue(ApiKeyEnvVar);

    /// <summary>
    /// Per-company API key persisted in the .argo file.
    /// On company open this is loaded into DotEnv so that the static ApiKey property works.
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string? PersistedApiKey { get; set; }

    /// <summary>
    /// Loads this company's API key into the process-level DotEnv cache.
    /// Call on company open.
    /// </summary>
    public static void ActivateApiKey(PortalSettings? settings)
    {
        var key = settings?.PersistedApiKey;
        if (!string.IsNullOrEmpty(key))
            DotEnv.SetInMemory(ApiKeyEnvVar, key);
        else
            DotEnv.Unset(ApiKeyEnvVar);
    }

    /// <summary>
    /// Clears the API key from the process-level DotEnv cache.
    /// Call on company close.
    /// </summary>
    public static void DeactivateApiKey()
    {
        DotEnv.Unset(ApiKeyEnvVar);
    }

    /// <summary>
    /// Auto-sync interval in minutes. 0 = manual sync only.
    /// </summary>
    [JsonPropertyName("autoSyncIntervalMinutes")]
    public int AutoSyncIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Whether to show a notification when new online payments are received.
    /// </summary>
    [JsonPropertyName("notifyOnPayment")]
    public bool NotifyOnPayment { get; set; } = true;

    /// <summary>
    /// The customer-facing portal URL for this company (returned by the server during setup).
    /// </summary>
    [JsonPropertyName("portalUrl")]
    public string? PortalUrl { get; set; }

    /// <summary>
    /// Timestamp of the last successful sync.
    /// </summary>
    [JsonPropertyName("lastSyncTime")]
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// Connected payment account info (which providers the user has connected).
    /// </summary>
    [JsonPropertyName("connectedAccounts")]
    public ConnectedPaymentAccounts ConnectedAccounts { get; set; } = new();
}

/// <summary>
/// Tracks which payment provider accounts the user has connected via OAuth.
/// </summary>
public class ConnectedPaymentAccounts
{
    /// <summary>
    /// Whether Stripe is connected via Stripe Connect.
    /// </summary>
    [JsonPropertyName("stripeConnected")]
    public bool StripeConnected { get; set; }

    /// <summary>
    /// The email associated with the connected Stripe account.
    /// </summary>
    [JsonPropertyName("stripeEmail")]
    public string? StripeEmail { get; set; }

    /// <summary>
    /// Whether PayPal is connected.
    /// </summary>
    [JsonPropertyName("paypalConnected")]
    public bool PaypalConnected { get; set; }

    /// <summary>
    /// The email associated with the connected PayPal account.
    /// </summary>
    [JsonPropertyName("paypalEmail")]
    public string? PaypalEmail { get; set; }

    /// <summary>
    /// Whether Square is connected via Square OAuth.
    /// </summary>
    [JsonPropertyName("squareConnected")]
    public bool SquareConnected { get; set; }

    /// <summary>
    /// The email/name associated with the connected Square account.
    /// </summary>
    [JsonPropertyName("squareEmail")]
    public string? SquareEmail { get; set; }

}
