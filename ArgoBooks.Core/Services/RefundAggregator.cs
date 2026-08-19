using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Single source of truth for "how much of X has been refunded" calculations.
/// All UI surfaces (revenue page, payments page, dashboard stat cards, charts)
/// route through here so refund deductions stay consistent across the app.
///
/// Cash-basis dating: a refund reduces revenue on the date the refund was
/// issued, not the date of the original payment. Same-day refund nets to zero
/// on that day; multi-day refund leaves the original day's revenue intact and
/// produces a negative on the refund's day.
/// </summary>
public static class RefundAggregator
{
    /// <summary>
    /// USD-normalized variant for dashboard aggregations. Uses Payment.EffectiveAmountUSD
    /// so multi-currency portals roll up consistently.
    /// </summary>
    public static decimal GetRefundedInDateRangeUSD(IEnumerable<Payment> allPayments, DateTime start, DateTime end)
    {
        return allPayments
            .Where(p => p.IsRefund && p.Date >= start && p.Date <= end)
            .Sum(p => Math.Abs(p.EffectiveAmountUSD));
    }

    /// <summary>
    /// Display-currency variant of <see cref="GetRefundedInDateRangeUSD"/>: converts each refund at
    /// its OWN date via <paramref name="toDisplay"/> before summing (docs/Calculations.md §3a Phase 2).
    /// Pass <c>CurrencyService.GetDisplayAmount</c>. Equals the USD sum for a USD display currency.
    /// </summary>
    public static decimal GetRefundedInDateRangeDisplay(
        IEnumerable<Payment> allPayments, DateTime start, DateTime end, Func<decimal, DateTime, decimal> toDisplay)
    {
        return allPayments
            .Where(p => p.IsRefund && p.Date >= start && p.Date <= end)
            .Sum(p => toDisplay(Math.Abs(p.EffectiveAmountUSD), p.Date));
    }

    /// <summary>
    /// Group refund amounts (absolute USD) by the day the refund was issued.
    /// Used by per-day charts that subtract refunds from revenue/profit so
    /// the deduction lands on the refund's own day, not the original payment's.
    /// </summary>
    public static Dictionary<DateTime, decimal> GroupRefundsByDayUSD(
        IEnumerable<Payment> allPayments, DateTime start, DateTime end)
    {
        return allPayments
            .Where(p => p.IsRefund && p.Date >= start && p.Date <= end)
            .GroupBy(p => p.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(p => Math.Abs(p.EffectiveAmountUSD)));
    }

    /// <summary>
    /// Pre-tax USD portion of refunds inside [start, end], for profit math.
    /// Each refund is scaled by its invoice's Subtotal/Total ratio so the
    /// tax part of the refund, which was never profit on the revenue side,
    /// isn't subtracted again. Falls back to the full refund amount when
    /// the invoice link is missing.
    /// See docs/Calculations.md §8 for the rationale.
    /// </summary>
    public static decimal GetRefundedPreTaxInDateRangeUSD(
        IEnumerable<Payment> allPayments,
        IReadOnlyDictionary<string, Invoice> invoicesById,
        DateTime start, DateTime end)
    {
        decimal sum = 0m;
        foreach (var p in allPayments.Where(x => x.IsRefund && x.Date >= start && x.Date <= end))
        {
            var refundTotalUSD = Math.Abs(p.EffectiveAmountUSD);
            if (!string.IsNullOrEmpty(p.InvoiceId)
                && invoicesById.TryGetValue(p.InvoiceId, out var invoice)
                && invoice.Total > 0)
            {
                sum += refundTotalUSD * (invoice.Subtotal / invoice.Total);
            }
            else
            {
                sum += refundTotalUSD;
            }
        }
        return sum;
    }

    /// <summary>
    /// Display-currency variant of <see cref="GetRefundedPreTaxInDateRangeUSD"/>: converts each
    /// refund's pre-tax USD portion at its OWN date via <paramref name="toDisplay"/> before summing
    /// (docs/Calculations.md §3a Phase 2). Equals the USD sum for a USD display currency.
    /// </summary>
    public static decimal GetRefundedPreTaxInDateRangeDisplay(
        IEnumerable<Payment> allPayments,
        IReadOnlyDictionary<string, Invoice> invoicesById,
        DateTime start, DateTime end, Func<decimal, DateTime, decimal> toDisplay)
    {
        decimal sum = 0m;
        foreach (var p in allPayments.Where(x => x.IsRefund && x.Date >= start && x.Date <= end))
        {
            var refundTotalUSD = Math.Abs(p.EffectiveAmountUSD);
            decimal preTaxUSD;
            if (!string.IsNullOrEmpty(p.InvoiceId)
                && invoicesById.TryGetValue(p.InvoiceId, out var invoice)
                && invoice.Total > 0)
            {
                preTaxUSD = refundTotalUSD * (invoice.Subtotal / invoice.Total);
            }
            else
            {
                preTaxUSD = refundTotalUSD;
            }
            sum += toDisplay(preTaxUSD, p.Date);
        }
        return sum;
    }
}
