using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using ClosedXML.Excel;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Importing over existing records (with "Skip existing records" off) changes only what the sheet
/// carries. A column the sheet does not have leaves the stored value alone; before, it was read
/// as blank and overwrote it, so a sheet of ids and notes wiped addresses, zeroed totals, reset
/// payment methods to Cash and marked unpaid revenue as paid.
/// </summary>
public class SpreadsheetImportUpdateTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"update_{Guid.NewGuid():N}.xlsx");

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
        GC.SuppressFinalize(this);
    }

    private async Task ImportSheetAsync(CompanyData data, string sheet, string[] headers, params object[][] rows)
    {
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet(sheet);
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < rows[r].Length; c++)
                    ws.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r][c]);
            wb.SaveAs(_path);
        }

        await new SpreadsheetImportService().ImportFromExcelAsync(_path, data, new ImportOptions { SkipExistingRecords = false });
    }

    private static Address Calgary() => new() { Street = "1 Main St", City = "Calgary", State = "AB", ZipCode = "T2P1A1", Country = "Canada" };

    [Fact]
    public async Task UpdatingACustomersNotes_LeavesTheRestOfTheCustomer()
    {
        var data = new CompanyData();
        data.Customers.Add(new Customer
        {
            Id = "CUS-001", Name = "Jane Doe", CompanyName = "Doe Co", Email = "jane@x.com", Phone = "555-0100",
            Address = Calgary(), Notes = "old", Status = EntityStatus.Inactive, TotalPurchases = 99m
        });

        await ImportSheetAsync(data, "Customers", ["ID", "Notes"], ["CUS-001", "new"]);

        var c = Assert.Single(data.Customers);
        Assert.Equal("new", c.Notes);
        Assert.Equal("Jane Doe", c.Name);
        Assert.Equal("Doe Co", c.CompanyName);
        Assert.Equal("jane@x.com", c.Email);
        Assert.Equal("555-0100", c.Phone);
        Assert.Equal("Calgary", c.Address.City);
        Assert.Equal(EntityStatus.Inactive, c.Status);
        Assert.Equal(99m, c.TotalPurchases);
    }

    [Fact]
    public async Task UpdatingASuppliersNotes_LeavesTheRestOfTheSupplier()
    {
        var data = new CompanyData();
        data.Suppliers.Add(new Supplier
        {
            Id = "SUP-001", Name = "Acme", Email = "a@acme.com", Phone = "555-0199", Website = "acme.com", Address = Calgary(), Notes = "old"
        });

        await ImportSheetAsync(data, "Suppliers", ["ID", "Notes"], ["SUP-001", "new"]);

        var s = Assert.Single(data.Suppliers);
        Assert.Equal("new", s.Notes);
        Assert.Equal("Acme", s.Name);
        Assert.Equal("a@acme.com", s.Email);
        Assert.Equal("555-0199", s.Phone);
        Assert.Equal("acme.com", s.Website);
        Assert.Equal("Calgary", s.Address.City);
    }

    [Fact]
    public async Task UpdatingAProductsDescription_LeavesItsCategorySupplierAndStockLevels()
    {
        var data = new CompanyData();
        data.Categories.Add(new Category { Id = "CAT-EXP-001", Name = "Supplies", Type = CategoryType.Expense });
        data.Suppliers.Add(new Supplier { Id = "SUP-001", Name = "Acme" });
        data.Products.Add(new Product
        {
            Id = "PRD-001", Name = "Paper", Type = CategoryType.Expense, ItemType = "Service", Sku = "P-1",
            Description = "old", CategoryId = "CAT-EXP-001", SupplierId = "SUP-001",
            ReorderPoint = 5, OverstockThreshold = 50, TrackInventory = true
        });

        await ImportSheetAsync(data, "Products", ["ID", "Description"], ["PRD-001", "new"]);

        var p = Assert.Single(data.Products);
        Assert.Equal("new", p.Description);
        Assert.Equal("Paper", p.Name);
        Assert.Equal(CategoryType.Expense, p.Type);
        Assert.Equal("Service", p.ItemType);
        Assert.Equal("P-1", p.Sku);
        Assert.Equal("CAT-EXP-001", p.CategoryId);
        Assert.Equal("SUP-001", p.SupplierId);
        Assert.Equal(5, p.ReorderPoint);
        Assert.Equal(50, p.OverstockThreshold);
    }

    [Fact]
    public async Task UpdatingALocationsPhone_LeavesTheRestOfTheLocation()
    {
        var data = new CompanyData();
        data.Locations.Add(new Location
        {
            Id = "LOC-001", Name = "Warehouse", ContactPerson = "Bob", Phone = "555-0111", Address = Calgary(),
            Capacity = 100, CurrentUtilization = 40
        });

        await ImportSheetAsync(data, "Locations", ["ID", "Phone"], ["LOC-001", "555-0222"]);

        var l = Assert.Single(data.Locations);
        Assert.Equal("555-0222", l.Phone);
        Assert.Equal("Warehouse", l.Name);
        Assert.Equal("Bob", l.ContactPerson);
        Assert.Equal("Calgary", l.Address.City);
        Assert.Equal(100, l.Capacity);
        Assert.Equal(40, l.CurrentUtilization);
    }

    private static List<LineItem> ThreeLines() =>
    [
        new() { ProductId = "PRD-001", Description = "Paper", Quantity = 1m, UnitPrice = 30m },
        new() { ProductId = "PRD-002", Description = "Pens", Quantity = 1m, UnitPrice = 30m },
        new() { ProductId = "PRD-003", Description = "Ink", Quantity = 1m, UnitPrice = 30m },
    ];

    [Fact]
    public async Task UpdatingAnExpensesReference_LeavesItsMoneyDateMethodAndLines()
    {
        var data = new CompanyData();
        data.Expenses.Add(new Expense
        {
            Id = "PUR-001", Date = new DateTime(2026, 2, 1), SupplierId = "SUP-001", Description = "Paper (+2 more)",
            Quantity = 3m, UnitPrice = 30m, Amount = 90m, TaxAmount = 9m, Total = 99m, TotalUSD = 99m,
            PaymentMethod = PaymentMethod.CreditCard, ReferenceNumber = "R1", OriginalCurrency = "USD",
            LineItems = ThreeLines()
        });

        await ImportSheetAsync(data, "Expenses", ["ID", "Reference"], ["PUR-001", "R2"]);

        var e = Assert.Single(data.Expenses);
        Assert.Equal("R2", e.ReferenceNumber);
        Assert.Equal(new DateTime(2026, 2, 1), e.Date);
        Assert.Equal("SUP-001", e.SupplierId);
        Assert.Equal(3m, e.Quantity);
        Assert.Equal(99m, e.Total);
        Assert.Equal(99m, e.TotalUSD);
        Assert.Equal(9m, e.TaxAmount);
        Assert.Equal(PaymentMethod.CreditCard, e.PaymentMethod);
        Assert.Equal(3, e.LineItems.Count);
    }

    [Fact]
    public async Task UpdatingARevenuesReference_LeavesItUnpaidWithItsMoneyAndLines()
    {
        var data = new CompanyData();
        data.Revenues.Add(new Revenue
        {
            Id = "REV-001", Date = new DateTime(2026, 2, 1), CustomerId = "CUS-001", Description = "Paper (+2 more)",
            Quantity = 3m, UnitPrice = 30m, Amount = 90m, Total = 90m, TotalUSD = 90m,
            PaymentStatus = RevenuePaymentStatus.Unpaid, ReferenceNumber = "R1", OriginalCurrency = "USD",
            LineItems = ThreeLines()
        });

        await ImportSheetAsync(data, "Revenue", ["ID", "Reference"], ["REV-001", "R2"]);

        var r = Assert.Single(data.Revenues);
        Assert.Equal("R2", r.ReferenceNumber);
        Assert.Equal(RevenuePaymentStatus.Unpaid, r.PaymentStatus);
        Assert.Equal(new DateTime(2026, 2, 1), r.Date);
        Assert.Equal("CUS-001", r.CustomerId);
        Assert.Equal(90m, r.Total);
        Assert.Equal(90m, r.TotalUSD);
        Assert.Equal(3, r.LineItems.Count);
        Assert.DoesNotContain(data.Products, p => p.Name == "Paper (+2 more)");
    }

    [Fact]
    public async Task UpdatingAForeignExpensesTotal_KeepsItsCurrency()
    {
        // No currency on the sheet says nothing about the currency, so a euro expense stays in euros
        // rather than becoming that many dollars.
        var data = new CompanyData();
        data.Expenses.Add(new Expense
        {
            Id = "PUR-001", Date = new DateTime(2999, 1, 1), Description = "Hotel", Quantity = 1m, UnitPrice = 100m,
            Amount = 100m, Total = 100m, OriginalCurrency = "EUR", IsPendingConversion = true
        });

        await ImportSheetAsync(data, "Expenses", ["ID", "Total"], ["PUR-001", 120]);

        var e = Assert.Single(data.Expenses);
        Assert.Equal(120m, e.Total);
        Assert.Equal("EUR", e.OriginalCurrency);
        Assert.True(e.IsPendingConversion);
        Assert.Contains(data.PendingConversions, p => p.TransactionId == "PUR-001" && p.OriginalCurrency == "EUR" && p.Total == 120m);
    }
}
