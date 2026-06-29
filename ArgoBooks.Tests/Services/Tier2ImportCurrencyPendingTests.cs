using System.Linq;
using System.Text.Json;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.AI;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// The Tier 2 (AI/LLM) import path must defer unpriceable foreign-currency Payments, Invoices, and
/// PurchaseOrders the same way the Tier 1 path does: mark them IsPendingConversion and enqueue them so
/// PendingConversionService heals them later, instead of silently storing 0 USD forever. With no
/// exchange-rate service wired up, every non-USD row is unpriceable, which exercises that branch.
/// </summary>
public class Tier2ImportCurrencyPendingTests
{
    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    private static LlmProcessedData Chunk(SpreadsheetSheetType type, params JsonElement[] entities)
    {
        var chunk = new LlmProcessedData { EntityType = type };
        foreach (var e in entities) chunk.Entities.Add(e);
        return chunk;
    }

    [Fact]
    public void Tier2_ForeignPayment_Unpriceable_IsPendingAndEnqueued()
    {
        var data = new CompanyData();
        var svc = new SpreadsheetImportService();

        var payment = Json("""{ "id": "PAY-1", "amount": 100, "originalCurrency": "EUR" }""");
        svc.ImportProcessedEntities(data, [Chunk(SpreadsheetSheetType.Payments, payment)], "Payments");

        var imported = Assert.Single(data.Payments);
        Assert.True(imported.IsPendingConversion);
        Assert.Equal(0m, imported.AmountUSD);
        Assert.Equal("EUR", imported.OriginalCurrency);
        Assert.Contains(data.PendingConversions, p => p.TransactionId == "PAY-1" && p.TransactionType == "Payment");
    }

    [Fact]
    public void Tier2_ForeignInvoice_Unpriceable_IsPendingAndEnqueued()
    {
        var data = new CompanyData();
        var svc = new SpreadsheetImportService();

        var invoice = Json("""{ "id": "INV-1", "total": 200, "balance": 80, "originalCurrency": "EUR" }""");
        svc.ImportProcessedEntities(data, [Chunk(SpreadsheetSheetType.Invoices, invoice)], "Invoices");

        var imported = Assert.Single(data.Invoices);
        Assert.True(imported.IsPendingConversion);
        Assert.Equal(0m, imported.TotalUSD);
        Assert.Equal("EUR", imported.OriginalCurrency);
        var entry = Assert.Single(data.PendingConversions, p => p.TransactionId == "INV-1");
        Assert.Equal("Invoice", entry.TransactionType);
        Assert.Equal(200m, entry.Total);
        Assert.Equal(80m, entry.Balance); // balance carried so the heal can convert it too
    }

    [Fact]
    public void Tier2_ForeignPurchaseOrder_Unpriceable_IsPendingAndEnqueued()
    {
        var data = new CompanyData();
        var svc = new SpreadsheetImportService();

        var po = Json("""{ "id": "PO-1", "total": 250, "originalCurrency": "EUR" }""");
        svc.ImportProcessedEntities(data, [Chunk(SpreadsheetSheetType.PurchaseOrders, po)], "Purchase Orders");

        var imported = Assert.Single(data.PurchaseOrders);
        Assert.True(imported.IsPendingConversion);
        Assert.Equal(0m, imported.TotalUSD);
        Assert.Equal("EUR", imported.OriginalCurrency);
        Assert.Contains(data.PendingConversions, p => p.TransactionId == "PO-1" && p.TransactionType == "PurchaseOrder");
    }

    [Fact]
    public void Tier2_UsdPayment_NotPending()
    {
        var data = new CompanyData();
        var svc = new SpreadsheetImportService();

        var payment = Json("""{ "id": "PAY-2", "amount": 100, "originalCurrency": "USD" }""");
        svc.ImportProcessedEntities(data, [Chunk(SpreadsheetSheetType.Payments, payment)], "Payments");

        var imported = Assert.Single(data.Payments);
        Assert.False(imported.IsPendingConversion);
        Assert.Equal(100m, imported.AmountUSD);
        Assert.Empty(data.PendingConversions);
    }
}
