using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Produces the transactions a recurring schedule has come due for. Generation is keyed on each
/// schedule's NextDate so occurrences missed while the app was closed are caught up on open.
/// Generated entries are real transactions flagged for review, not rows held outside the books:
/// holding them out would mean every total and report needed a filter to exclude them.
/// </summary>
/// <summary>
/// Converts an amount into USD at a given date, reporting false when no rate is held for that
/// date so the caller can queue the entry rather than record an approximate figure.
/// </summary>
public delegate bool UsdConverter(decimal amount, string currency, DateTime date, out decimal usd);

public static class RecurringTransactionService
{
    /// <summary>Stops a corrupt far-past date with a short cadence from spinning.</summary>
    public const int MaxOccurrencesPerSchedulePerRun = 500;

    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static event Action<int>? ExpensesGenerated;
    public static event Action<int>? RevenuesGenerated;

    /// <summary>
    /// Counts the Expenses and Revenue pages read on construction, so a banner survives generation
    /// happening while the user is still on the dashboard.
    /// </summary>
    public static int PendingExpenseCount { get; private set; }

    public static int PendingRevenueCount { get; private set; }

    public static void RaiseGenerated(int expenses, int revenues)
    {
        PendingExpenseCount = expenses;
        PendingRevenueCount = revenues;
        if (expenses > 0) ExpensesGenerated?.Invoke(expenses);
        if (revenues > 0) RevenuesGenerated?.Invoke(revenues);
    }

    public static void ClearPendingExpenses() => PendingExpenseCount = 0;

    public static void ClearPendingRevenues() => PendingRevenueCount = 0;

    /// <summary>
    /// Generates every occurrence due on or before <paramref name="asOfUtc"/>. The converter is
    /// injectable so this does not have to reach for the exchange rate singleton, which is set
    /// once per process and cannot be controlled by a caller.
    /// </summary>
    public static IReadOnlyList<Transaction> GenerateDue(
        CompanyData data, DateTime asOfUtc, UsdConverter? convert = null)
    {
        convert ??= DefaultConverter;

        var generated = new List<Transaction>();
        var asOfDate = asOfUtc.Date;

        foreach (var schedule in data.RecurringTransactions)
        {
            if (schedule.Template == null) continue;
            if (schedule.Status != RecurringTransactionStatus.Active) continue;

            var count = 0;
            while (schedule.NextDate.Date <= asOfDate && count < MaxOccurrencesPerSchedulePerRun)
            {
                if (schedule.EndDate != null && schedule.NextDate.Date > schedule.EndDate.Value.Date)
                {
                    schedule.Status = RecurringTransactionStatus.Completed;
                    break;
                }

                var occurrence = schedule.NextDate.Date;

                var skipped = schedule.SkippedDates.Any(d => d.Date == occurrence);
                if (!skipped && !AlreadyGenerated(data, schedule, occurrence))
                {
                    generated.Add(CloneFor(schedule, occurrence, data, convert));
                    schedule.LastGeneratedAt = asOfUtc;
                }

                schedule.NextDate = RecurrenceSchedule.AdvanceDate(
                    schedule.NextDate, schedule.Frequency, schedule.StartDate.Day);
                count++;

                if (schedule.EndDate != null && schedule.NextDate.Date > schedule.EndDate.Value.Date)
                {
                    schedule.Status = RecurringTransactionStatus.Completed;
                    break;
                }
            }
        }

        return generated;
    }

    /// <summary>
    /// Records an occurrence as skipped. Undoing a generated entry calls this, so undo means
    /// "not this one" rather than having it reappear on the next open.
    /// </summary>
    public static void SkipOccurrence(RecurringTransaction schedule, DateTime occurrence)
    {
        var date = occurrence.Date;
        if (!schedule.SkippedDates.Any(d => d.Date == date))
            schedule.SkippedDates.Add(date);
    }

    public static void UnskipOccurrence(RecurringTransaction schedule, DateTime occurrence)
    {
        schedule.SkippedDates.RemoveAll(d => d.Date == occurrence.Date);
    }

    /// <summary>
    /// Entries this schedule generated that an amount correction may touch. Bank-matched entries
    /// are excluded: rewriting a matched amount breaks the match without telling anyone.
    /// </summary>
    public static IReadOnlyList<Transaction> FindCorrectableOccurrences(
        CompanyData data, RecurringTransaction schedule)
    {
        var source = schedule.Type == CategoryType.Revenue
            ? data.Revenues.Cast<Transaction>()
            : data.Expenses.Cast<Transaction>();

        return source
            .Where(t => t.RecurringScheduleId == schedule.Id && !t.BankMatched)
            .ToList();
    }

    /// <summary>Copies the template's money fields onto occurrences the caller chose.</summary>
    public static void ApplyTemplateAmounts(
        RecurringTransaction schedule, IReadOnlyList<Transaction> targets)
    {
        var template = schedule.Template;
        if (template == null) return;

        foreach (var target in targets)
        {
            target.Amount = template.Amount;
            target.UnitPrice = template.UnitPrice;
            target.Quantity = template.Quantity;
            target.TaxRate = template.TaxRate;
            target.TaxAmount = template.TaxAmount;
            target.ShippingCost = template.ShippingCost;
            target.Discount = template.Discount;
            target.Fee = template.Fee;
            target.Total = template.Total;

            // The USD figures are what the books and every report actually read, so a correction
            // that moved only the entered amount would leave the old converted value behind.
            target.OriginalCurrency = template.OriginalCurrency;
            target.TotalUSD = template.TotalUSD;
            target.UnitPriceUSD = template.UnitPriceUSD;
            target.TaxAmountUSD = template.TaxAmountUSD;
            target.ShippingCostUSD = template.ShippingCostUSD;
            target.DiscountUSD = template.DiscountUSD;
            target.FeeUSD = template.FeeUSD;

            target.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Keys on the schedule and occurrence date rather than a counter, so a restored backup, a
    /// second run, or a crash mid-run cannot produce a second copy.
    /// </summary>
    private static bool AlreadyGenerated(CompanyData data, RecurringTransaction schedule, DateTime occurrence)
    {
        bool Matches(Transaction t) =>
            t.RecurringScheduleId == schedule.Id && t.OccurrenceDate?.Date == occurrence;

        return schedule.Type == CategoryType.Revenue
            ? data.Revenues.Any(Matches)
            : data.Expenses.Any(Matches);
    }

    private static Transaction CloneFor(
        RecurringTransaction schedule, DateTime occurrence, CompanyData data, UsdConverter convert)
    {
        Transaction entry;

        if (schedule.Type == CategoryType.Revenue)
        {
            var revenue = Clone(schedule.RevenueTemplate!);
            data.IdCounters.Revenue++;
            revenue.Id = $"REV-{occurrence:yyyy}-{data.IdCounters.Revenue:D5}";
            data.Revenues.Add(revenue);
            entry = revenue;
        }
        else
        {
            var expense = Clone(schedule.ExpenseTemplate!);
            data.IdCounters.Expense++;
            expense.Id = $"PUR-{occurrence:yyyy}-{data.IdCounters.Expense:D5}";
            data.Expenses.Add(expense);
            entry = expense;
        }

        entry.Date = occurrence;
        entry.OccurrenceDate = occurrence;
        entry.RecurringScheduleId = schedule.Id;
        entry.NeedsReview = true;

        // A template is built from a real transaction, so it can carry links that belong to that
        // one occurrence rather than to the schedule.
        entry.ReceiptId = null;
        entry.BankMatched = false;
        entry.BankMatchedDate = null;
        entry.BankMatchedLineId = null;

        entry.CreatedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;

        ConvertForOccurrence(data, entry, occurrence, convert);
        return entry;
    }

    /// <summary>
    /// The template holds the amount in its own currency converted at the schedule's start date.
    /// Every occurrence falls on a different day, so each one is reconverted at its own date, the
    /// way a hand-entered transaction is. Without this a year of a foreign-currency schedule would
    /// all sit at one stale rate.
    ///
    /// When no rate is held for that date the entry is queued the same way an offline manual entry
    /// is, so PendingConversionService fixes it up rather than the books carrying a wrong figure.
    /// </summary>
    private static void ConvertForOccurrence(
        CompanyData data, Transaction entry, DateTime occurrence, UsdConverter convert)
    {
        if (string.Equals(entry.OriginalCurrency, "USD", StringComparison.OrdinalIgnoreCase))
        {
            entry.IsPendingConversion = false;
            return;
        }

        if (convert(entry.Total, entry.OriginalCurrency, occurrence, out var totalUsd))
        {
            entry.TotalUSD = totalUsd;
            entry.UnitPriceUSD = Rebase(convert, entry.UnitPrice, entry.OriginalCurrency, occurrence);
            entry.TaxAmountUSD = Rebase(convert, entry.TaxAmount, entry.OriginalCurrency, occurrence);
            entry.ShippingCostUSD = Rebase(convert, entry.ShippingCost, entry.OriginalCurrency, occurrence);
            entry.DiscountUSD = Rebase(convert, entry.Discount, entry.OriginalCurrency, occurrence);
            entry.FeeUSD = Rebase(convert, entry.Fee, entry.OriginalCurrency, occurrence);
            entry.IsPendingConversion = false;
            return;
        }

        entry.IsPendingConversion = true;
        data.PendingConversions.Add(new PendingConversion
        {
            TransactionId = entry.Id,
            TransactionType = entry is Models.Transactions.Revenue ? "Revenue" : "Expense",
            OriginalCurrency = entry.OriginalCurrency,
            TransactionDate = occurrence,
            Total = entry.Total,
            TaxAmount = entry.TaxAmount,
            ShippingCost = entry.ShippingCost,
            Discount = entry.Discount,
            Fee = entry.Fee,
            UnitPrice = entry.UnitPrice
        });
    }

    private static decimal Rebase(
        UsdConverter convert, decimal amount, string currency, DateTime date) =>
        amount != 0 && convert(amount, currency, date, out var usd) ? usd : 0m;

    private static bool DefaultConverter(decimal amount, string currency, DateTime date, out decimal usd)
    {
        var rates = ExchangeRateService.Instance;
        if (rates != null)
            return rates.TryConvertToUsdBase(amount, currency, date, out usd);

        usd = 0m;
        return false;
    }

    private static T Clone<T>(T source) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(source, CloneOptions), CloneOptions)!;
}
