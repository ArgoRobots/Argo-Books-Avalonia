using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// A payment whose invoice is not in the file is still cash.
///
/// Both reports counted only payments that HAD an invoice id, on the reasoning that a
/// revenue-linked payment is already counted by its own Revenue row. An imported payments sheet
/// whose invoices were never imported has neither id, so the money showed on the Payments page
/// and was missing from the balance sheet and the cash flow statement. The reports simply
/// disagreed with the ledger, with nothing to say why.
///
/// The test that matters is the pair: the orphan has to count, and the revenue-linked payment
/// still must not, or fixing one breaks the other.
/// </summary>
public class ImportedPaymentCashTests
{
    private static ReportFilters Filters() => new()
    {
        StartDate = new DateTime(2026, 1, 1),
        EndDate = new DateTime(2026, 12, 31),
    };

    private static Payment Paid(string id, decimal amount, string? invoiceId = null, string? revenueId = null) => new()
    {
        Id = id,
        Amount = amount,
        Date = new DateTime(2026, 6, 15),
        InvoiceId = invoiceId ?? string.Empty,
        RevenueId = revenueId ?? string.Empty,
    };

    private static decimal Cash(CompanyData data)
    {
        AccountingTableData table =
            new AccountingReportDataService(data, Filters()).GetReportData(AccountingReportType.BalanceSheet);

        return Money(table.Rows.First(r => r.Label.StartsWith("Cash", StringComparison.Ordinal)).Values[0]);
    }

    private static decimal CashFromPayments(CompanyData data)
    {
        AccountingTableData table =
            new AccountingReportDataService(data, Filters()).GetReportData(AccountingReportType.CashFlowStatement);

        return Money(table.Rows.First(r => r.Label.Contains("Invoice Payments", StringComparison.Ordinal)).Values[0]);
    }

    private static decimal Money(string formatted) =>
        decimal.Parse(new string([.. formatted.Where(c => char.IsAsciiDigit(c) || c is '.' or '-')]),
                      System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void APaymentWithNoInvoice_CountsAsCashOnTheBalanceSheet()
    {
        var data = new CompanyData();
        data.Payments.Add(Paid("PAY-0001", 500m));

        Assert.Equal(500m, Cash(data));
    }

    [Fact]
    public void APaymentWithNoInvoice_CountsOnTheCashFlowStatement()
    {
        var data = new CompanyData();
        data.Payments.Add(Paid("PAY-0001", 500m));

        Assert.Equal(500m, CashFromPayments(data));
    }

    /// <summary>
    /// The exclusion that has to survive: a payment recorded against a Revenue row is the same
    /// money as that row, and counting both doubles the sale.
    /// </summary>
    [Fact]
    public void ARevenueLinkedPayment_IsStillLeftOut()
    {
        var data = new CompanyData();
        data.Payments.Add(Paid("PAY-0001", 500m, revenueId: "REV-0001"));

        Assert.Equal(0m, Cash(data));
        Assert.Equal(0m, CashFromPayments(data));
    }

    [Fact]
    public void AnInvoiceLinkedPayment_StillCounts()
    {
        var data = new CompanyData();
        data.Payments.Add(Paid("PAY-0001", 500m, invoiceId: "INV-2026-00001"));

        Assert.Equal(500m, Cash(data));
        Assert.Equal(500m, CashFromPayments(data));
    }

    /// <summary>The two reports have to agree with each other, whatever the mix.</summary>
    [Fact]
    public void TheTwoReports_AgreeOnWhatCounted()
    {
        var data = new CompanyData();
        data.Payments.Add(Paid("PAY-0001", 500m));
        data.Payments.Add(Paid("PAY-0002", 250m, invoiceId: "INV-2026-00001"));
        data.Payments.Add(Paid("PAY-0003", 900m, revenueId: "REV-0001"));

        Assert.Equal(750m, Cash(data));
        Assert.Equal(750m, CashFromPayments(data));
    }
}
