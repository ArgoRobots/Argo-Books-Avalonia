namespace ArgoBooks.Core.Services.Integrations;

/// <summary>What a mapped Stripe item represents for the books.</summary>
public enum StripeItemKind { Revenue, Fee, Refund, Payout, Other }

/// <summary>
/// A normalized Stripe activity item. Amount is always a positive decimal in the
/// account currency; Kind carries the money-in / money-out meaning.
/// </summary>
public record StripeImportItem(
    StripeItemKind Kind,
    decimal Amount,
    DateTime Date,
    string Description,
    string SourceId);
