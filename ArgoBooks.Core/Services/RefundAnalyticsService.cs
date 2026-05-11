using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Aggregations for the Refunds tab on the Analytics page. All metrics are
/// computed locally from the company's Payment list (no server roundtrip).
/// Refund Payments are identified by IsRefund==true and have negative Amount;
/// the absolute value of EffectiveAmountUSD is the refund amount.
///
/// All sums are USD-normalized (Payment.EffectiveAmountUSD) so multi-currency
/// portals roll up consistently. Callers must format with
/// <c>CurrencyService.FormatFromUSD</c> for display. See docs/Calculations.md §3.
/// </summary>
public static class RefundAnalyticsService
{
    /// <summary>Total refunded over a window, in USD.</summary>
    public static decimal TotalRefundedUSD(CompanyData company, DateTime since)
        => company.Payments
            .Where(p => p.IsRefund && p.Date >= since)
            .Sum(p => Math.Abs(p.EffectiveAmountUSD));

    /// <summary>Refund rate = sum(refunds) / sum(positive payments) over the window. 0 if no payments.</summary>
    public static decimal RefundRate(CompanyData company, DateTime since)
    {
        var refunds = TotalRefundedUSD(company, since);
        var positive = company.Payments
            .Where(p => !p.IsRefund && p.Date >= since)
            .Sum(p => p.EffectiveAmountUSD);
        return positive > 0 ? refunds / positive : 0m;
    }

    /// <summary>Monthly buckets of refund totals (USD) for the last <paramref name="months"/> months.</summary>
    public static IReadOnlyList<MonthlyRefundTotal> MonthlyTotals(CompanyData company, int months)
    {
        var firstMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(months - 1));
        var buckets = new Dictionary<DateTime, decimal>();
        for (int i = 0; i < months; i++)
            buckets[firstMonth.AddMonths(i)] = 0m;
        foreach (var p in company.Payments.Where(p => p.IsRefund && p.Date >= firstMonth))
        {
            var key = new DateTime(p.Date.Year, p.Date.Month, 1);
            if (buckets.ContainsKey(key)) buckets[key] += Math.Abs(p.EffectiveAmountUSD);
        }
        return buckets
            .OrderBy(kv => kv.Key)
            .Select(kv => new MonthlyRefundTotal(kv.Key, kv.Value))
            .ToList();
    }

    /// <summary>Top customers by absolute refund total (USD) since <paramref name="since"/>.</summary>
    public static IReadOnlyList<CustomerRefundTotal> TopRefundedCustomers(CompanyData company, DateTime since, int top)
    {
        var byCustomer = company.Payments
            .Where(p => p.IsRefund && p.Date >= since)
            .GroupBy(p => p.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Total = g.Sum(p => Math.Abs(p.EffectiveAmountUSD)),
                Count = g.Count(),
            })
            .OrderByDescending(x => x.Total)
            .Take(top)
            .ToList();

        return byCustomer.Select(x => new CustomerRefundTotal(
            x.CustomerId,
            company.GetCustomer(x.CustomerId)?.Name ?? "Unknown",
            x.Total,
            x.Count)).ToList();
    }

    /// <summary>Top product/line items by refund total (USD) — derived from refunded invoices' line items.</summary>
    public static IReadOnlyList<ProductRefundTotal> TopRefundedProducts(CompanyData company, DateTime since, int top)
    {
        // Sum refund amounts per invoice, then attribute proportionally across the
        // invoice's line items by their share of the original total. This is an
        // approximation — the true refunded line items are stored in the server's
        // line_items_json snapshot but not surfaced to the desktop.
        var byProduct = new Dictionary<string, decimal>();
        var refundsByInvoice = company.Payments
            .Where(p => p.IsRefund && p.Date >= since)
            .GroupBy(p => p.InvoiceId)
            .ToDictionary(g => g.Key, g => g.Sum(p => Math.Abs(p.EffectiveAmountUSD)));

        foreach (var (invoiceId, refundAmt) in refundsByInvoice)
        {
            var invoice = company.GetInvoice(invoiceId);
            if (invoice?.LineItems == null || invoice.Total <= 0) continue;
            var totalLines = invoice.LineItems.Sum(li => li.Amount);
            if (totalLines <= 0) continue;
            // refundAmt is USD; (li.Amount / totalLines) is a dimensionless
            // share. Product attribution stays in USD.
            foreach (var li in invoice.LineItems)
            {
                var share = (li.Amount / totalLines) * refundAmt;
                var key = string.IsNullOrEmpty(li.Description) ? "(unnamed)" : li.Description;
                byProduct[key] = byProduct.GetValueOrDefault(key) + share;
            }
        }

        return byProduct
            .OrderByDescending(kv => kv.Value)
            .Take(top)
            .Select(kv => new ProductRefundTotal(kv.Key, kv.Value))
            .ToList();
    }

    /// <summary>Top reasons by occurrence count (filtered to non-empty reasons since the window). Totals in USD.</summary>
    public static IReadOnlyList<RefundReasonCount> TopReasons(CompanyData company, DateTime since, int top)
        => company.Payments
            .Where(p => p.IsRefund && p.Date >= since && !string.IsNullOrWhiteSpace(p.RefundReason))
            .GroupBy(p => p.RefundReason!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new RefundReasonCount(g.Key, g.Count(), g.Sum(p => Math.Abs(p.EffectiveAmountUSD))))
            .OrderByDescending(r => r.Count)
            .Take(top)
            .ToList();

    /// <summary>Channel breakdown by total refunded amount (USD).</summary>
    public static IReadOnlyDictionary<string, decimal> ChannelBreakdown(CompanyData company, DateTime since)
        => company.Payments
            .Where(p => p.IsRefund && p.Date >= since)
            .GroupBy(p => p.PaymentMethod.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(p => Math.Abs(p.EffectiveAmountUSD)));

    /// <summary>Average days between original payment and its refund (for refunds that link to a known source).</summary>
    public static double AverageRefundLatencyDays(CompanyData company, DateTime since)
    {
        var pairs = new List<double>();
        foreach (var refund in company.Payments.Where(p => p.IsRefund && p.Date >= since))
        {
            if (string.IsNullOrEmpty(refund.RefundedFromPaymentId)) continue;
            var source = company.Payments.FirstOrDefault(p => p.Id == refund.RefundedFromPaymentId);
            if (source == null) continue;
            var span = (refund.Date - source.Date).TotalDays;
            if (span >= 0) pairs.Add(span);
        }
        return pairs.Count > 0 ? pairs.Average() : 0;
    }
}

public record MonthlyRefundTotal(DateTime Month, decimal AmountUSD);
public record CustomerRefundTotal(string CustomerId, string CustomerName, decimal AmountUSD, int Count);
public record ProductRefundTotal(string ProductLabel, decimal AmountUSD);
public record RefundReasonCount(string Reason, int Count, decimal TotalAmountUSD);
