namespace ArgoBooks.Core.Services.Integrations;

/// <summary>
/// Fetches the exact-date exchange rates an integration sync is about to need.
///
/// Money is displayed by converting the USD figure at the transaction's own date,
/// and a date whose rate is not cached shows "Pending" instead of an amount. The
/// spreadsheet import gates on this already (see RateReadinessService). A sync
/// did not, so a row dated on any day the cache happened not to hold arrived
/// showing "Pending" and stayed that way, because nothing backfills the cache for
/// rows that are already in the books.
///
/// Best-effort by design, unlike the spreadsheet gate: a sync is something the
/// merchant kicked off to get their data in, and refusing to import because a
/// rate server is unreachable would be a worse trade than a row that reads
/// "Pending" until the next sync fills the gap.
/// </summary>
public static class IntegrationRates
{
    /// <summary>
    /// Cache the USD rates for <paramref name="dates"/>, skipping the work entirely
    /// when the books are kept in USD and no conversion can be needed.
    /// </summary>
    public static async Task EnsureAsync(
        IEnumerable<DateTime> dates,
        string? displayCurrency,
        IErrorLogger? errorLogger = null,
        CancellationToken ct = default)
    {
        var rates = ExchangeRateService.Instance;
        if (rates == null) return;

        if (string.IsNullOrWhiteSpace(displayCurrency) ||
            string.Equals(displayCurrency, "USD", StringComparison.OrdinalIgnoreCase))
            return;

        var today = DateTime.Today;

        // Future dates are unpriceable, not missing. They stay pending until the
        // day arrives, which is the documented behaviour rather than a failure.
        var needed = dates
            .Select(d => d.Date)
            .Where(d => d <= today)
            .Distinct()
            .ToList();

        if (needed.Count == 0) return;

        try
        {
            await rates.PreloadRatesAsync(needed, null, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            // Worth knowing about, not worth failing the sync over: the rows land
            // showing "Pending" and pick up their amount on a later sync.
            errorLogger?.LogWarning(
                $"Could not fetch exchange rates for {needed.Count} dates before an integration import: {ex.Message}",
                "IntegrationRates");
        }
    }
}
