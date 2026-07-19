namespace ArgoBooks.Core.Services.Integrations;

/// <summary>
/// Groups normalized Stripe items into one batch per payout. Items not yet
/// assigned to a payout (null PayoutId) are excluded until a later sync sees
/// them in their payout, so nothing is imported before it has actually paid out.
/// </summary>
public class StripePayoutAggregator
{
    public IReadOnlyList<StripePayoutBatch> Aggregate(IEnumerable<StripeImportItem> items)
    {
        var batches = new List<StripePayoutBatch>();
        var groups = items
            .Where(i => !string.IsNullOrEmpty(i.PayoutId))
            .GroupBy(i => i.PayoutId!);

        foreach (var g in groups)
        {
            var gross = g.Where(i => i.Kind == StripeItemKind.Revenue).Sum(i => i.Amount);
            var fees = g.Where(i => i.Kind == StripeItemKind.Fee).Sum(i => i.Amount);
            var refunds = g.Where(i => i.Kind == StripeItemKind.Refund).Sum(i => i.Amount);

            var payoutItem = g.FirstOrDefault(i => i.Kind == StripeItemKind.Payout);
            var net = payoutItem != null ? payoutItem.Amount : gross - fees - refunds;
            var date = payoutItem?.Date ?? g.Max(i => i.Date);

            batches.Add(new StripePayoutBatch(g.Key, date, gross, fees, refunds, net));
        }

        return batches;
    }
}
