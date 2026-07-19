using ArgoBooks.Core.Data;

namespace ArgoBooks.Core.Services.Integrations;

public record StripeSyncPreview(IReadOnlyList<StripePayoutBatch> NewBatches, decimal TotalRevenue, decimal TotalFees);

/// <summary>
/// Orchestrates a Stripe sync: fetch balance transactions, map and aggregate them
/// into per-payout batches, and filter out payouts already imported. Splits preview
/// from import so the UI can confirm before anything is written. Idempotent: dedupe
/// is by the ImportedPayouts memory, so re-fetching an overlapping window is safe.
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

        var txns = await _client.FetchBalanceTransactionsSinceAsync(stripe.ApiKey!, null, ct);
        var items = new StripeActivityMapper().Map(txns);
        var batches = new StripePayoutAggregator().Aggregate(items);

        var already = new HashSet<string>(stripe.ImportedPayouts.Select(p => p.StripePayoutId), StringComparer.Ordinal);
        var fresh = batches.Where(b => !already.Contains(b.PayoutId)).ToList();

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
