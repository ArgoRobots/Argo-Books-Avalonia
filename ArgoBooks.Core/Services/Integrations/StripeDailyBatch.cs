namespace ArgoBooks.Core.Services.Integrations;

/// <summary>
/// One day's aggregated Stripe activity: gross revenue (charges), processing fees,
/// and refunds for that date. Revenue is recognized when the charge happens, not
/// when it pays out, so this works regardless of automatic vs manual payouts.
/// </summary>
public record StripeDailyBatch(
    DateTime Date,
    decimal GrossRevenue,
    decimal Fees,
    decimal Refunds);
