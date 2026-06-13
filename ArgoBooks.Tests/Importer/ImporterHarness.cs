using System.Text.Json;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Services;
using ArgoBooks.Tests.Importer.Models;

namespace ArgoBooks.Tests.Importer;

public sealed record HarnessReport(bool Passed, string FailureMessage);

public static class ImporterHarness
{
    public static string CorpusRoot =>
        Path.Combine(FindRepoRoot(), "TestData", "MainImporter", "corpus");

    public static IEnumerable<string> EnumerateFixtureDirectories() =>
        Directory.Exists(CorpusRoot)
            ? Directory.EnumerateDirectories(CorpusRoot).OrderBy(p => p)
            : [];

    public static async Task<HarnessReport> RunTrackAAsync(string fixtureDir)
    {
        var expected = JsonSerializer.Deserialize<ExpectedResult>(
            await File.ReadAllTextAsync(Path.Combine(fixtureDir, "expected.json")))!;
        var responses = JsonSerializer.Deserialize<Dictionary<string, string>>(
            await File.ReadAllTextAsync(Path.Combine(fixtureDir, "responses.json")))!;
        var inputPath = Directory.EnumerateFiles(fixtureDir, "input.*").Single();

        var fake = new ScriptedGeminiService(responses);
        var data = new CompanyData();

        var analysis = inputPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            ? await new SpreadsheetAnalysisService(fake).AnalyzeCsvAsync(inputPath)
            : await new SpreadsheetAnalysisService(fake).AnalyzeAsync(inputPath);

        var failures = new List<string>();
        if (analysis == null) return new HarnessReport(false, "analysis returned null");

        // 1) classification + tier per expected sheet
        foreach (var es in expected.Sheets)
        {
            var got = analysis.Sheets.FirstOrDefault(s => s.SourceSheetName == es.Name);
            if (got == null) { failures.Add($"sheet '{es.Name}' missing from analysis"); continue; }
            if (got.DetectedType.ToString() != es.DetectedType)
                failures.Add($"sheet '{es.Name}' type {got.DetectedType} != {es.DetectedType}");
            if (got.Tier.ToString() != es.Tier)
                failures.Add($"sheet '{es.Name}' tier {got.Tier} != {es.Tier}");
        }

        // 2) import Tier 1 sheets and assert counts/records
        var importSvc = new SpreadsheetImportService(null, null, fake);
        var options = new ImportOptions { AutoCreateMissingReferences = true };
        var result = await importSvc.ImportWithMappingsAsync(inputPath, data, analysis, options);

        if (result.TotalImported != expected.Import.TotalImported)
            failures.Add($"totalImported {result.TotalImported} != {expected.Import.TotalImported}");
        if (result.TotalUpdated != expected.Import.TotalUpdated)
            failures.Add($"totalUpdated {result.TotalUpdated} != {expected.Import.TotalUpdated}");

        foreach (var kr in expected.KeyRecords)
        {
            if (!KeyRecordPresent(data, kr))
                failures.Add($"missing key record {kr.Type} {kr.Id}");
        }

        foreach (var sub in expected.ExpectedDropReasonSubstrings)
        {
            var allReasons = result.SheetResults.SelectMany(r => r.SkipReasons);
            if (!allReasons.Any(r => r.Contains(sub, StringComparison.OrdinalIgnoreCase)))
                failures.Add($"expected a drop reason containing '{sub}'");
        }

        return failures.Count == 0
            ? new HarnessReport(true, "")
            : new HarnessReport(false, string.Join("; ", failures));
    }

    private static bool KeyRecordPresent(CompanyData data, ExpectedKeyRecord kr) => kr.Type switch
    {
        "Customer" => data.Customers.Any(c => c.Id == kr.Id && (kr.Name == null || c.Name == kr.Name)),
        "Supplier" => data.Suppliers.Any(s => s.Id == kr.Id),
        "Product"  => data.Products.Any(p => p.Id == kr.Id),
        "Expense"  => data.Expenses.Any(e => e.Id == kr.Id),
        "Revenue"  => data.Revenues.Any(r => r.Id == kr.Id),
        "Invoice"  => data.Invoices.Any(i => i.Id == kr.Id),
        _ => false
    };

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "ArgoBooks.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        return dir ?? throw new DirectoryNotFoundException("ArgoBooks.sln not found above test output dir");
    }
}
