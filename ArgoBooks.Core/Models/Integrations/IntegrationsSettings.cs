namespace ArgoBooks.Core.Models.Integrations;

/// <summary>
/// Container for third-party data integrations (import a user's own provider
/// activity into their books). Stripe is the first; others slot in beside it.
/// </summary>
public class IntegrationsSettings
{
    [JsonPropertyName("stripe")]
    public StripeIntegrationSettings Stripe { get; set; } = new();

    /// <summary>
    /// The Argo Books public API: data pushed in by developers the merchant has
    /// issued a key to. Inbound rather than fetched, but it imports through the
    /// same preview-then-single-undo flow as Stripe.
    /// </summary>
    [JsonPropertyName("argoApi")]
    public ArgoApiIntegrationSettings ArgoApi { get; set; } = new();
}
