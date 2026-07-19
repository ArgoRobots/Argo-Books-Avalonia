using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Integrations;

namespace ArgoBooks.Core.Services.Integrations;

public record StripeSyncPreview(
    IReadOnlyList<StripeChargeDetail> Charges,
    decimal TotalRevenue,
    decimal TotalFees,
    string? NewCursor,
    IReadOnlyList<StripePayoutSummary> NewPayouts)
{
    /// <summary>True when there's anything to import: new revenue/fees, or new payouts to remember.</summary>
    public bool HasActivity => Charges.Count > 0 || NewPayouts.Count > 0;
}

/// <summary>
/// Orchestrates a Stripe sync. Revenue, fees and refunds come from a detailed per-charge
/// fetch (fetched newest-first until the last-synced watermark), each charge becoming its
/// own Revenue with product/customer/tax/discount. Payouts come from the payouts list and
/// are remembered only so a later bank import can auto-ignore the matching deposit.
/// Separating the two avoids Stripe's automatic-payout-only transaction grouping, so it
/// works for manual payouts too. Preview is read-only; import writes the records, remembers
/// the payouts, and advances the cursor.
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

        var rawCharges = await _client.FetchChargesUntilAsync(stripe.ApiKey!, stripe.LastSyncCursor, ct);
        var newCursor = rawCharges.Count > 0 ? rawCharges[0].ChargeId : stripe.LastSyncCursor;

        // A charge's own expanded balance_transaction can silently yield a zero fee, so fill fees
        // from the balance-transactions list (the reliable source), keyed by charge id.
        var feeMap = rawCharges.Count > 0
            ? await _client.FetchChargeFeesAsync(stripe.ApiKey!, ct)
            : (IReadOnlyDictionary<string, long>)new Dictionary<string, long>();
        var charges = rawCharges
            .Select(c => feeMap.TryGetValue(c.ChargeId, out var fee) && fee > c.FeeCents ? c with { FeeCents = fee } : c)
            .ToList();

        var payouts = await _client.FetchPayoutsAsync(stripe.ApiKey!, ct);
        var known = new HashSet<string>(stripe.ImportedPayouts.Select(p => p.StripePayoutId), StringComparer.Ordinal);
        var newPayouts = payouts
            .Where(p => !known.Contains(p.Id) && p.Status is not ("canceled" or "failed"))
            .ToList();

        var totalRevenue = charges.Sum(c => c.GrossCents) / 100m;
        var totalFees = charges.Sum(c => c.FeeCents) / 100m;

        return new StripeSyncPreview(charges, totalRevenue, totalFees, newCursor, newPayouts);
    }

    public StripeDetailResult ImportPreview(CompanyData data, StripeSyncPreview preview)
    {
        var stripe = data.Settings.Integrations.Stripe;
        var importer = new StripeDetailImporter();
        var result = importer.ImportCharges(data, preview.Charges);
        importer.ApplyRefunds(data, preview.Charges);

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
        => new(Array.Empty<StripeChargeDetail>(), 0m, 0m, null, Array.Empty<StripePayoutSummary>());
}
