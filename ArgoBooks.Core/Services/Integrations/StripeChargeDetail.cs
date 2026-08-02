namespace ArgoBooks.Core.Services.Integrations;

/// <summary>
/// One Stripe charge with the detail needed for a full book entry. Amounts in cents.
/// </summary>
public record StripeChargeDetail(
    string ChargeId,
    long CreatedUnix,
    long GrossCents,
    long FeeCents,
    string Currency,
    string? CustomerName,
    string? CustomerEmail,
    string ProductName,
    long TaxCents,
    long DiscountCents,
    long AmountRefundedCents);
