namespace ArgoBooks.Core.Services.Integrations;

/// <summary>
/// Groups normalized Stripe items into one batch per calendar day: gross revenue
/// (charges), processing fees, and refunds. Grouping by day (rather than by payout)
/// keeps the books readable and works for any account, since it doesn't depend on
/// Stripe's automatic-payout-only transaction grouping.
/// </summary>
public class StripeDailyAggregator
{
    public IReadOnlyList<StripeDailyBatch> AggregateByDay(IEnumerable<StripeImportItem> items)
    {
        var batches = new List<StripeDailyBatch>();

        foreach (var g in items.GroupBy(i => i.Date.Date).OrderBy(g => g.Key))
        {
            var gross = g.Where(i => i.Kind == StripeItemKind.Revenue).Sum(i => i.Amount);
            var fees = g.Where(i => i.Kind == StripeItemKind.Fee).Sum(i => i.Amount);
            var refunds = g.Where(i => i.Kind == StripeItemKind.Refund).Sum(i => i.Amount);

            // A day with only payout/other items and no money movement isn't worth a batch.
            if (gross == 0 && fees == 0 && refunds == 0) continue;

            batches.Add(new StripeDailyBatch(g.Key, gross, fees, refunds));
        }

        return batches;
    }
}
