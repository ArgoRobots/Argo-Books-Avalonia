using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.AI;
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
}
