using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class BankStatementSchemaTests
{
    [Fact]
    public void Schema_HasBankStatementType_WithRequiredDateAndDescription()
    {
        var schema = ImportSchemaDefinition.GetSchemaForType(SpreadsheetSheetType.BankStatement);

        Assert.NotNull(schema);
        Assert.Contains(schema!, c => c.Name == "Date" && c.Required);
        Assert.Contains(schema!, c => c.Name == "Description" && c.Required);
        Assert.Contains(schema!, c => c.Name == "Amount");
        Assert.Contains(schema!, c => c.Name == "Debit");
        Assert.Contains(schema!, c => c.Name == "Credit");
    }
}
