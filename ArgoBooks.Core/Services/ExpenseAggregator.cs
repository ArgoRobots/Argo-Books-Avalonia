using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Helpers for aggregating Expense rows in USD. Expenses have no
/// "paid vs unpaid" distinction: an Expense row means the money already
/// went out, so there is no cash-basis filter to apply.
///
/// All expense aggregations use EffectiveTotalUSD (gross of any tax paid
/// to suppliers), that's the actual cash that left the business.
///
/// See docs/Calculations.md §9 for the standard.
/// </summary>
public static class ExpenseAggregator
{
    /// <summary>
    /// Sum gross USD expenses (EffectiveTotalUSD) inside a date range.
    /// </summary>
    public static decimal SumExpensesUSD(
        IEnumerable<Expense> expenses, DateTime start, DateTime end)
    {
        return expenses
            .Where(e => e.Date >= start && e.Date <= end)
            .Sum(e => e.EffectiveTotalUSD);
    }
}
