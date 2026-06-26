using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.BankMatching;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class BankLineImportServiceTests
{
    [Fact]
    public void CreateFromLines_MoneyOut_AttachesProduct_AndMarksLineMatched()
    {
        var data = new CompanyData();
        data.Products.Add(new Product { Id = "PRD-001", Name = "Office Supplies", Type = CategoryType.Expense, CategoryId = "CAT-PUR-001" });

        var line = new BankStatementLine { Id = "L1", Date = new DateTime(2026, 4, 5), Description = "AMZN MKTP", Amount = -38.20m };
        var resolution = new BankLineResolution { Line = line, Type = BookRecordType.Expense, ProductId = "PRD-001" };

        var result = new BankLineImportService().CreateFromLines(data, [resolution]);

        Assert.Single(data.Expenses);
        Assert.Equal(38.20m, data.Expenses[0].Total);
        // The product (which carries the category) is attached to the line item.
        Assert.Equal("PRD-001", data.Expenses[0].LineItems[0].ProductId);
        Assert.True(data.Expenses[0].BankMatched);
        Assert.Equal("L1", data.Expenses[0].BankMatchedLineId);
        Assert.Equal(BankLineMatchStatus.Matched, line.MatchStatus);
        Assert.Single(result.CreatedTransactions);
    }

    [Fact]
    public void CreateFromLines_NewSupplierAndProduct_AutoCreatesBoth_WithCategory()
    {
        var data = new CompanyData();
        var line = new BankStatementLine { Id = "L2", Date = new DateTime(2026, 4, 9), Description = "NEW VENDOR", Amount = -10m };
        var resolution = new BankLineResolution
        {
            Line = line,
            Type = BookRecordType.Expense,
            NewCounterpartyName = "New Vendor Ltd",
            NewProductName = "Fasteners",
            NewProductCategoryName = "Materials"
        };

        var result = new BankLineImportService().CreateFromLines(data, [resolution]);

        Assert.Single(data.Suppliers);
        Assert.Equal("New Vendor Ltd", data.Suppliers[0].Name);
        Assert.Equal(data.Suppliers[0].Id, data.Expenses[0].SupplierId);

        var product = Assert.Single(data.Products);
        Assert.Equal("Fasteners", product.Name);
        Assert.Equal(CategoryType.Expense, product.Type);

        var category = Assert.Single(data.Categories);
        Assert.Equal("Materials", category.Name);
        Assert.Equal(category.Id, product.CategoryId);
        Assert.Equal(product.Id, data.Expenses[0].LineItems[0].ProductId);

        Assert.Contains(result.CreatedEntities, e => e is Supplier);
        Assert.Contains(result.CreatedEntities, e => e is Product);
        Assert.Contains(result.CreatedEntities, e => e is Category);
    }

    [Fact]
    public void CreateFromLines_DuplicateNewProduct_CreatesOnlyOne()
    {
        var data = new CompanyData();
        var l1 = new BankStatementLine { Id = "A", Date = new DateTime(2026, 4, 1), Description = "SHELL 1", Amount = -20m };
        var l2 = new BankStatementLine { Id = "B", Date = new DateTime(2026, 4, 2), Description = "SHELL 2", Amount = -25m };
        var resolutions = new[]
        {
            new BankLineResolution { Line = l1, Type = BookRecordType.Expense, NewProductName = "Fuel", NewProductCategoryName = "Vehicle" },
            new BankLineResolution { Line = l2, Type = BookRecordType.Expense, NewProductName = "Fuel", NewProductCategoryName = "Vehicle" }
        };

        new BankLineImportService().CreateFromLines(data, resolutions);

        // Both lines reuse a single created product + category.
        Assert.Single(data.Products);
        Assert.Single(data.Categories);
        Assert.Equal(2, data.Expenses.Count);
        Assert.Equal(data.Products[0].Id, data.Expenses[0].LineItems[0].ProductId);
        Assert.Equal(data.Products[0].Id, data.Expenses[1].LineItems[0].ProductId);
    }

    [Fact]
    public void CreateFromLines_MoneyIn_CreatesRevenue()
    {
        var data = new CompanyData();
        var line = new BankStatementLine { Id = "L3", Date = new DateTime(2026, 4, 6), Description = "STRIPE", Amount = 1200m };
        var resolution = new BankLineResolution { Line = line, Type = BookRecordType.Revenue };

        new BankLineImportService().CreateFromLines(data, [resolution]);

        Assert.Single(data.Revenues);
        Assert.Equal(1200m, data.Revenues[0].Total);
        Assert.Equal(BankLineMatchStatus.Matched, line.MatchStatus);
    }

    [Fact]
    public void CreateFromLines_LinkToBankLineFalse_CreatesPlainTransaction()
    {
        var data = new CompanyData();
        data.Products.Add(new Product { Id = "PRD-001", Name = "Office", Type = CategoryType.Expense });
        var line = new BankStatementLine { Id = "L1", Date = new DateTime(2026, 4, 5), Description = "AMZN", Amount = -10m };
        var resolution = new BankLineResolution { Line = line, Type = BookRecordType.Expense, ProductId = "PRD-001" };

        new BankLineImportService().CreateFromLines(data, [resolution], linkToBankLine: false);

        Assert.Single(data.Expenses);
        Assert.False(data.Expenses[0].BankMatched);
        Assert.Null(data.Expenses[0].BankMatchedLineId);
        Assert.Equal(BankLineMatchStatus.Unmatched, line.MatchStatus); // unchanged from default
    }
}
