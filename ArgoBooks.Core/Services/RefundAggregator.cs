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
    /// Total amount refunded against a single original Payment, from any number
    /// of partial refund Payments tied to it via RefundedFromPaymentId.
    /// </summary>
    public static decimal GetRefundedForPayment(Payment original, IEnumerable<Payment> allPayments)
    {
        if (original == null) return 0m;
        return allPayments
            .Where(r => r.IsRefund && r.RefundedFromPaymentId == original.Id)
            .Sum(r => Math.Abs(r.Amount));
    }

    /// <summary>
    /// Total amount refunded against the invoice this revenue is tied to.
    /// Returns 0 for revenues without an InvoiceId (e.g. manual entries).
    /// </summary>
    public static decimal GetRefundedForRevenue(Revenue revenue, IEnumerable<Payment> allPayments)
    {
        if (revenue == null || string.IsNullOrEmpty(revenue.InvoiceId)) return 0m;
        return allPayments
            .Where(p => p.IsRefund && p.InvoiceId == revenue.InvoiceId)
            .Sum(p => Math.Abs(p.Amount));
    }

    /// <summary>
    /// Sum of refund amounts (in original currency) whose refund date falls
    /// inside [start, end]. For UI surfaces that display per-payment currency.
    /// </summary>
    public static decimal GetRefundedInDateRange(IEnumerable<Payment> allPayments, DateTime start, DateTime end)
    {
        return allPayments
            .Where(p => p.IsRefund && p.Date >= start && p.Date <= end)
            .Sum(p => Math.Abs(p.Amount));
    }

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
}
