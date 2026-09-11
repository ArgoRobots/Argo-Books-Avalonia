using System.Reflection;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Drives the real PaymentModalsViewModel save/undo/redo flows against an in-memory company. Guards
/// the currency-tagging fix (a payment must settle its invoice in the invoice's currency) and the
/// undo/redo round-trip for recorded payments.
/// </summary>
public class PaymentModalsViewModelTests : ModalViewModelTestBase
{
    private Invoice AddInvoice(string currency, decimal total)
    {
        var invoice = new Invoice
        {
            Id = "INV-001",
            InvoiceNumber = "INV-001",
            CustomerId = "CUST-1",
            OriginalCurrency = currency,
            Subtotal = total,
            Total = total,
            TotalUSD = total,
            Balance = total,
            Status = InvoiceStatus.Sent,
            IssueDate = new DateTime(2026, 1, 5),
            DueDate = new DateTime(2026, 2, 5)
        };
        Company.Invoices.Add(invoice);
        return invoice;
    }

    private static PaymentModalsViewModel NewPaymentFor(string invoiceId, string amount)
    {
        var vm = new PaymentModalsViewModel
        {
            ModalInvoiceId = invoiceId,
            ModalAmount = amount,
            ModalPaymentMethod = "Cash",
            ModalDate = new DateTimeOffset(new DateTime(2026, 1, 10), TimeSpan.Zero)
        };
        return vm;
    }

    [Fact]
    public async Task SaveNewPayment_ForeignCurrencyInvoice_TagsInvoiceCurrencyAndCounts()
    {
        var invoice = AddInvoice("EUR", 119m);

        await NewPaymentFor(invoice.Id, "119").SaveNewPayment();

        var payment = Assert.Single(Company.Payments);
        // The fix: the payment settles the EUR invoice in EUR, not the company display currency.
        Assert.Equal("EUR", payment.OriginalCurrency);
        // Because the currency matches, InvoiceTotalsService counts it toward the invoice.
        Assert.Equal(119m, invoice.AmountPaid);
        Assert.Equal(0m, invoice.Balance);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public async Task SaveNewPayment_PartialPayment_SetsPartialStatus()
    {
        var invoice = AddInvoice("USD", 200m);

        await NewPaymentFor(invoice.Id, "50").SaveNewPayment();

        Assert.Equal(50m, invoice.AmountPaid);
        Assert.Equal(150m, invoice.Balance);
        Assert.Equal(InvoiceStatus.Partial, invoice.Status);
    }

    [Fact]
    public async Task SaveNewPayment_ThenUndo_RemovesPaymentAndRestoresBalance()
    {
        var invoice = AddInvoice("USD", 100m);
        await NewPaymentFor(invoice.Id, "100").SaveNewPayment();
        Assert.Single(Company.Payments);

        Undo();

        Assert.Empty(Company.Payments);
        Assert.Equal(0m, invoice.AmountPaid);
        Assert.Equal(100m, invoice.Balance);
    }

    [Fact]
    public async Task SaveNewPayment_UndoThenRedo_RestoresPayment()
    {
        var invoice = AddInvoice("USD", 100m);
        await NewPaymentFor(invoice.Id, "100").SaveNewPayment();
        Undo();

        Redo();

        var payment = Assert.Single(Company.Payments);
        Assert.Equal(100m, payment.Amount);
        Assert.Equal(100m, invoice.AmountPaid);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    private Revenue AddInvoiceRevenue(Invoice invoice)
    {
        var revenue = new Revenue
        {
            Id = "REV-001",
            InvoiceId = invoice.Id,
            Date = invoice.IssueDate,
            OriginalCurrency = invoice.OriginalCurrency,
            Total = invoice.Total,
            TotalUSD = invoice.TotalUSD,
            PaymentStatus = RevenuePaymentStatus.Unpaid
        };
        Company.Revenues.Add(revenue);
        return revenue;
    }

    /// <summary>
    /// Sending an invoice creates its revenue unpaid, and only the online payment path marked it
    /// collected, so an invoice paid by cash or cheque never reached revenue or profit.
    /// </summary>
    [Fact]
    public async Task SaveNewPayment_PayingTheInvoiceInFull_CountsItsRevenue()
    {
        var invoice = AddInvoice("USD", 100m);
        var revenue = AddInvoiceRevenue(invoice);

        await NewPaymentFor(invoice.Id, "100").SaveNewPayment();
        Assert.Equal(RevenuePaymentStatus.Paid, revenue.PaymentStatus);

        Undo();
        Assert.Equal(RevenuePaymentStatus.Unpaid, revenue.PaymentStatus);

        Redo();
        Assert.Equal(RevenuePaymentStatus.Paid, revenue.PaymentStatus);
    }

    [Fact]
    public async Task SaveNewPayment_PayingPartOfTheInvoice_LeavesItsRevenueUncollected()
    {
        var invoice = AddInvoice("USD", 100m);
        var revenue = AddInvoiceRevenue(invoice);

        await NewPaymentFor(invoice.Id, "40").SaveNewPayment();

        Assert.Equal(RevenuePaymentStatus.Unpaid, revenue.PaymentStatus);
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }

    /// <summary>No exchange rate service at all, so no rate for any date.</summary>
    private static IDisposable NoExchangeRates()
    {
        var instance = typeof(ExchangeRateService).GetProperty(nameof(ExchangeRateService.Instance), BindingFlags.Public | BindingFlags.Static)!;
        var prior = instance.GetValue(null);
        instance.SetValue(null, null);
        return new Restore(() => instance.SetValue(null, prior));
    }

    /// <summary>
    /// With no rate for the payment's date, the foreign amount was stored as the USD figure, so a
    /// ¥50,000 payment counted as $50,000. It now waits for its own date's rate (Rule 3a).
    /// </summary>
    [Fact]
    public async Task SaveNewPayment_NoRateForItsDate_WaitsForConversionInsteadOfCountingAsUsd()
    {
        using var noRates = NoExchangeRates();
        var invoice = AddInvoice("JPY", 100000m);

        await NewPaymentFor(invoice.Id, "50000").SaveNewPayment();

        var payment = Assert.Single(Company.Payments);
        Assert.True(payment.IsPendingConversion);
        Assert.Equal(0m, payment.EffectiveAmountUSD);
        var queued = Assert.Single(Company.PendingConversions);
        Assert.Equal((payment.Id, "Payment", "JPY", 50000m), (queued.TransactionId, queued.TransactionType, queued.OriginalCurrency, queued.Total));

        Undo();
        Assert.Empty(Company.PendingConversions);

        Redo();
        Assert.Equal(payment.Id, Assert.Single(Company.PendingConversions).TransactionId);
    }
}
