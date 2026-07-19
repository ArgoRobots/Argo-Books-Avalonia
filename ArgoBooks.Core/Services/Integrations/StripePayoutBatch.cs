namespace ArgoBooks.Core.Services.Integrations;

/// <summary>
/// One Stripe payout's aggregated activity: the gross revenue, fees, and refunds
/// that make it up, plus the net amount that actually hit the bank.
/// </summary>
public record StripePayoutBatch(
    string PayoutId,
    DateTime Date,
    decimal GrossRevenue,
    decimal Fees,
    decimal Refunds,
    decimal NetAmount);
