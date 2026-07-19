using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services.Integrations;

public record StripeImportResult(int RevenuesCreated, int ExpensesCreated);

/// <summary>
/// Creates aggregated book records from Stripe daily batches: one gross Revenue plus
/// one fees Expense (and refunds) per day of activity. No dedupe here; the sync cursor
/// guarantees each balance transaction is only ever fetched once.
/// </summary>
public class StripeImportService
{
    public StripeImportResult Import(CompanyData data, IReadOnlyList<StripeDailyBatch> batches)
    {
        int revs = 0, exps = 0;

        foreach (var b in batches)
        {
            if (b.GrossRevenue > 0)
            {
                var draft = new TransactionDraft(
                    Date: b.Date,
                    Description: "Stripe sales",
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
                AddExpense(data, b.Date, "Stripe fees", b.Fees);
                exps++;
            }
            if (b.Refunds > 0)
            {
                AddExpense(data, b.Date, "Stripe refunds", b.Refunds);
                exps++;
            }
        }

        if (revs > 0 || exps > 0) data.MarkAsModified();
        return new StripeImportResult(revs, exps);
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
