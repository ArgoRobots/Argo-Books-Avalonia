using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// A schedule generates real transactions flagged for review, so the books stay complete while
/// each occurrence is still put in front of someone.
/// </summary>
public class RecurringTransactionServiceTests
{
    private static (CompanyData data, RecurringTransaction schedule) WithMonthlyRent(DateTime start)
    {
        var data = new CompanyData();
        var schedule = new RecurringTransaction
        {
            Id = "REC-TXN-00001",
            Type = CategoryType.Expense,
            Frequency = Frequency.Monthly,
            StartDate = start,
            NextDate = start,
            ExpenseTemplate = new Expense { Description = "Rent", Amount = 2000m, Total = 2000m }
        };
        data.RecurringTransactions.Add(schedule);
        return (data, schedule);
    }

    [Fact]
    public void NextRecurringTransactionId_UsesTheRecTxnPrefix()
    {
        var data = new CompanyData();
        var ids = new IdGenerator(data);

        Assert.Equal("REC-TXN-00001", ids.NextRecurringTransactionId());
        Assert.Equal("REC-TXN-00002", ids.NextRecurringTransactionId());
    }

    [Fact]
    public void GenerateDue_AfterALongGap_ProducesOneEntryPerMissedOccurrence()
    {
        var (data, _) = WithMonthlyRent(new DateTime(2026, 1, 1));

        var generated = RecurringTransactionService.GenerateDue(data, new DateTime(2026, 5, 15));

        Assert.Equal(5, generated.Count);
        Assert.Equal(5, data.Expenses.Count);
        Assert.All(data.Expenses, e => Assert.True(e.NeedsReview));
        Assert.Equal(new DateTime(2026, 6, 1), data.RecurringTransactions[0].NextDate);
    }

    [Fact]
    public void GenerateDue_RunTwice_ProducesNothingTheSecondTime()
    {
        var (data, _) = WithMonthlyRent(new DateTime(2026, 1, 1));
        RecurringTransactionService.GenerateDue(data, new DateTime(2026, 5, 15));

        var second = RecurringTransactionService.GenerateDue(data, new DateTime(2026, 5, 15));

        Assert.Empty(second);
        Assert.Equal(5, data.Expenses.Count);
    }

    [Fact]
    public void GenerateDue_RewoundSchedule_DoesNotDuplicateExistingOccurrences()
    {
        var (data, schedule) = WithMonthlyRent(new DateTime(2026, 1, 1));
        RecurringTransactionService.GenerateDue(data, new DateTime(2026, 5, 15));

        schedule.NextDate = new DateTime(2026, 3, 1);
        var again = RecurringTransactionService.GenerateDue(data, new DateTime(2026, 5, 15));

        Assert.Empty(again);
        Assert.Equal(5, data.Expenses.Count);
    }

    [Fact]
    public void GenerateDue_SkippedDate_AdvancesWithoutGenerating()
    {
        var (data, schedule) = WithMonthlyRent(new DateTime(2026, 1, 1));
        schedule.SkippedDates.Add(new DateTime(2026, 2, 1));

        RecurringTransactionService.GenerateDue(data, new DateTime(2026, 3, 15));

        Assert.Equal(2, data.Expenses.Count);
        Assert.DoesNotContain(data.Expenses, e => e.OccurrenceDate == new DateTime(2026, 2, 1));
    }

    [Fact]
    public void GenerateDue_PastTheEndDate_MarksTheScheduleCompleted()
    {
        var (data, schedule) = WithMonthlyRent(new DateTime(2026, 1, 1));
        schedule.EndDate = new DateTime(2026, 2, 28);

        RecurringTransactionService.GenerateDue(data, new DateTime(2026, 6, 1));

        Assert.Equal(2, data.Expenses.Count);
        Assert.Equal(RecurringTransactionStatus.Completed, schedule.Status);
    }

    [Fact]
    public void GenerateDue_CorruptFarPastDate_StopsAtTheCap()
    {
        var (data, schedule) = WithMonthlyRent(new DateTime(1900, 1, 1));
        schedule.Frequency = Frequency.Weekly;

        var generated = RecurringTransactionService.GenerateDue(data, new DateTime(2026, 1, 1));

        Assert.Equal(RecurringTransactionService.MaxOccurrencesPerSchedulePerRun, generated.Count);
    }

    [Fact]
    public void GenerateDue_RevenueSchedule_AddsToRevenues()
    {
        var (data, schedule) = WithMonthlyRent(new DateTime(2026, 1, 1));
        schedule.Type = CategoryType.Revenue;
        schedule.ExpenseTemplate = null;
        schedule.RevenueTemplate = new Revenue { Description = "Retainer", Amount = 500m, Total = 500m };

        RecurringTransactionService.GenerateDue(data, new DateTime(2026, 2, 15));

        Assert.Equal(2, data.Revenues.Count);
        Assert.Empty(data.Expenses);
    }

    [Fact]
    public void GenerateDue_PausedSchedule_GeneratesNothing()
    {
        var (data, schedule) = WithMonthlyRent(new DateTime(2026, 1, 1));
        schedule.Status = RecurringTransactionStatus.Paused;

        Assert.Empty(RecurringTransactionService.GenerateDue(data, new DateTime(2026, 5, 1)));
    }

    [Fact]
    public void GenerateDue_DoesNotCarryReceiptOrBankMatchFromTheTemplate()
    {
        var (data, schedule) = WithMonthlyRent(new DateTime(2026, 1, 1));
        schedule.ExpenseTemplate!.ReceiptId = "RCP-001";
        schedule.ExpenseTemplate.BankMatched = true;

        RecurringTransactionService.GenerateDue(data, new DateTime(2026, 1, 15));

        var entry = Assert.Single(data.Expenses);
        Assert.Null(entry.ReceiptId);
        Assert.False(entry.BankMatched);
    }

    [Fact]
    public void SkipOccurrence_RecordsTheDateOnce()
    {
        var (_, schedule) = WithMonthlyRent(new DateTime(2026, 1, 1));

        RecurringTransactionService.SkipOccurrence(schedule, new DateTime(2026, 2, 1));
        RecurringTransactionService.SkipOccurrence(schedule, new DateTime(2026, 2, 1));

        Assert.Single(schedule.SkippedDates);
    }

    [Fact]
    public void UnskipOccurrence_RemovesTheDate()
    {
        var (_, schedule) = WithMonthlyRent(new DateTime(2026, 1, 1));
        RecurringTransactionService.SkipOccurrence(schedule, new DateTime(2026, 2, 1));

        RecurringTransactionService.UnskipOccurrence(schedule, new DateTime(2026, 2, 1));

        Assert.Empty(schedule.SkippedDates);
    }

    [Fact]
    public void FindCorrectableOccurrences_ExcludesBankMatchedEntries()
    {
        var (data, schedule) = WithMonthlyRent(new DateTime(2026, 1, 1));
        RecurringTransactionService.GenerateDue(data, new DateTime(2026, 3, 15));
        data.Expenses[0].BankMatched = true;

        var correctable = RecurringTransactionService.FindCorrectableOccurrences(data, schedule);

        Assert.Equal(2, correctable.Count);
        Assert.DoesNotContain(data.Expenses[0], correctable);
    }

    [Fact]
    public void FindCorrectableOccurrences_IgnoresEntriesFromOtherSchedules()
    {
        var (data, schedule) = WithMonthlyRent(new DateTime(2026, 1, 1));
        RecurringTransactionService.GenerateDue(data, new DateTime(2026, 1, 15));
        data.Expenses.Add(new Expense { Id = "PUR-2026-09999", RecurringScheduleId = "REC-TXN-00002" });

        Assert.Single(RecurringTransactionService.FindCorrectableOccurrences(data, schedule));
    }

    [Fact]
    public void ApplyTemplateAmounts_UpdatesOnlyTheGivenEntries()
    {
        var (data, schedule) = WithMonthlyRent(new DateTime(2026, 1, 1));
        RecurringTransactionService.GenerateDue(data, new DateTime(2026, 3, 15));
        data.Expenses[0].BankMatched = true;
        schedule.ExpenseTemplate!.Amount = 2200m;
        schedule.ExpenseTemplate.Total = 2200m;

        var correctable = RecurringTransactionService.FindCorrectableOccurrences(data, schedule);
        RecurringTransactionService.ApplyTemplateAmounts(schedule, correctable);

        Assert.Equal(2000m, data.Expenses[0].Total);
        Assert.Equal(2200m, data.Expenses[1].Total);
        Assert.Equal(2200m, data.Expenses[2].Total);
    }
}
