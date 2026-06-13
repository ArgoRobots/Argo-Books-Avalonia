using Xunit;
using ArgoBooks.Core.Services;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.AI;
using ArgoBooks.Core.Enums;

namespace ArgoBooks.Tests.Services;

public class UnimportedRowsTests
{
    [Fact]
    public void ImportProcessedEntities_RecordsUnimportedRowForMissingId()
    {
        var data = new CompanyData();
        var svc = new SpreadsheetImportService();
        var chunk = new LlmProcessedData
        {
            EntityType = SpreadsheetSheetType.Customers,
            Entities = { System.Text.Json.JsonDocument.Parse("{\"name\":\"NoId\"}").RootElement.Clone() }
        };
        var result = svc.ImportProcessedEntities(data, [chunk], "Customers");
        Assert.Single(result.UnimportedRows);
        Assert.Contains("ID", result.UnimportedRows[0].Reason, System.StringComparison.OrdinalIgnoreCase);
    }
}
