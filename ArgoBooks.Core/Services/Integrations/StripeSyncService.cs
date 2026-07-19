using ArgoBooks.Core.Data;

namespace ArgoBooks.Core.Services.Integrations;

public record StripeSyncPreview(IReadOnlyList<StripePayoutBatch> NewBatches, decimal TotalRevenue, decimal TotalFees);

/// <summary>
/// Orchestrates a Stripe sync: list payouts, pull each not-yet-imported payout's
/// transactions (via the ?payout= filter, since a balance transaction carries no
/// payout id of its own), map and aggregate them into per-payout batches. Splits
/// preview from import so the UI can confirm before anything is written. Idempotent:
/// dedupe is by the ImportedPayouts memory, so re-listing payouts is safe.
/// </summary>
public class StripeSyncService
{
    private readonly StripeApiClient _client;
    public StripeSyncService(StripeApiClient client) => _client = client;

    public async Task<StripeSyncPreview> PreviewAsync(CompanyData data, CancellationToken ct = default)
    {
        var stripe = data.Settings.Integrations.Stripe;
        if (string.IsNullOrWhiteSpace(stripe.ApiKey))
            return new StripeSyncPreview(Array.Empty<StripePayoutBatch>(), 0m, 0m);

        var payouts = await _client.FetchPayoutsAsync(stripe.ApiKey!, ct);
        var already = new HashSet<string>(stripe.ImportedPayouts.Select(p => p.StripePayoutId), StringComparer.Ordinal);
        var mapper = new StripeActivityMapper();
        var aggregator = new StripePayoutAggregator();

        var fresh = new List<StripePayoutBatch>();
        foreach (var p in payouts)
        {
            if (already.Contains(p.Id)) continue;
            if (p.Status is "canceled" or "failed") continue; // a payout that never lands shouldn't post

            var txns = await _client.FetchPayoutTransactionsAsync(stripe.ApiKey!, p.Id, ct);
            var items = mapper.Map(txns);
            fresh.AddRange(aggregator.Aggregate(items));
        }

        var totalRev = fresh.Sum(b => b.GrossRevenue);
        var totalFees = fresh.Sum(b => b.Fees);
        return new StripeSyncPreview(fresh, totalRev, totalFees);
    }

    public StripeImportResult ImportPreview(CompanyData data, StripeSyncPreview preview)
    {
        var result = new StripeImportService().Import(data, preview.NewBatches);
        if (result.PayoutsImported > 0)
            data.Settings.Integrations.Stripe.LastSyncTime = DateTime.Now;
        return result;
    }

    public int PendingCount(StripeSyncPreview preview) => preview.NewBatches.Count;
}
