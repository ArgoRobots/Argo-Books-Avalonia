using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.AI;
using ArgoBooks.Core.Services;
using ClosedXML.Excel;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for in-cell currency detection: the <see cref="CurrencyImportPreparer"/> pre-pass and
/// its end-to-end effect on a Tier 1 import (per-row OriginalCurrency), plus Tier 2 normalization
/// of a symbol/code the LLM emits.
/// </summary>
public class CurrencyImportPreparerTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var f in _tempFiles)
            try { if (File.Exists(f)) File.Delete(f); } catch { /* best effort */ }
    }

    private string NewTempXlsx() { var p = Path.Combine(Path.GetTempPath(), $"argo-cur-{Guid.NewGuid():N}.xlsx"); _tempFiles.Add(p); return p; }

    /// <summary>A Revenue sheet whose Total cells carry mixed in-cell currencies.</summary>
    private string BuildMixedCurrencyRevenueSheet()
    {
        var path = NewTempXlsx();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Revenue");
        ws.Cell(1, 1).Value = "ID";
        ws.Cell(1, 2).Value = "Date";
        ws.Cell(1, 3).Value = "Total";
        ws.Cell(1, 4).Value = "Description";

        var rows = new[]
        {
            ("R1", "£100",     "London sale"),   // unambiguous -> GBP
            ("R2", "$50 CAD",  "Toronto sale"),  // explicit code -> CAD
            ("R3", "€20",      "Paris sale"),    // unambiguous -> EUR
            ("R4", "$30",      "USD warehouse"), // ambiguous "$" (description has "USD" but is not scanned)
        };
        for (int i = 0; i < rows.Length; i++)
        {
            ws.Cell(i + 2, 1).Value = rows[i].Item1;
            ws.Cell(i + 2, 2).Value = $"2026-01-0{i + 1}";
            ws.Cell(i + 2, 3).Value = rows[i].Item2; // text cells, symbol/code preserved
            ws.Cell(i + 2, 4).Value = rows[i].Item3;
        }
        wb.SaveAs(path);
        return path;
    }

    private static SpreadsheetAnalysisResult RevenueAnalysis() => new()
    {
        Sheets =
        [
            new SheetAnalysis
            {
                SourceSheetName = "Revenue",
                DetectedType = SpreadsheetSheetType.Revenue,
                Tier = ProcessingTier.Tier1_Mapping,
                IsIncluded = true
            }
        ]
    };

    // ─── Pre-pass scan ───────────────────────────────────────────────────────

    [Fact]
    public void ScanWorkbook_ResolvesExplicitAndUnambiguous_FlagsAmbiguousDollar()
    {
        var path = BuildMixedCurrencyRevenueSheet();

        var scan = CurrencyImportPreparer.ScanWorkbook(path, RevenueAnalysis());

        var rows = scan.Resolved["Revenue"];
        Assert.Equal("GBP", rows[0]);
        Assert.Equal("CAD", rows[1]);
        Assert.Equal("EUR", rows[2]);
        Assert.False(rows.ContainsKey(3)); // ambiguous "$" not resolved yet

        var dollar = Assert.Single(scan.Ambiguities);
        Assert.Equal("$", dollar.Symbol);
        Assert.Equal(1, dollar.RowCount);
        Assert.Contains("USD", dollar.Candidates);
    }

    [Fact]
    public void ApplyResolution_FillsAmbiguousRowsFromUserChoice()
    {
        var path = BuildMixedCurrencyRevenueSheet();
        var scan = CurrencyImportPreparer.ScanWorkbook(path, RevenueAnalysis());

        CurrencyImportPreparer.ApplyResolution(scan, new Dictionary<string, string> { ["$"] = "AUD" });

        Assert.Equal("AUD", scan.Resolved["Revenue"][3]);
    }

    // ─── Tier 1 end-to-end ───────────────────────────────────────────────────

    [Fact]
    public async Task Tier1Import_AppliesPerRowCurrency_FromScan()
    {
        var path = BuildMixedCurrencyRevenueSheet();
        var analysis = RevenueAnalysis();

        var scan = CurrencyImportPreparer.ScanWorkbook(path, analysis);
        CurrencyImportPreparer.ApplyResolution(scan, new Dictionary<string, string> { ["$"] = "AUD" });

        var data = new CompanyData();
        data.Settings.Localization.Currency = "USD";
        var svc = new SpreadsheetImportService();
        var options = new ImportOptions { RowCurrencyBySheet = scan.Resolved };

        await svc.ImportWithMappingsAsync(path, data, analysis, options);

        Assert.Equal("GBP", data.Revenues.Single(r => r.Id == "R1").OriginalCurrency);
        Assert.Equal("CAD", data.Revenues.Single(r => r.Id == "R2").OriginalCurrency);
        Assert.Equal("EUR", data.Revenues.Single(r => r.Id == "R3").OriginalCurrency);
        Assert.Equal("AUD", data.Revenues.Single(r => r.Id == "R4").OriginalCurrency);

        // Amounts parsed (symbols stripped) regardless of currency.
        Assert.Equal(100m, data.Revenues.Single(r => r.Id == "R1").Total);
        Assert.Equal(50m, data.Revenues.Single(r => r.Id == "R2").Total);
    }

    [Fact]
    public async Task Tier1Import_NoDetectedCurrency_KeepsCompanyCurrency()
    {
        // A plain-number sheet leaves rows on the company-currency path.
        var path = NewTempXlsx();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Revenue");
            ws.Cell(1, 1).Value = "ID"; ws.Cell(1, 2).Value = "Date"; ws.Cell(1, 3).Value = "Total";
            ws.Cell(2, 1).Value = "R1"; ws.Cell(2, 2).Value = "2026-01-01"; ws.Cell(2, 3).Value = 100;
            wb.SaveAs(path);
        }
        var analysis = RevenueAnalysis();
        var scan = CurrencyImportPreparer.ScanWorkbook(path, analysis);
        Assert.Empty(scan.Ambiguities);

        var data = new CompanyData();
        data.Settings.Localization.Currency = "CAD";
        var svc = new SpreadsheetImportService();
        await svc.ImportWithMappingsAsync(path, data, analysis, new ImportOptions { RowCurrencyBySheet = scan.Resolved });

        var rev = data.Revenues.Single(r => r.Id == "R1");
        Assert.Equal("CAD", rev.OriginalCurrency);
        Assert.Equal(rev.Total, rev.TotalUSD);
    }

    // ─── The shipped sample file imports correctly via Tier 1 ────────────────

    [Fact]
    public async Task SampleFile_MultiCurrency_ImportsWithCorrectCurrencies()
    {
        // Resolve the committed sample under the repo's TestData.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ArgoBooks.sln"))) dir = dir.Parent;
        var path = Path.Combine(dir!.FullName, "TestData", "MainImporter", "multi_currency_sample.xlsx");
        Assert.True(File.Exists(path), $"sample missing: {path}");

        var analysis = new SpreadsheetAnalysisResult
        {
            Sheets =
            [
                new SheetAnalysis { SourceSheetName = "Sales", DetectedType = SpreadsheetSheetType.Revenue, Tier = ProcessingTier.Tier1_Mapping, IsIncluded = true },
                new SheetAnalysis { SourceSheetName = "Expenses", DetectedType = SpreadsheetSheetType.Expenses, Tier = ProcessingTier.Tier1_Mapping, IsIncluded = true },
            ]
        };

        var scan = CurrencyImportPreparer.ScanWorkbook(path, analysis);

        // The $ (twice) and ¥ symbols are flagged as ambiguous; the user resolves them.
        Assert.Contains(scan.Ambiguities, a => a.Symbol == "$");
        Assert.Contains(scan.Ambiguities, a => a.Symbol == "¥");
        CurrencyImportPreparer.ApplyResolution(scan, new Dictionary<string, string> { ["$"] = "USD", ["¥"] = "JPY" });

        var data = new CompanyData();
        data.Settings.Localization.Currency = "USD";
        var svc = new SpreadsheetImportService();
        await svc.ImportWithMappingsAsync(path, data, analysis, new ImportOptions { RowCurrencyBySheet = scan.Resolved });

        // Expenses: amounts non-zero, currency detected from the cell format/code, and the
        // record is internally consistent (line-item amount == total, so the edit modal agrees).
        var e1 = data.Expenses.Single(e => e.Id == "E1");
        Assert.Equal(140m, e1.Total);
        Assert.Equal(140m, e1.Amount);
        Assert.Equal("GBP", e1.OriginalCurrency);
        Assert.Equal("CAD", data.Expenses.Single(e => e.Id == "E2").OriginalCurrency);
        Assert.Equal("USD", data.Expenses.Single(e => e.Id == "E3").OriginalCurrency); // resolved $
        Assert.Equal(1500m, data.Expenses.Single(e => e.Id == "E4").Total);           // plain number

        // Revenue: a spread of currencies, all amounts non-zero and consistent.
        Assert.Equal("GBP", data.Revenues.Single(r => r.Id == "R1").OriginalCurrency);
        Assert.Equal("EUR", data.Revenues.Single(r => r.Id == "R2").OriginalCurrency);
        Assert.Equal("CAD", data.Revenues.Single(r => r.Id == "R3").OriginalCurrency);
        Assert.Equal("JPY", data.Revenues.Single(r => r.Id == "R4").OriginalCurrency); // resolved ¥
        Assert.Equal(800m, data.Revenues.Single(r => r.Id == "R2").Total);
        Assert.All(data.Revenues, r => Assert.True(r.Total > 0 && r.Amount == r.Total,
            $"{r.Id}: total={r.Total} amount={r.Amount} should be equal and > 0"));
    }

    // ─── Tier 2 normalization (LLM-emitted symbol/code) ──────────────────────

    [Fact]
    public void Tier2_NormalizesUnambiguousSymbol_ToCode()
    {
        var data = new CompanyData();
        data.Settings.Localization.Currency = "USD";
        var svc = new SpreadsheetImportService();

        var row = JsonDocument.Parse(
            """{ "id": "EXP-1", "date": "2026-03-15", "total": 100, "description": "x", "originalCurrency": "£" }""")
            .RootElement.Clone();

        var chunk = new LlmProcessedData { EntityType = SpreadsheetSheetType.Expenses };
        chunk.Entities.Add(row);
        svc.ImportProcessedEntities(data, [chunk], "Expenses");

        Assert.Equal("GBP", data.Expenses.Single(e => e.Id == "EXP-1").OriginalCurrency);
    }

    [Fact]
    public void Tier2_ResolvesAmbiguousSymbol_ViaSymbolResolution()
    {
        var data = new CompanyData();
        data.Settings.Localization.Currency = "USD";
        var svc = new SpreadsheetImportService();
        var options = new ImportOptions { SymbolResolution = new() { ["$"] = "CAD" } };

        var row = JsonDocument.Parse(
            """{ "id": "EXP-2", "date": "2026-03-15", "total": 100, "description": "x", "originalCurrency": "$" }""")
            .RootElement.Clone();

        var chunk = new LlmProcessedData { EntityType = SpreadsheetSheetType.Expenses };
        chunk.Entities.Add(row);
        svc.ImportProcessedEntities(data, [chunk], "Expenses", options);

        Assert.Equal("CAD", data.Expenses.Single(e => e.Id == "EXP-2").OriginalCurrency);
    }
}
