using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Single point of truth for recomputing the stored totals on an
/// <see cref="Invoice"/> from its <see cref="Payment"/> rows.
///
/// Historically these fields were updated inline by every code path that
/// added a payment (portal sync, spreadsheet import, manual entry). That
/// scattering let stored totals drift if any path forgot to update one of
/// the fields. Anything that mutates an invoice's payments should now
/// call <see cref="RecalculateFromPayments"/> followed by
/// <see cref="RecalculateStatus"/>.
///
/// See docs/Calculations.md §5 for the field semantics.
/// </summary>
public static class InvoiceTotalsService
{
    /// <summary>
    /// Derive AmountPaid, AmountRefunded, Balance, and BalanceUSD from the
    /// Payment rows tied to this invoice. Idempotent.
    /// </summary>
    public static void RecalculateFromPayments(
        Invoice invoice, IEnumerable<Payment> allPayments)
    {
        if (invoice == null) return;

        // Normalize currency comparison: treat null/empty as "USD" and
        // compare case-insensitively. This matches the historical inline
        // logic in PaymentPortalService.
        var invoiceCurrency = string.IsNullOrEmpty(invoice.OriginalCurrency)
            ? "USD" : invoice.OriginalCurrency;

        bool MatchesInvoiceCurrency(Payment p) =>
            string.Equals(
                string.IsNullOrEmpty(p.OriginalCurrency) ? "USD" : p.OriginalCurrency,
                invoiceCurrency,
                StringComparison.OrdinalIgnoreCase);

        var invoicePayments = allPayments.Where(p => p.InvoiceId == invoice.Id).ToList();

        // AmountPaid: gross positive payments in the invoice's currency.
        invoice.AmountPaid = invoicePayments
            .Where(p => !p.IsRefund && p.Amount > 0 && MatchesInvoiceCurrency(p))
            .Sum(p => p.Amount);

        // AmountRefunded: absolute sum of refund rows in the invoice's currency.
        invoice.AmountRefunded = invoicePayments
            .Where(p => p.IsRefund && MatchesInvoiceCurrency(p))
            .Sum(p => Math.Abs(p.Amount));

        // Balance: remaining owed. Refunds don't raise the balance, once
        // the customer paid, returning their money doesn't make them owe
        // again.
        invoice.Balance = Math.Max(0, invoice.Total - invoice.AmountPaid);

        // Keep USD fields in sync when we have a conversion ratio.
        if (invoice.TotalUSD > 0)
        {
            var totalPaidUSD = invoicePayments
                .Where(p => !p.IsRefund && p.Amount > 0)
                .Sum(p => p.EffectiveAmountUSD);
            invoice.BalanceUSD = Math.Max(0, invoice.TotalUSD - totalPaidUSD);
        }
    }

    /// <summary>
    /// Recompute the invoice's stored Status from AmountPaid / AmountRefunded.
    /// Touches Paid / Partial / PartiallyRefunded / Refunded only. Lifecycle
    /// states (Draft / Pending / Sent / Viewed / Cancelled / Overdue) are
    /// owned by the surfaces that drive them and are not overwritten here.
    ///
    /// The refund-status rule has two competing inputs:
    ///   (a) Processing fees make AmountPaid &gt; Total even when a single
    ///       full refund returns the entire invoice value (refunds don't
    ///       include the fee). Status should be Refunded.
    ///   (b) A second payment after a refund also makes AmountPaid &gt;&gt; Total,
    ///       but the refund history should stay visible. Status should be
    ///       PartiallyRefunded.
    /// The discriminator: net paid (AmountPaid − AmountRefunded). If it's
    /// less than one full invoice value it's fee residue (case a); if it's
    /// at least one full invoice value the customer has paid the invoice
    /// over again on top of the refund (case b).
    /// Tiny epsilon for cent-level float drift.
    /// </summary>
    public static void RecalculateStatus(Invoice invoice)
    {
        if (invoice == null) return;
        if (invoice.Status == InvoiceStatus.Draft
            || invoice.Status == InvoiceStatus.Pending
            || invoice.Status == InvoiceStatus.Sent
            || invoice.Status == InvoiceStatus.Viewed
            || invoice.Status == InvoiceStatus.Cancelled
            || invoice.Status == InvoiceStatus.Overdue)
        {
            // Lifecycle states are external, only re-evaluate once a
            // payment or refund has actually happened.
            if (invoice.AmountPaid <= 0 && invoice.AmountRefunded <= 0)
                return;
        }

        if (invoice.AmountRefunded > 0 && invoice.AmountPaid > 0)
        {
            if (invoice.AmountRefunded + 0.01m >= invoice.Total)
            {
                // Refund covers a full invoice value. Distinguish "single
                // pay + fee, fully refunded" from "pay → refund → pay again":
                // the latter leaves a net of at least one invoice value.
                var netPaid = invoice.AmountPaid - invoice.AmountRefunded;
                invoice.Status = netPaid + 0.01m < invoice.Total
                    ? InvoiceStatus.Refunded
                    : InvoiceStatus.PartiallyRefunded;
            }
            else
            {
                invoice.Status = InvoiceStatus.PartiallyRefunded;
            }
            return;
        }

        if (invoice.Balance <= 0 && invoice.AmountPaid > 0)
        {
            invoice.Status = InvoiceStatus.Paid;
            return;
        }

        if (invoice.AmountPaid > 0)
        {
            invoice.Status = InvoiceStatus.Partial;
        }
    }

    /// <summary>
    /// Convenience wrapper: recalc totals, then recalc status. Use this
    /// after any mutation to the invoice's payment list.
    /// </summary>
    public static void Recalculate(Invoice invoice, IEnumerable<Payment> allPayments)
    {
        RecalculateFromPayments(invoice, allPayments);
        RecalculateStatus(invoice);
    }
}
