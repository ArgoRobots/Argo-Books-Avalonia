using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// What survives an export followed by an import.
///
/// The spreadsheet export is a report and the backup is the restore path, but people do export
/// and re-import, and until now that quietly changed the books: every invoice came back with a
/// hash in front of its id and no lines on it, every multi-unit expense came back as one unit,
/// and every foreign-currency transaction came back as company currency.
///
/// These go through the real Excel writer and the real Excel reader rather than testing either
/// half in isolation, because each of those bugs lived in the seam between them and the two
/// halves were individually self-consistent.
/// </summary>
public class SpreadsheetRoundTripFidelityTests : IDisposable
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(), $"roundtrip_{Guid.NewGuid():N}.xlsx");

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }

        GC.SuppressFinalize(this);
    }

    private static readonly string[] AllSheets =
    [
        "Customers", "Suppliers", "Products", "Invoices", "Invoice Line Items",
        "Payments", "Expenses", "Revenue", "Purchase Orders", "Purchase Order Line Items",
    ];

    private static CompanyData Source()
    {
        var data = new CompanyData();
        data.Settings.Localization.Currency = "USD";

        data.Customers.Add(new Customer { Id = "CUS-003", Name = "Jane Doe", Email = "jane@x.com" });
        data.Suppliers.Add(new Supplier { Id = "SUP-014", Name = "Acme Hardware" });
        data.Products.Add(new Product { Id = "PRD-001", Name = "Widget" });

        data.Invoices.Add(new Invoice
        {
            Id = "INV-2026-00001",
            InvoiceNumber = "#INV-2026-00001",
            CustomerId = "CUS-003",
            IssueDate = new DateTime(2026, 5, 4),
            DueDate = new DateTime(2026, 6, 4),
            Subtotal = 300m,
            TaxAmount = 15m,
            Total = 315m,
            Status = Core.Enums.InvoiceStatus.Sent,
            LineItems =
            {
                new LineItem { ProductId = "PRD-001", Description = "Widget", Quantity = 2m, UnitPrice = 100m, TaxRate = 0.05m },
                new LineItem { Description = "Delivery", Quantity = 1m, UnitPrice = 100m, TaxRate = 0.05m, Discount = 0m },
            },
        });

        data.Payments.Add(new Payment
        {
            Id = "PAY-001",
            InvoiceId = "INV-2026-00001",
            CustomerId = "CUS-003",
            Date = new DateTime(2026, 5, 11),
            Amount = 100m,
        });

        data.Expenses.Add(new Expense
        {
            Id = "PUR-001",
            Date = new DateTime(2026, 5, 4),
            SupplierId = "SUP-014",
            Description = "Screws",
            Quantity = 3m,
            UnitPrice = 12m,
            Amount = 36m,
            TaxAmount = 1.80m,
            ShippingCost = 5m,
            Total = 42.80m,
            ReferenceNumber = "RCPT-88",
        });

        data.Revenues.Add(new Revenue
        {
            Id = "REV-001",
            Date = new DateTime(2026, 5, 6),
            CustomerId = "CUS-003",
            Description = "Consulting",
            Quantity = 4m,
            UnitPrice = 50m,
            Amount = 200m,
            Total = 200m,
        });

        return data;
    }

    private async Task<CompanyData> RoundTripAsync(CompanyData source)
    {
        await new SpreadsheetExportService().ExportToExcelAsync(_path, source, [.. AllSheets], null, null);

        var target = new CompanyData();
        target.Settings.Localization.Currency = "USD";
        await new SpreadsheetImportService().ImportFromExcelAsync(_path, target);

        return target;
    }

    #region Invoices

    [Fact]
    public async Task AnInvoiceKeepsItsIdInsteadOfGainingAHash()
    {
        // The export wrote only InvoiceNumber (#INV-...), the importer read that column as the
        // id, so every round trip prefixed the id with a hash and orphaned its payments.
        CompanyData target = await RoundTripAsync(Source());

        Invoice invoice = Assert.Single(target.Invoices);

        Assert.Equal("INV-2026-00001", invoice.Id);
        Assert.Equal("#INV-2026-00001", invoice.InvoiceNumber);
    }

    [Fact]
    public async Task APaymentStillPointsAtItsInvoice()
    {
        CompanyData target = await RoundTripAsync(Source());

        Payment payment = Assert.Single(target.Payments);

        Assert.Contains(target.Invoices, i => i.Id == payment.InvoiceId);
    }

    [Fact]
    public async Task AnInvoiceKeepsItsLines()
    {
        // Before the Invoice Line Items sheet existed this came back as a set of totals with
        // nothing behind them, so the invoice could no longer be reprinted or edited.
        CompanyData target = await RoundTripAsync(Source());

        Invoice invoice = Assert.Single(target.Invoices);

        Assert.Equal(2, invoice.LineItems.Count);

        LineItem widget = invoice.LineItems[0];
        Assert.Equal("PRD-001", widget.ProductId);
        Assert.Equal("Widget", widget.Description);
        Assert.Equal(2m, widget.Quantity);
        Assert.Equal(100m, widget.UnitPrice);
        Assert.Equal(0.05m, widget.TaxRate);
    }

    [Fact]
    public async Task ImportingTheSameFileTwice_DoesNotDoubleTheLines()
    {
        // Re-running an import after fixing something in the sheet is normal. Appending rather
        // than replacing would silently double every invoice.
        CompanyData source = Source();
        await new SpreadsheetExportService().ExportToExcelAsync(_path, source, [.. AllSheets], null, null);

        var target = new CompanyData();
        target.Settings.Localization.Currency = "USD";

        var importer = new SpreadsheetImportService();
        await importer.ImportFromExcelAsync(_path, target);
        await importer.ImportFromExcelAsync(_path, target);

        Assert.Equal(2, Assert.Single(target.Invoices).LineItems.Count);
    }

    [Fact]
    public async Task ASheetThatOnlyHasAnInvoiceNumber_StillImports()
    {
        // Spreadsheets from other systems have no ID column, and that is how this app's own
        // export used to look. The number has to keep working as the identifier.
        var source = new CompanyData();
        source.Settings.Localization.Currency = "USD";
        source.Customers.Add(new Customer { Id = "CUS-003", Name = "Jane Doe" });
        source.Invoices.Add(new Invoice
        {
            Id = "INV-9",
            InvoiceNumber = "INV-9",
            CustomerId = "CUS-003",
            IssueDate = new DateTime(2026, 5, 4),
            Total = 10m,
        });

        CompanyData target = await RoundTripAsync(source);

        Invoice invoice = Assert.Single(target.Invoices);
        Assert.Equal("INV-9", invoice.Id);
        Assert.Equal("INV-9", invoice.InvoiceNumber);
    }

    #endregion

    #region Quantities

    [Fact]
    public async Task AMultiUnitExpense_ComesBackWithItsQuantity()
    {
        // The Unit Price column was being fed Amount, and there was no Quantity column at all,
        // so three units at $12 came back as one unit at $36.
        CompanyData target = await RoundTripAsync(Source());

        Expense expense = Assert.Single(target.Expenses);

        Assert.Equal(3m, expense.Quantity);
        Assert.Equal(12m, expense.UnitPrice);
        Assert.Equal(36m, expense.Amount);
    }

    [Fact]
    public async Task AMultiUnitRevenueRow_ComesBackWithItsQuantity()
    {
        CompanyData target = await RoundTripAsync(Source());

        Revenue revenue = target.Revenues.Single(r => r.Id == "REV-001");

        Assert.Equal(4m, revenue.Quantity);
        Assert.Equal(50m, revenue.UnitPrice);
    }

    [Fact]
    public async Task AnExpenseKeepsItsShippingAndReference()
    {
        CompanyData target = await RoundTripAsync(Source());

        Expense expense = Assert.Single(target.Expenses);

        Assert.Equal(5m, expense.ShippingCost);
        Assert.Equal("RCPT-88", expense.ReferenceNumber);
    }

    #endregion

    #region Currency

    [Theory]
    [InlineData("Invoices")]
    [InlineData("Expenses")]
    [InlineData("Revenue")]
    [InlineData("Payments")]
    [InlineData("Purchase Orders")]
    public async Task EverySheetThatCanCarryACurrency_PrintsIt(string sheet)
    {
        // The importer has accepted a Currency column on all five of these for a long time; the
        // export simply never wrote one. A sheet showing 1,200 with no indication it is euros is
        // a wrong report before it is a lossy one.
        CompanyData source = Source();
        source.PurchaseOrders.Add(new PurchaseOrder
        {
            Id = "PO-001",
            SupplierId = "SUP-014",
            OrderDate = new DateTime(2026, 5, 2),
            Total = 240m,
        });

        foreach (Expense e in source.Expenses) e.OriginalCurrency = "EUR";
        foreach (Revenue r in source.Revenues) r.OriginalCurrency = "EUR";
        foreach (Invoice i in source.Invoices) i.OriginalCurrency = "EUR";
        foreach (Payment p in source.Payments) p.OriginalCurrency = "EUR";
        foreach (PurchaseOrder p in source.PurchaseOrders) p.OriginalCurrency = "EUR";

        string csv = Path.ChangeExtension(_path, ".csv");

        try
        {
            await new SpreadsheetExportService().ExportToCsvAsync(csv, source, [sheet], null, null);
            List<List<string>> rows = CsvReader.ReadAllRows(csv, out List<string> headers);

            int index = headers.IndexOf("Currency");
            Assert.True(index >= 0, $"no Currency column on {sheet}: {string.Join(", ", headers)}");
            Assert.All(rows, r => Assert.Equal("EUR", r[index]));
        }
        finally
        {
            File.Delete(csv);
        }
    }

    [Fact]
    public async Task ATransactionInTheCompanysOwnCurrency_KeepsItThroughARoundTrip()
    {
        CompanyData source = Source();
        source.Settings.Localization.Currency = "CAD";
        foreach (Expense e in source.Expenses) e.OriginalCurrency = "CAD";

        await new SpreadsheetExportService().ExportToExcelAsync(_path, source, [.. AllSheets], null, null);

        var target = new CompanyData();
        target.Settings.Localization.Currency = "CAD";
        await new SpreadsheetImportService().ImportFromExcelAsync(_path, target);

        Assert.Equal("CAD", Assert.Single(target.Expenses).OriginalCurrency);
    }

    #endregion

    #region Purchase orders

    [Fact]
    public async Task APurchaseOrderKeepsItsLines()
    {
        // The export service could always write this sheet. It was simply never offered in the
        // export modal, so in practice purchase orders exported with nothing on them.
        CompanyData source = Source();
        source.PurchaseOrders.Add(new PurchaseOrder
        {
            Id = "PO-001",
            SupplierId = "SUP-014",
            OrderDate = new DateTime(2026, 5, 2),
            Total = 240m,
            LineItems =
            {
                new PurchaseOrderLineItem { ProductId = "PRD-001", Quantity = 20, UnitCost = 12m },
            },
        });

        CompanyData target = await RoundTripAsync(source);

        PurchaseOrder order = Assert.Single(target.PurchaseOrders);
        PurchaseOrderLineItem line = Assert.Single(order.LineItems);

        Assert.Equal("PRD-001", line.ProductId);
        Assert.Equal(20, line.Quantity);
    }

    #endregion
}
