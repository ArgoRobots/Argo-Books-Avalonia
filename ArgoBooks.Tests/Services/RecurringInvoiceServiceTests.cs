using System;
using System.Linq;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for <see cref="RecurringInvoiceService"/> cadence math and idempotent catch-up generation.
/// </summary>
public class RecurringInvoiceServiceTests
{
    private static RecurringInvoice MakeSchedule(
        DateTime nextDate,
        Frequency freq = Frequency.Monthly,
        DateTime? endDate = null,
        RecurringInvoiceStatus status = RecurringInvoiceStatus.Active,
        string paymentTerms = "Net 30",
        bool withTemplate = true)
        => new()
        {
            Id = "REC-INV-00001",
            CustomerId = "CUST-1",
            Frequency = freq,
            StartDate = nextDate,
            NextInvoiceDate = nextDate,
            EndDate = endDate,
            Status = status,
            PaymentTerms = paymentTerms,
            Template = withTemplate ? new Invoice { CustomerId = "CUST-1", Total = 100m } : null
        };

    [Theory]
    [InlineData(Frequency.Weekly, 7)]
    [InlineData(Frequency.BiWeekly, 14)]
    public void AdvanceDate_AddsDays(Frequency freq, int days)
    {
        var start = new DateTime(2026, 3, 1);
        Assert.Equal(start.AddDays(days), RecurringInvoiceService.AdvanceDate(start, freq));
    }

    [Fact]
    public void AdvanceDate_Monthly_ClampsEndOfMonth()
    {
        Assert.Equal(
            new DateTime(2026, 2, 28),
            RecurringInvoiceService.AdvanceDate(new DateTime(2026, 1, 31), Frequency.Monthly));
    }

    [Fact]
    public void AdvanceDate_QuarterlyAndAnnually()
    {
        Assert.Equal(
            new DateTime(2026, 4, 1),
            RecurringInvoiceService.AdvanceDate(new DateTime(2026, 1, 1), Frequency.Quarterly));
        Assert.Equal(
            new DateTime(2027, 1, 1),
            RecurringInvoiceService.AdvanceDate(new DateTime(2026, 1, 1), Frequency.Annually));
    }

    [Theory]
    [InlineData("Net 30", 30)]
    [InlineData("Net 15", 15)]
    [InlineData("Net 7", 7)]
    [InlineData("Due on receipt", 0)]
    [InlineData("nonsense", 30)]
    [InlineData(null, 30)]
    public void PaymentTermsDays_Parses(string? terms, int days)
        => Assert.Equal(days, RecurringInvoiceService.PaymentTermsDays(terms));

    [Fact]
    public void GenerateDueInvoices_PastDates_GeneratesEachDueOccurrenceAndAdvances()
    {
        var data = new CompanyData();
        // Jan 15 and Feb 15 are on/before Feb 20; Mar 15 is not.
        data.RecurringInvoices.Add(MakeSchedule(new DateTime(2026, 1, 15), Frequency.Monthly));

        var generated = RecurringInvoiceService.GenerateDueInvoices(data, new DateTime(2026, 2, 20));

        Assert.Equal(2, generated.Count);
        Assert.Equal(2, data.Invoices.Count);
        Assert.Equal(new DateTime(2026, 3, 15), data.RecurringInvoices[0].NextInvoiceDate);
        Assert.All(generated, i => Assert.Equal(InvoiceStatus.Draft, i.Status));
        Assert.All(generated, i => Assert.Equal("REC-INV-00001", i.RecurringInvoiceId));
        Assert.All(generated, i => Assert.StartsWith("INV-", i.Id));
        Assert.Equal(generated[0].IssueDate.AddDays(30), generated[0].DueDate); // Net 30
    }

    [Fact]
    public void GenerateDueInvoices_IsIdempotent()
    {
        var data = new CompanyData();
        data.RecurringInvoices.Add(MakeSchedule(new DateTime(2026, 1, 15), Frequency.Monthly));

        var asOf = new DateTime(2026, 2, 20);
        var first = RecurringInvoiceService.GenerateDueInvoices(data, asOf);
        var second = RecurringInvoiceService.GenerateDueInvoices(data, asOf);

        Assert.Equal(2, first.Count);
        Assert.Empty(second);
        Assert.Equal(2, data.Invoices.Count);
    }

    [Fact]
    public void GenerateDueInvoices_PausedSchedule_GeneratesNothing()
    {
        var data = new CompanyData();
        data.RecurringInvoices.Add(MakeSchedule(new DateTime(2026, 1, 15), status: RecurringInvoiceStatus.Paused));

        var generated = RecurringInvoiceService.GenerateDueInvoices(data, new DateTime(2026, 6, 1));

        Assert.Empty(generated);
        Assert.Empty(data.Invoices);
    }

    [Fact]
    public void GenerateDueInvoices_PastEndDate_CompletesAfterLastOccurrence()
    {
        var data = new CompanyData();
        // Ends the same day as the first occurrence: generate that one, then complete.
        data.RecurringInvoices.Add(MakeSchedule(
            new DateTime(2026, 1, 15), Frequency.Monthly, endDate: new DateTime(2026, 1, 15)));

        var generated = RecurringInvoiceService.GenerateDueInvoices(data, new DateTime(2026, 6, 1));

        Assert.Single(generated);
        Assert.Equal(RecurringInvoiceStatus.Completed, data.RecurringInvoices[0].Status);
    }

    [Fact]
    public void GenerateDueInvoices_NullTemplate_IsSkipped()
    {
        var data = new CompanyData();
        data.RecurringInvoices.Add(MakeSchedule(new DateTime(2026, 1, 15), withTemplate: false));

        var generated = RecurringInvoiceService.GenerateDueInvoices(data, new DateTime(2026, 6, 1));

        Assert.Empty(generated);
    }
}
