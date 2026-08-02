namespace ArgoBooks.Core.Models.Integrations;

/// <summary>
/// Container for third-party data integrations (import a user's own provider
/// activity into their books). Stripe is the first; others slot in beside it.
/// </summary>
public class IntegrationsSettings
{
    [JsonPropertyName("stripe")]
    public StripeIntegrationSettings Stripe { get; set; } = new();
}
