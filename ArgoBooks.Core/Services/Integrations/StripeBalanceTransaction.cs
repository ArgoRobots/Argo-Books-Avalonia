namespace ArgoBooks.Core.Services.Integrations;

/// <summary>
/// One Stripe balance-transaction, amounts in the smallest currency unit (cents).
/// For a charge: AmountCents is gross, FeeCents is Stripe's fee, NetCents = Amount - Fee.
/// </summary>
public record StripeBalanceTransaction(
    string Id,
    string Type,
    long AmountCents,
    long FeeCents,
    long NetCents,
    long CreatedUnix,
    string Currency,
    string? Description,
    string? PayoutId);
