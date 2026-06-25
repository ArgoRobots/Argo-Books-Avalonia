using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.BankMatching;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class BankLineImportServiceTests
{
    [Fact]
    public void CreateFromLines_MoneyOut_CreatesExpense_AndMarksLineMatched()
    {
        var data = new CompanyData();
        data.Categories.Add(new Core.Models.Entities.Category
        { Id = "CAT-PUR-001", Name = "Office Supplies", Type = CategoryType.Expense });

        var line = new BankStatementLine { Id = "L1", Date = new DateTime(2026, 4, 5), Description = "AMZN MKTP", Amount = -38.20m };
        var resolution = new BankLineResolution
        {
            Line = line, Type = BookRecordType.Expense, CategoryId = "CAT-PUR-001", CounterpartyId = null
        };

        var result = new BankLineImportService().CreateFromLines(data, [resolution]);

        Assert.Single(data.Expenses);
        Assert.Equal(38.20m, data.Expenses[0].Total);
        Assert.True(data.Expenses[0].BankMatched);
        Assert.Equal("L1", data.Expenses[0].BankMatchedLineId);
        Assert.Equal(BankLineMatchStatus.Matched, line.MatchStatus);
        Assert.Equal(data.Expenses[0].Id, line.MatchedRecordId);
        Assert.Single(result.CreatedTransactions);
    }

    [Fact]
    public void CreateFromLines_NewSupplierName_AutoCreatesSupplier()
    {
        var data = new CompanyData();
        var line = new BankStatementLine { Id = "L2", Date = new DateTime(2026, 4, 9), Description = "NEW VENDOR", Amount = -10m };
        var resolution = new BankLineResolution
        {
            Line = line, Type = BookRecordType.Expense, NewCounterpartyName = "New Vendor Ltd", NewCategoryName = "Misc"
        };

        var result = new BankLineImportService().CreateFromLines(data, [resolution]);

        Assert.Single(data.Suppliers);
        Assert.Equal("New Vendor Ltd", data.Suppliers[0].Name);
        Assert.Equal(data.Suppliers[0].Id, data.Expenses[0].SupplierId);
        Assert.Contains(result.CreatedEntities, e => e is Core.Models.Entities.Supplier);
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
        data.Categories.Add(new Core.Models.Entities.Category { Id = "CAT-PUR-001", Name = "Office", Type = CategoryType.Expense });
        var line = new BankStatementLine { Id = "L1", Date = new DateTime(2026, 4, 5), Description = "AMZN", Amount = -10m };
        var resolution = new BankLineResolution { Line = line, Type = BookRecordType.Expense, CategoryId = "CAT-PUR-001" };

        new BankLineImportService().CreateFromLines(data, [resolution], linkToBankLine: false);

        Assert.Single(data.Expenses);
        Assert.False(data.Expenses[0].BankMatched);
        Assert.Null(data.Expenses[0].BankMatchedLineId);
        Assert.Equal(BankLineMatchStatus.Unmatched, line.MatchStatus); // unchanged from default
    }
}
