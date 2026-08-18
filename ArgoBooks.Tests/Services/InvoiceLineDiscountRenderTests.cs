using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services.InvoiceTemplates;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// A discounted line shows what it actually came to.
///
/// The Amount column was printed as quantity times price, ignoring the per-line discount, while
/// the invoice Subtotal underneath was summed from the discounted figures. On a discounted
/// invoice the lines did not add up to the total sitting directly beneath them, which is the
/// first thing a customer checks and the kind of error that gets an invoice disputed rather
/// than paid.
/// </summary>
public class InvoiceLineDiscountRenderTests
{
    private readonly InvoiceHtmlRenderer _renderer = new();

    private static Invoice Discounted()
    {
        var line = new LineItem
        {
            Description = "Consulting",
            Quantity = 4m,
            UnitPrice = 100m,
            Discount = 150m,
        };

        return new Invoice
        {
            Id = "INV-2026-00001",
            InvoiceNumber = "INV-2026-00001",
            CustomerId = "CUS-001",
            IssueDate = new DateTime(2026, 8, 14),
            LineItems = { line },
            Subtotal = line.Subtotal,
            Total = line.Subtotal,
        };
    }

    private string Html(Invoice invoice) =>
        _renderer.RenderInvoice(invoice, InvoiceTemplateFactory.CreateProfessionalTemplate(), new CompanyData());

    /// <summary>
    /// Line subtotal is quantity times price less the discount, floored at zero. Everything the
    /// invoice prints is built from it, so it is worth stating outright.
    /// </summary>
    [Fact]
    public void ALineSubtotal_IsThePriceLessTheDiscount()
    {
        Assert.Equal(250m, Discounted().LineItems[0].Subtotal);
    }

    [Fact]
    public void TheRenderedLine_ShowsTheDiscountedAmount()
    {
        string html = Html(Discounted());

        Assert.Contains("250.00", html, StringComparison.Ordinal);
        Assert.DoesNotContain("400.00", html, StringComparison.Ordinal);
    }

    /// <summary>The lines reconcile with the subtotal, which is the whole point.</summary>
    [Fact]
    public void TheRenderedLines_AddUpToTheSubtotal()
    {
        Invoice invoice = Discounted();

        invoice.LineItems.Add(new LineItem
        {
            Description = "Materials",
            Quantity = 2m,
            UnitPrice = 50m,
        });

        invoice.Subtotal = invoice.LineItems.Sum(l => l.Subtotal);
        invoice.Total = invoice.Subtotal;

        Assert.Equal(350m, invoice.Subtotal);

        string html = Html(invoice);

        Assert.Contains("250.00", html, StringComparison.Ordinal);
        Assert.Contains("100.00", html, StringComparison.Ordinal);
        Assert.Contains("350.00", html, StringComparison.Ordinal);
    }

    /// <summary>An undiscounted line is unaffected, or the fix has changed the ordinary case.</summary>
    [Fact]
    public void AnUndiscountedLine_StillShowsQuantityTimesPrice()
    {
        var invoice = new Invoice
        {
            Id = "INV-2026-00002",
            InvoiceNumber = "INV-2026-00002",
            CustomerId = "CUS-001",
            IssueDate = new DateTime(2026, 8, 14),
            LineItems = { new LineItem { Description = "Consulting", Quantity = 4m, UnitPrice = 100m } },
            Subtotal = 400m,
            Total = 400m,
        };

        Assert.Contains("400.00", Html(invoice), StringComparison.Ordinal);
    }

    /// <summary>
    /// A discount larger than the line cannot print a negative amount: the subtotal floors at
    /// zero and the rendered figure has to follow it.
    /// </summary>
    [Fact]
    public void ADiscountBiggerThanTheLine_PrintsZeroRatherThanANegative()
    {
        var line = new LineItem { Description = "Goodwill", Quantity = 1m, UnitPrice = 100m, Discount = 250m };

        var invoice = new Invoice
        {
            Id = "INV-2026-00003",
            InvoiceNumber = "INV-2026-00003",
            CustomerId = "CUS-001",
            IssueDate = new DateTime(2026, 8, 14),
            LineItems = { line },
            Subtotal = line.Subtotal,
            Total = line.Subtotal,
        };

        Assert.Equal(0m, line.Subtotal);
        Assert.DoesNotContain("-100.00", Html(invoice), StringComparison.Ordinal);
    }
}
