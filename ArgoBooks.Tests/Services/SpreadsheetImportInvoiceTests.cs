using System.Reflection;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the invoice rows of the spreadsheet importer. ImportInvoices is private (it's an
/// internal step of the larger import pipeline), so it's invoked here via reflection.
/// </summary>
public class SpreadsheetImportInvoiceTests
{
    private static void ImportInvoices(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        var svc = new SpreadsheetImportService();
        var method = typeof(SpreadsheetImportService).GetMethod(
            "ImportInvoices", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(svc, [data, headers, rows, null]);
    }

    [Fact]
    public void ImportInvoices_BalanceColumnPresentNoPaidColumn_KeepsImportedBalance()
    {
        // A third-party sheet supplies Total and Balance but no "Paid" column. The importer must not
        // assume nothing was paid and overwrite the imported Balance with the full Total.
        var data = new CompanyData();
        var headers = new List<string> { "Invoice #", "Total", "Balance" };
        var rows = new List<List<object?>>
        {
            new() { "INV-1", 1000m, 600m }
        };

        ImportInvoices(data, headers, rows);

        Assert.Single(data.Invoices);
        Assert.Equal(600m, data.Invoices[0].Balance);
    }

    [Fact]
    public void ImportInvoices_DayFirstIssueDate_ParsesInsteadOfDefaultingToMinValue()
    {
        // A UK/EU date like 15/03/2023 (the 15th) can't be read month-first, so the invariant parse
        // fails and the date silently became DateTime.MinValue (0001-01-01). Because 15 can't be a
        // month, the day-first reading is unambiguous and should be used.
        var data = new CompanyData();
        var headers = new List<string> { "Invoice #", "Issue Date", "Total" };
        var rows = new List<List<object?>>
        {
            new() { "INV-1", "15/03/2023", 100m }
        };

        ImportInvoices(data, headers, rows);

        Assert.Single(data.Invoices);
        Assert.Equal(new DateTime(2023, 3, 15), data.Invoices[0].IssueDate);
    }
}
