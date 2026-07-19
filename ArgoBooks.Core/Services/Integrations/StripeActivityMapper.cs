namespace ArgoBooks.Core.Services.Integrations;

/// <summary>
/// Turns raw Stripe balance transactions into normalized import items: charges
/// become gross revenue plus a processing-fee expense, refunds and payouts get
/// their own kinds, and anything unrecognized is surfaced as Other rather than
/// silently dropped.
/// </summary>
public class StripeActivityMapper
{
    public IReadOnlyList<StripeImportItem> Map(IEnumerable<StripeBalanceTransaction> txns)
    {
        var items = new List<StripeImportItem>();
        foreach (var t in txns)
        {
            var date = DateTimeOffset.FromUnixTimeSeconds(t.CreatedUnix).LocalDateTime;
            var desc = string.IsNullOrWhiteSpace(t.Description) ? t.Type : t.Description!;

            switch (t.Type)
            {
                case "charge":
                    items.Add(new StripeImportItem(StripeItemKind.Revenue, Money(t.AmountCents), date, desc, t.Id));
                    if (t.FeeCents > 0)
                        items.Add(new StripeImportItem(StripeItemKind.Fee, Money(t.FeeCents), date, "Stripe processing fee", t.Id));
                    break;
                case "refund":
                    items.Add(new StripeImportItem(StripeItemKind.Refund, Money(Math.Abs(t.AmountCents)), date, desc, t.Id));
                    break;
                case "payout":
                    items.Add(new StripeImportItem(StripeItemKind.Payout, Money(Math.Abs(t.AmountCents)), date, "Stripe payout to bank", t.Id));
                    break;
                default:
                    items.Add(new StripeImportItem(StripeItemKind.Other, Money(Math.Abs(t.AmountCents)), date,
                        $"Stripe {t.Type}", t.Id));
                    break;
            }
        }
        return items;
    }

    private static decimal Money(long cents) => cents / 100m;
}
