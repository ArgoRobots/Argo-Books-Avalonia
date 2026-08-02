using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
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
}
