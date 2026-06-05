using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.BankMatching;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class BankMatchingServiceTests
{
    private static CompanyData NewCompany() => new();

    private static BankStatementLine Line(decimal amount, DateTime date, string desc) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Date = date,
        Amount = amount,
        Description = desc,
        MatchStatus = BankLineMatchStatus.Unmatched
    };

    [Fact]
    public void MatchDeterministic_ExactAmountAndDate_AutoMatchesExpenseAndSetsFlag()
    {
        var date = new DateTime(2025, 1, 5);
        var data = NewCompany();
        var expense = new Expense { Id = "EXP-1", Total = 263.38m, Date = date, Description = "Milwaukee Supply" };
        data.Expenses.Add(expense);

        var line = Line(-263.38m, date, "MILWAUKEE SUPPLY CO POS");
        var result = new BankMatchingService().MatchDeterministic([line], data, new BankMatchingOptions());

        Assert.Equal(BankLineMatchStatus.Matched, line.MatchStatus);
        Assert.Equal(1, result.AutoMatchedCount);
        Assert.True(expense.BankMatched);
        Assert.Equal(line.Id, expense.BankMatchedLineId);
        Assert.Equal(BookRecordType.Expense, line.MatchedRecordType);
        Assert.Equal("EXP-1", line.MatchedRecordId);
    }

    [Fact]
    public void MatchDeterministic_ExactAmountWithinWindowNoDesc_Suggests()
    {
        var data = NewCompany();
        // 3 days apart, no descriptive overlap => below auto threshold, above suggest threshold.
        data.Expenses.Add(new Expense { Id = "EXP-1", Total = 100m, Date = new DateTime(2025, 1, 1), Description = "zzzz" });

        var line = Line(-100m, new DateTime(2025, 1, 4), "qqqq");
        var result = new BankMatchingService().MatchDeterministic([line], data, new BankMatchingOptions());

        Assert.Equal(BankLineMatchStatus.Suggested, line.MatchStatus);
        Assert.Equal(1, result.SuggestedCount);
        Assert.True(result.CandidatesByLineId.ContainsKey(line.Id));
        Assert.False(data.Expenses[0].BankMatched);
    }

    [Fact]
    public void MatchDeterministic_NoAmountMatch_LineUnmatchedAndRecordListedAsUnmatched()
    {
        var data = NewCompany();
        data.Expenses.Add(new Expense { Id = "EXP-1", Total = 999m, Date = new DateTime(2025, 1, 1), Description = "unrelated" });

        var line = Line(-100m, new DateTime(2025, 1, 1), "anything");
        var result = new BankMatchingService().MatchDeterministic([line], data, new BankMatchingOptions());

        Assert.Equal(BankLineMatchStatus.Unmatched, line.MatchStatus);
        Assert.Equal(1, result.UnmatchedLineCount);
        Assert.Contains(result.UnmatchedBookRecords, r => r.Id == "EXP-1");
    }

    [Fact]
    public void MatchDeterministic_AlreadyMatchedRecord_IsNotReused()
    {
        var date = new DateTime(2025, 1, 5);
        var data = NewCompany();
        data.Expenses.Add(new Expense { Id = "EXP-1", Total = 50m, Date = date, Description = "x", BankMatched = true });

        var line = Line(-50m, date, "x");
        var result = new BankMatchingService().MatchDeterministic([line], data, new BankMatchingOptions());

        Assert.Equal(BankLineMatchStatus.Unmatched, line.MatchStatus);
    }

    [Fact]
    public void MatchDeterministic_MoneyIn_MatchesRevenue()
    {
        var date = new DateTime(2025, 2, 10);
        var data = NewCompany();
        data.Revenues.Add(new Revenue { Id = "REV-1", Total = 1343.75m, Date = date, Description = "Alice Johnson" });

        var line = Line(1343.75m, date, "ACH DEPOSIT ALICE JOHNSON");
        new BankMatchingService().MatchDeterministic([line], data, new BankMatchingOptions());

        Assert.Equal(BankLineMatchStatus.Matched, line.MatchStatus);
        Assert.True(data.Revenues[0].BankMatched);
    }

    [Fact]
    public void ConfirmThenUnlink_TogglesRecordFlag()
    {
        var data = NewCompany();
        var expense = new Expense { Id = "EXP-1", Total = 100m, Date = new DateTime(2025, 1, 1), Description = "x" };
        data.Expenses.Add(expense);

        var line = Line(-100m, new DateTime(2025, 1, 1), "x");
        var candidate = new BankMatchCandidate
        {
            LineId = line.Id, RecordType = BookRecordType.Expense, RecordId = "EXP-1", Confidence = 0.8
        };
        var svc = new BankMatchingService();

        svc.ConfirmMatch(line, candidate, data);
        Assert.True(expense.BankMatched);
        Assert.Equal(BankLineMatchStatus.Matched, line.MatchStatus);

        svc.UnlinkMatch(line, data);
        Assert.False(expense.BankMatched);
        Assert.Null(expense.BankMatchedLineId);
        Assert.Equal(BankLineMatchStatus.Unmatched, line.MatchStatus);
    }
}
