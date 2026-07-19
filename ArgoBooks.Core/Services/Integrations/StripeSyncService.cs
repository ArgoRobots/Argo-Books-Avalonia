using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Integrations;

namespace ArgoBooks.Core.Services.Integrations;

public record StripeSyncPreview(
    IReadOnlyList<StripeDailyBatch> Days,
    decimal TotalRevenue,
    decimal TotalFees,
    string? NewCursor,
    IReadOnlyList<StripePayoutSummary> NewPayouts)
{
    /// <summary>True when there's anything to import: new revenue/fees, or new payouts to remember.</summary>
    public bool HasActivity => Days.Count > 0 || NewPayouts.Count > 0;
}

/// <summary>
/// Orchestrates a Stripe sync. Revenue, fees and refunds come from balance transactions
/// (fetched newest-first until the last-synced watermark) grouped by day. Payouts come
/// from the payouts list and are remembered only so a later bank import can auto-ignore
/// the matching deposit. Separating the two avoids Stripe's automatic-payout-only
/// transaction grouping, so it works for manual payouts too. Preview is read-only;
/// import writes the records, remembers the payouts, and advances the cursor.
/// </summary>
public class StripeSyncService
{
    private readonly StripeApiClient _client;
    public StripeSyncService(StripeApiClient client) => _client = client;

    public async Task<StripeSyncPreview> PreviewAsync(CompanyData data, CancellationToken ct = default)
    {
        var stripe = data.Settings.Integrations.Stripe;
        if (string.IsNullOrWhiteSpace(stripe.ApiKey))
            return Empty();

        var txns = await _client.FetchBalanceTransactionsUntilAsync(stripe.ApiKey!, stripe.LastSyncCursor, ct);
        var items = new StripeActivityMapper().Map(txns);
        var days = new StripeDailyAggregator().AggregateByDay(items);
        var newCursor = txns.Count > 0 ? txns[0].Id : stripe.LastSyncCursor;

        var payouts = await _client.FetchPayoutsAsync(stripe.ApiKey!, ct);
        var known = new HashSet<string>(stripe.ImportedPayouts.Select(p => p.StripePayoutId), StringComparer.Ordinal);
        var newPayouts = payouts
            .Where(p => !known.Contains(p.Id) && p.Status is not ("canceled" or "failed"))
            .ToList();

        return new StripeSyncPreview(days, days.Sum(d => d.GrossRevenue), days.Sum(d => d.Fees), newCursor, newPayouts);
    }

    public StripeImportResult ImportPreview(CompanyData data, StripeSyncPreview preview)
    {
        var stripe = data.Settings.Integrations.Stripe;
        var result = new StripeImportService().Import(data, preview.Days);

        // Remember each new payout so a later bank import auto-ignores the matching deposit.
        foreach (var p in preview.NewPayouts)
        {
            stripe.ImportedPayouts.Add(new StripePayoutRecord
            {
                StripePayoutId = p.Id,
                AmountCents = Math.Abs(p.AmountCents),
                Date = DateTimeOffset.FromUnixTimeSeconds(p.DateUnix).LocalDateTime
            });
        }

        if (!string.IsNullOrEmpty(preview.NewCursor))
            stripe.LastSyncCursor = preview.NewCursor;

        if (preview.HasActivity)
        {
            stripe.LastSyncTime = DateTime.Now;
            data.MarkAsModified();
        }

        return result;
    }

    private static StripeSyncPreview Empty()
        => new(Array.Empty<StripeDailyBatch>(), 0m, 0m, null, Array.Empty<StripePayoutSummary>());
}
