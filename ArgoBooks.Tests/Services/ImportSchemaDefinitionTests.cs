using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class ImportSchemaDefinitionTests
{
    [Fact]
    public void GetSchema_ContainsAllEntityTypes()
    {
        var schema = ImportSchemaDefinition.GetSchema();
        var allTypes = Enum.GetValues<SpreadsheetSheetType>();

        foreach (var entityType in allTypes)
        {
            Assert.True(schema.ContainsKey(entityType), $"Schema missing definition for {entityType}");
        }
    }

    [Fact]
    public void GetSchema_AllTypesHaveAtLeastOneColumn()
    {
        var schema = ImportSchemaDefinition.GetSchema();

        foreach (var (entityType, columns) in schema)
        {
            Assert.True(columns.Count > 0, $"Schema for {entityType} has no columns");
        }
    }

    [Fact]
    public void GetSchema_AllColumnsHaveNameAndType()
    {
        var schema = ImportSchemaDefinition.GetSchema();

        foreach (var (entityType, columns) in schema)
        {
            foreach (var col in columns)
            {
                Assert.False(string.IsNullOrWhiteSpace(col.Name),
                    $"Schema column in {entityType} has empty name");
                Assert.False(string.IsNullOrWhiteSpace(col.Type),
                    $"Schema column '{col.Name}' in {entityType} has empty type");
            }
        }
    }

    [Fact]
    public void GetSchema_CustomersHasRequiredColumns()
    {
        var schema = ImportSchemaDefinition.GetSchema();
        var customers = schema[SpreadsheetSheetType.Customers];

        var requiredColumns = customers.Where(c => c.Required).Select(c => c.Name).ToList();
        Assert.Contains("ID", requiredColumns);
        Assert.Contains("Name", requiredColumns);
    }

    [Fact]
    public void GetSchema_InvoicesHasRequiredColumns()
    {
        var schema = ImportSchemaDefinition.GetSchema();
        var invoices = schema[SpreadsheetSheetType.Invoices];

        var columnNames = invoices.Select(c => c.Name).ToList();
        Assert.Contains("Invoice Number", columnNames);
        Assert.Contains("Customer", columnNames);
        Assert.Contains("Total", columnNames);
    }

    [Fact]
    public void FormatSchemaForPrompt_ReturnsNonEmptyString()
    {
        var prompt = ImportSchemaDefinition.FormatSchemaForPrompt();

        Assert.False(string.IsNullOrWhiteSpace(prompt));
        Assert.Contains("Customers", prompt);
        Assert.Contains("Invoices", prompt);
        Assert.Contains("Expenses", prompt);
    }

    [Fact]
    public void FormatSchemaForPrompt_ContainsAllEntityTypes()
    {
        var prompt = ImportSchemaDefinition.FormatSchemaForPrompt();
        var allTypes = Enum.GetValues<SpreadsheetSheetType>();

        foreach (var entityType in allTypes)
        {
            Assert.Contains(entityType.ToString(), prompt);
        }
    }

    [Fact]
    public void GetSchema_NoDuplicateColumnNames()
    {
        var schema = ImportSchemaDefinition.GetSchema();

        foreach (var (entityType, columns) in schema)
        {
            var names = columns.Select(c => c.Name).ToList();
            var distinctNames = names.Distinct().ToList();
            Assert.Equal(names.Count, distinctNames.Count);
        }
    }
}
