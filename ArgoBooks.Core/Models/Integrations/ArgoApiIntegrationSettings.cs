namespace ArgoBooks.Core.Models.Integrations;

/// <summary>
/// Connection state for the Argo Books public API, stored per company in the
/// .argo file.
///
/// This is the inbound side of the app: third-party developers push customers,
/// suppliers, products, categories, expenses, revenue and refunds to
/// argorobots.com with a key the merchant issued them, and this integration
/// pulls what is waiting so the merchant can review and import it.
///
/// Distinct from <see cref="StripeIntegrationSettings"/>, which pulls the
/// merchant's own Stripe activity, and from the payment portal, which takes
/// money from the merchant's customers.
/// </summary>
public class ArgoApiIntegrationSettings
{
    /// <summary>True once the API has been enabled for this company on the server.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>The account id (acct_...) this company's data lands under.</summary>
    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    /// <summary>
    /// Identifier for this company file on the server. Seeded from the mobile
    /// sync uid when one exists so both features name the same company, and
    /// generated otherwise, because mobile sync may never have been set up.
    /// </summary>
    [JsonPropertyName("companyUid")]
    public string? CompanyUid { get; set; }

    /// <summary>
    /// The key the desktop uses to talk to /v1 on the merchant's behalf. Minted
    /// automatically when the API is enabled, separate from any key handed to a
    /// developer so revoking a developer's access never locks the app out.
    /// </summary>
    [JsonPropertyName("desktopKey")]
    public string? DesktopKey { get; set; }

    /// <summary>Timestamp of the last successful import.</summary>
    [JsonPropertyName("lastSyncTime")]
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// Import batches this company has created, newest last. Kept so an undo can
    /// tell the server to release the objects it claimed, rather than leaving the
    /// queue insisting they were imported when the merchant has just removed them.
    /// </summary>
    [JsonPropertyName("importedBatches")]
    public List<string> ImportedBatches { get; set; } = new();
}
