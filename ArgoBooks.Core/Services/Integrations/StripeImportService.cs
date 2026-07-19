using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Integrations;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services.Integrations;

public record StripeImportResult(int PayoutsImported, int RevenuesCreated, int ExpensesCreated, int SkippedAlreadyImported);

/// <summary>
/// Creates aggregated book records from Stripe payout batches (one gross Revenue +
/// one fees Expense + refunds per payout) and remembers each payout so a later bank
/// import can auto-ignore the matching deposit rather than double-counting it.
/// </summary>
public class StripeImportService
{
    public StripeImportResult Import(CompanyData data, IReadOnlyList<StripePayoutBatch> batches)
    {
        var stripe = data.Settings.Integrations.Stripe;
        var already = new HashSet<string>(stripe.ImportedPayouts.Select(p => p.StripePayoutId), StringComparer.Ordinal);

        int payouts = 0, revs = 0, exps = 0, skipped = 0;

        foreach (var b in batches)
        {
            if (already.Contains(b.PayoutId)) { skipped++; continue; }

            if (b.GrossRevenue > 0)
            {
                var draft = new TransactionDraft(
                    Date: b.Date,
                    Description: $"Stripe sales (payout {b.PayoutId})",
                    Total: b.GrossRevenue,
                    CounterpartyId: null,
                    Notes: "Imported from Stripe",
                    OriginalCurrency: CompanyCurrency(data));
                var tx = TransactionFactory.CreateRevenue(data, draft);
                tx.TotalUSD = tx.Total; tx.UnitPriceUSD = tx.UnitPrice;
                data.Revenues.Add(tx);
                revs++;
            }

            if (b.Fees > 0)
            {
                AddExpense(data, b.Date, $"Stripe fees (payout {b.PayoutId})", b.Fees);
                exps++;
            }
            if (b.Refunds > 0)
            {
                AddExpense(data, b.Date, $"Stripe refunds (payout {b.PayoutId})", b.Refunds);
                exps++;
            }

            stripe.ImportedPayouts.Add(new StripePayoutRecord
            {
                StripePayoutId = b.PayoutId,
                AmountCents = (long)Math.Round(b.NetAmount * 100m),
                Date = b.Date
            });
            already.Add(b.PayoutId);
            payouts++;
        }

        if (payouts > 0) data.MarkAsModified();
        return new StripeImportResult(payouts, revs, exps, skipped);
    }

    private static void AddExpense(CompanyData data, DateTime date, string description, decimal total)
    {
        var draft = new TransactionDraft(
            Date: date, Description: description, Total: total,
            CounterpartyId: null, Notes: "Imported from Stripe",
            OriginalCurrency: CompanyCurrency(data));
        var tx = TransactionFactory.CreateExpense(data, draft);
        tx.TotalUSD = tx.Total; tx.UnitPriceUSD = tx.UnitPrice;
        data.Expenses.Add(tx);
    }

    private static string CompanyCurrency(CompanyData data)
        => string.IsNullOrWhiteSpace(data.Settings.Localization.Currency) ? "USD" : data.Settings.Localization.Currency;
}
