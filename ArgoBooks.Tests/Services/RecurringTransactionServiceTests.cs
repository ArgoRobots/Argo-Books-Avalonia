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
}
