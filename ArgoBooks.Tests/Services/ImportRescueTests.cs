using System.IO;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.AI;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class ImportRescueTests
{
    private static RescueSheetResult Extracted(string sheet, int entities)
    {
        var data = new LlmProcessedData { EntityType = SpreadsheetSheetType.Expenses };
        for (int i = 0; i < entities; i++)
            data.Entities.Add(System.Text.Json.JsonDocument.Parse("{}").RootElement.Clone());
        return new RescueSheetResult { SheetName = sheet, ProcessedData = { data } };
    }

    private static RescueSheetResult Rejected(string sheet, ImportRescueRejectionReason reason)
        => new() { SheetName = sheet, Reason = reason };

    [Fact]
    public void Aggregate_AnyExtracted_OutcomeIsExtracted()
    {
        var result = ImportRescueResult.Aggregate([
            Rejected("A", ImportRescueRejectionReason.SummaryOrReport),
            Extracted("B", 3),
        ]);

        Assert.Equal(ImportRescueOutcome.Extracted, result.Outcome);
        Assert.Single(result.Extractions);
        Assert.Equal("B", result.Extractions[0].SheetName);
    }

    [Fact]
    public void Aggregate_AllRejected_UsesMostCommonReason()
    {
        var result = ImportRescueResult.Aggregate([
            Rejected("A", ImportRescueRejectionReason.SummaryOrReport),
            Rejected("B", ImportRescueRejectionReason.SummaryOrReport),
            Rejected("C", ImportRescueRejectionReason.NotArgoData),
        ]);

        Assert.Equal(ImportRescueOutcome.Rejected, result.Outcome);
        Assert.Equal(ImportRescueRejectionReason.SummaryOrReport, result.ReasonCode);
    }

    [Fact]
    public void Aggregate_ReasonTie_ResolvesToUnsupportedStructure()
    {
        var result = ImportRescueResult.Aggregate([
            Rejected("A", ImportRescueRejectionReason.SummaryOrReport),
            Rejected("B", ImportRescueRejectionReason.NotArgoData),
        ]);

        Assert.Equal(ImportRescueRejectionReason.UnsupportedStructure, result.ReasonCode);
    }

    [Fact]
    public void Aggregate_NoSheets_IsEmptyOrUnreadable()
    {
        var result = ImportRescueResult.Aggregate([]);

        Assert.Equal(ImportRescueOutcome.Rejected, result.Outcome);
        Assert.Equal(ImportRescueRejectionReason.EmptyOrUnreadable, result.ReasonCode);
    }

    [Fact]
    public void Aggregate_ClassifiedButEmpty_CountsAsUnsupportedStructure()
    {
        // A sheet that classified as a type but yielded zero entities carries Reason=UnsupportedStructure
        // (set by RescueAsync); confirm the fold treats it as a rejection, not an extraction.
        var result = ImportRescueResult.Aggregate([
            Rejected("A", ImportRescueRejectionReason.UnsupportedStructure),
        ]);

        Assert.Equal(ImportRescueOutcome.Rejected, result.Outcome);
        Assert.Equal(ImportRescueRejectionReason.UnsupportedStructure, result.ReasonCode);
    }

    private static RescueClassification InvokeParse(string response)
    {
        var method = typeof(ArgoBooks.Core.Services.SpreadsheetAnalysisService).GetMethod(
            "ParseRescueClassification",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (RescueClassification)method.Invoke(null, [response])!;
    }

    [Fact]
    public void ParseRescueClassification_Extract_ReturnsType()
    {
        var r = InvokeParse("""{"action":"extract","entityType":"Expenses"}""");
        Assert.Equal(SpreadsheetSheetType.Expenses, r.EntityType);
        Assert.Null(r.Reason);
    }

    [Fact]
    public void ParseRescueClassification_ExtractUnknownType_FallsBackToUnsupported()
    {
        var r = InvokeParse("""{"action":"extract","entityType":"Unknown"}""");
        Assert.Null(r.EntityType);
        Assert.Equal(ImportRescueRejectionReason.UnsupportedStructure, r.Reason);
    }

    [Fact]
    public void ParseRescueClassification_Reject_ReturnsReason()
    {
        var r = InvokeParse("""{"action":"reject","reason":"SummaryOrReport"}""");
        Assert.Null(r.EntityType);
        Assert.Equal(ImportRescueRejectionReason.SummaryOrReport, r.Reason);
    }

    [Fact]
    public void ParseRescueClassification_UnknownReason_FallsBackToUnsupported()
    {
        var r = InvokeParse("""{"action":"reject","reason":"WeirdMadeUpCode"}""");
        Assert.Equal(ImportRescueRejectionReason.UnsupportedStructure, r.Reason);
    }

    [Fact]
    public void ParseRescueClassification_Malformed_FallsBackToUnsupported()
    {
        var r = InvokeParse("this is not json");
        Assert.Null(r.EntityType);
        Assert.Equal(ImportRescueRejectionReason.UnsupportedStructure, r.Reason);
    }

    // Mock that returns different payloads for the classify call vs the Tier-2 extraction call,
    // distinguished by a stable marker in the rescue classify system prompt.
    private sealed class BranchingMockGeminiService : IGeminiService
    {
        private readonly string _classifyJson;
        private readonly string _entitiesJson;
        public BranchingMockGeminiService(string classifyJson, string entitiesJson)
        {
            _classifyJson = classifyJson;
            _entitiesJson = entitiesJson;
        }

        public bool IsConfigured => true;

        public Task<string?> SendChatAsync(
            string systemPrompt, string userPrompt, int maxTokens = 4000, double temperature = 0.1,
            CancellationToken cancellationToken = default,
            OperationKind operation = OperationKind.Completion, long? sizeFeature = null)
        {
            var isClassify = systemPrompt.Contains("Decide ONE of two things", StringComparison.Ordinal);
            return Task.FromResult<string?>(isClassify ? _classifyJson : _entitiesJson);
        }

        public Task<string?> SendVisionChatAsync(
            string systemPrompt, string userPrompt, string base64Image, string mimeType,
            int maxTokens = 4000, double temperature = 0.1, string? model = null,
            CancellationToken cancellationToken = default,
            OperationKind operation = OperationKind.ReceiptScan)
            => Task.FromResult<string?>(null);

        public Task<SupplierCategorySuggestion?> GetSupplierCategorySuggestionAsync(
            ReceiptAnalysisRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<SupplierCategorySuggestion?>(null);

        public Task<List<BankLineSuggestion>?> GetBankLineSuggestionsAsync(
            BankLineCategorizationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<List<BankLineSuggestion>?>(null);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ArgoBooks.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    [Fact]
    public async Task RescueAsync_SummaryReport_RejectsWithReason()
    {
        var mock = new BranchingMockGeminiService(
            classifyJson: """{"action":"reject","reason":"SummaryOrReport"}""",
            entitiesJson: "[]");
        var service = new SpreadsheetAnalysisService(mock);
        var path = Path.Combine(RepoRoot(), "TestData", "MainImporter", "quickbooks_profit_and_loss.xlsx");
        Assert.True(File.Exists(path), $"fixture missing: {path}");

        var result = await service.RescueAsync(path, isCsv: false);

        Assert.Equal(ImportRescueOutcome.Rejected, result.Outcome);
        Assert.Equal(ImportRescueRejectionReason.SummaryOrReport, result.ReasonCode);
        Assert.Empty(result.Extractions);
    }

    [Fact]
    public async Task RescueAsync_TransactionRows_ExtractsEntities()
    {
        var mock = new BranchingMockGeminiService(
            classifyJson: """{"action":"extract","entityType":"Expenses"}""",
            entitiesJson: """[{"id":"E1","date":"2026-01-05","description":"Fuel","total":50.0}]""");
        var service = new SpreadsheetAnalysisService(mock);

        var csv = Path.Combine(Path.GetTempPath(), $"rescue_{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(csv, "Date,Vendor,Amount\n2026-01-05,Shell,50.00\n");
        try
        {
            var result = await service.RescueAsync(csv, isCsv: true);

            Assert.Equal(ImportRescueOutcome.Extracted, result.Outcome);
            Assert.Single(result.Extractions);
            Assert.True(result.Extractions[0].ProcessedData.Sum(d => d.Entities.Count) >= 1);
        }
        finally
        {
            File.Delete(csv);
        }
    }

    [Fact]
    public async Task RescueAsync_TooLarge_RejectsWithoutTrying()
    {
        // Over the hard cap: reject with TooLarge and make no AI extraction attempt.
        var mock = new BranchingMockGeminiService(
            classifyJson: """{"action":"extract","entityType":"Expenses"}""",
            entitiesJson: """[{"id":"E1"}]""");
        var service = new SpreadsheetAnalysisService(mock);

        var csv = Path.Combine(Path.GetTempPath(), $"rescue_big_{Guid.NewGuid():N}.csv");
        var sb = new System.Text.StringBuilder("Date,Vendor,Amount\n");
        for (int i = 0; i <= SpreadsheetAnalysisService.RescueMaxTotalRows; i++)
            sb.Append("2026-01-05,Shell,1.00\n");
        await File.WriteAllTextAsync(csv, sb.ToString());
        try
        {
            var result = await service.RescueAsync(csv, isCsv: true);

            Assert.Equal(ImportRescueOutcome.Rejected, result.Outcome);
            Assert.Equal(ImportRescueRejectionReason.TooLarge, result.ReasonCode);
        }
        finally
        {
            File.Delete(csv);
        }
    }

    [Theory]
    [InlineData(5, 100)]    // 2500/5 = 500, clamped to 100
    [InlineData(50, 50)]    // 2500/50 = 50
    [InlineData(200, 20)]   // 2500/200 = 12, clamped up to 20
    [InlineData(0, 100)]    // guard against divide-by-zero
    public void RescueChunkSize_ScalesWithWidth(int columns, int expected)
        => Assert.Equal(expected, SpreadsheetAnalysisService.RescueChunkSize(columns));
}
