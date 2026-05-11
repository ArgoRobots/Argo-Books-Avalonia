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
}
