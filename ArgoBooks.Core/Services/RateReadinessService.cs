namespace ArgoBooks.Core.Services;

/// <summary>Whether all required exact-date rates are cached.</summary>
public enum RateReadinessStatus { Ready, Unavailable }

/// <summary>Why rates could not be made ready (for the user-facing message).</summary>
public enum RateUnavailableReason { None, NoInternet, ServerUnreachable, Unknown }

/// <summary>
/// Result of <see cref="RateReadinessService.EnsureRatesAsync"/>. <see cref="FutureDatesDeferred"/>
/// lists dates that cannot be priced (future) so the caller can mark those rows pending.
/// </summary>
public sealed record RateReadiness(
    RateReadinessStatus Status,
    RateUnavailableReason Reason,
    IReadOnlyList<DateTime> FutureDatesDeferred);

/// <summary>
/// Ensures the exact-date USD rates needed for a set of transaction dates are cached, fetching any
/// that are missing. Used before import (and other bulk operations) so money converts at the exact
/// date and never falls back to a wrong date. Future dates are never required (unpriceable) and are
/// returned for deferral. The server returns USD-&gt;all per date, so caching a date covers every
/// currency for that date. See docs/Calculations.md (Rule 3a).
/// </summary>
public sealed class RateReadinessService
{
    private const string Host = "https://argorobots.com";
    private const string ProbeCurrency = "EUR"; // any always-present currency confirms a date is cached

    private readonly ExchangeRateService _rates;
    private readonly IConnectivityService _connectivity;

    public RateReadinessService(ExchangeRateService rates, IConnectivityService connectivity)
    {
        _rates = rates;
        _connectivity = connectivity;
    }

    public async Task<RateReadiness> EnsureRatesAsync(IEnumerable<DateTime> dates, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var distinct = dates.Select(d => d.Date).Distinct().ToList();
        var future = distinct.Where(d => d > today).ToList();
        var required = distinct.Where(d => d <= today).ToList();

        var missing = required.Where(d => _rates.GetExchangeRate("USD", ProbeCurrency, d) <= 0).ToList();
        if (missing.Count == 0)
            return new RateReadiness(RateReadinessStatus.Ready, RateUnavailableReason.None, future);

        // Quick offline check: if there is clearly no internet, surface the pause prompt immediately
        // rather than grinding through a slow per-date fetch-and-retry loop that is bound to fail.
        if (!await _connectivity.IsInternetAvailableAsync(ct))
            return new RateReadiness(RateReadinessStatus.Unavailable, RateUnavailableReason.NoInternet, future);

        // Try to fetch the missing dates (PreloadRatesAsync batches and falls back per-date).
        try
        {
            await _rates.PreloadRatesAsync(missing, cancellationToken: ct);
        }
        catch (OperationCanceledException) { throw; }
        catch { /* fall through to re-check + reason classification */ }

        var stillMissing = missing.Where(d => _rates.GetExchangeRate("USD", ProbeCurrency, d) <= 0).ToList();
        if (stillMissing.Count == 0)
            return new RateReadiness(RateReadinessStatus.Ready, RateUnavailableReason.None, future);

        var reason = await ClassifyAsync(ct);
        return new RateReadiness(RateReadinessStatus.Unavailable, reason, future);
    }

    private async Task<RateUnavailableReason> ClassifyAsync(CancellationToken ct)
    {
        if (!await _connectivity.IsInternetAvailableAsync(ct))
            return RateUnavailableReason.NoInternet;
        if (!await _connectivity.IsHostReachableAsync(Host, ct))
            return RateUnavailableReason.ServerUnreachable;
        return RateUnavailableReason.Unknown;
    }
}
